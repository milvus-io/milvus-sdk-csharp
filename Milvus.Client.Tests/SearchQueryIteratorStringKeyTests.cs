﻿using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class SearchQueryIteratorStringKeyTests : IClassFixture<SearchQueryIteratorStringKeyTests.DataCollectionFixture>,
                                                 IAsyncLifetime
{
    private const string CollectionName = nameof(SearchQueryIteratorStringKeyTests);

    private readonly DataCollectionFixture _dataCollectionFixture;
    private readonly MilvusClient Client;

    public SearchQueryIteratorStringKeyTests(MilvusFixture milvusFixture, DataCollectionFixture dataCollectionFixture)
    {
        Client = milvusFixture.CreateClient();
        _dataCollectionFixture = dataCollectionFixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    private MilvusCollection Collection => _dataCollectionFixture.Collection;

    [Fact]
    public async Task QueryWithIterator_NoOutputFields()
    {
        var items = new List<Item>
        {
            new("1", new[] { 10f, 20f }),
            new("2", new[] { 30f, 40f }),
            new("3", new[] { 50f, 60f })
        };

        await Collection.InsertAsync(
        [
            FieldData.Create("id", items.Select(x => x.Id).ToArray()),
            FieldData.CreateFloatVector("float_vector", items.Select(x => x.Vector).ToArray())
        ]);

        var iterator = Collection.QueryWithIteratorAsync();

        List<IReadOnlyList<FieldData>> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        var returnedItems = results.SelectMany(ExtractItems).ToList();
        Assert.Empty(returnedItems);
    }

    [Theory]
    [InlineData("id in ['1', '2', '3']", 1, null)]
    [InlineData("id in ['1', '2', '3']", 1, 2)]
    [InlineData("id in ['1', '2', '3']", 2, null)]
    [InlineData("id in ['1', '2', '3']", 2, 2)]
    [InlineData("id in ['1', '2', '3']", 1000, null)]
    [InlineData("id in ['1', '2', '3']", 1000, 2)]
    [InlineData(null, 1, null)]
    [InlineData(null, 1, 2)]
    [InlineData(null, 2, null)]
    [InlineData(null, 2, 2)]
    [InlineData(null, 1000, null)]
    [InlineData(null, 1000, 2)]
    public async Task QueryWithIterator(string? expression, int batchSize, int? limit)
    {
        var items = new List<Item>
        {
            new("1", new[] { 10f, 20f }),
            new("2", new[] { 30f, 40f }),
            new("3", new[] { 50f, 60f })
        };

        await Collection.InsertAsync(
        [
            FieldData.Create("id", items.Select(x => x.Id).ToArray()),
            FieldData.CreateFloatVector("float_vector", items.Select(x => x.Vector).ToArray())
        ]);

        var queryParameters = new QueryParameters
        {
            OutputFields = { "id", "float_vector" },
            Limit = limit
        };

        var iterator = Collection.QueryWithIteratorAsync(
            expression: expression,
            batchSize: batchSize,
            parameters: queryParameters);

        List<IReadOnlyList<FieldData>> results = new();
        await foreach (var result in iterator)
        {
            results.Add(result);
        }

        var returnedItems = results.SelectMany(ExtractItems).ToArray();
        var expectedItems = items.Take(limit ?? int.MaxValue).ToArray();
        Assert.Equal(expectedItems.Length, returnedItems.Length);

        foreach (var expectedItem in expectedItems)
        {
            Assert.Contains(expectedItem, returnedItems);
        }
    }

    IEnumerable<Item> ExtractItems(IReadOnlyList<FieldData> result)
    {
        long rowCount = result.Select(x => x.RowCount).FirstOrDefault();

        var items = new Item[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            items[i] = new Item();
        }

        foreach (var fieldData in result)
        {
            switch (fieldData.FieldName)
            {
                case "id":
                {
                    for (int j = 0; j < rowCount; j++)
                    {
                        items[j].Id = ((FieldData<string>) fieldData).Data[j];
                    }

                    break;
                }

                case "float_vector":
                {
                    for (int j = 0; j < rowCount; j++)
                    {
                        items[j].Vector = ((FloatVectorFieldData) fieldData).Data[j];
                    }

                    break;
                }
            }
        }

        return items;
    }

    #region Nested type: DataCollectionFixture

    public class DataCollectionFixture : IAsyncLifetime
    {
        public MilvusCollection Collection;
        private readonly MilvusClient Client;

        public DataCollectionFixture(MilvusFixture milvusFixture)
        {
            Client = milvusFixture.CreateClient();
            Collection = Client.GetCollection(CollectionName);
        }

        public async Task InitializeAsync()
        {
            await Collection.DropAsync();

            await Client.CreateCollectionAsync(
                Collection.Name,
                new[]
                {
                    FieldSchema.CreateVarchar("id", 16, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("float_vector", 2)
                });

            await Collection.CreateIndexAsync("float_vector", IndexType.Flat, SimilarityMetricType.L2);
            await Collection.WaitForIndexBuildAsync("float_vector");
            await Collection.LoadAsync();
            await Collection.WaitForCollectionLoadAsync();
        }

        public Task DisposeAsync()
        {
            Client.Dispose();
            return Task.CompletedTask;
        }
    }

    #endregion

    #region Nested type: Item

    public record Item
    {
        public Item(string id, ReadOnlyMemory<float> vector)
        {
            Id = id;
            Vector = vector;
        }

        public Item()
        {
        }

        public virtual bool Equals(Item? other)
        {
            return other != null && Id == other.Id && Vector.Span.SequenceEqual(other.Vector.Span);
        }

        public string? Id { get; set; }

        public ReadOnlyMemory<float> Vector { get; set; }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Id);
            foreach (float value in Vector.ToArray())
            {
                hashCode.Add(value);
            }

            return hashCode.ToHashCode();
        }
    }

    #endregion
}
