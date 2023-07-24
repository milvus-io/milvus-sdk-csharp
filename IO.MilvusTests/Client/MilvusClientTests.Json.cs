using Xunit;
using IO.Milvus;
using System.Text.Json.Serialization;
using System.Text.Json;
using FluentAssertions;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    private const string Link = "https://medium.com/swlh/the-reported-mortality-rate-of-coronavirus-is-not-important-369989c8d912";

    /// <summary>
    /// <see href="https://milvus.io/docs/json_data_type.md"/>
    /// </summary>
    /// <param name="milvusClient"></param>
    /// <returns></returns>
    [Fact]
    public async Task JsonTest()
    {
        var version = await Client.GetMilvusVersionAsync();
        if (!version.GreaterThan(2, 2, 8))
        {
            return;
        }

        MilvusCollection collection = Client.GetCollection(Client.GetType().Name + "Json");

        //Check if collection exists.
        bool collectionExist = await Client.HasCollectionAsync(collection.Name);
        if (collectionExist)
        {
            await collection.DropAsync();
        }

        //Define fields.
        var fields = new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true, autoId: true),
                FieldSchema.CreateVarchar("title", maxLength: 512),
                FieldSchema.CreateFloatVector("title_vector", dimension: 2),
                FieldSchema.CreateJson("article_meta")
            };

        //Create collection.
        collection = await Client.CreateCollectionAsync(collection.Name, fields);

        //Create index.
        await collection.CreateIndexAsync(
            "title_vector",
            MilvusIndexType.AutoIndex,
            MilvusSimilarityMetricType.L2, new Dictionary<string, string>(), "idx");

        await collection.LoadAsync();

        await collection.WaitForCollectionLoadAsync(
            waitingInterval: TimeSpan.FromSeconds(1),
            timeout: TimeSpan.FromSeconds(10));

        List<ReadOnlyMemory<float>> vectors = new();
        List<ArticleMeta> articleMetas = new();
        List<string> titles = new();

        Random r = new();
        for (int i = 0; i < 100; i++)
        {
            var vector = new float[2];
            vector[0] = i / 10f;
            vector[1] = 9 * i / 10f;

            titles.Add("title" + i);
            articleMetas.Add(new ArticleMeta(
                        link: Link,
                        readingTime: r.Next(0, 40),
                        publication: "The Startup",
                        claps: r.Next(20, 50),
                        responses: 18));

            vectors.Add(vector);
        }

        int count = articleMetas.Count(p => p is { ReadingTime: < 10, Claps: > 30 });
        List<string> metaList = articleMetas
            .Select(p => JsonSerializer.Serialize(p))
            .ToList();

        await collection.InsertAsync(
            new[]
            {
                FieldData.Create("title", titles),
                FieldData.CreateFloatVector("title_vector", vectors),
                FieldData.CreateJson("article_meta", metaList)
            });

        MilvusSearchResults searchResults = await collection.SearchAsync(
            vectorFieldName: "title_vector",
            new ReadOnlyMemory<float>[] { new[] { 0.5f, 0.5f } },
            MilvusSimilarityMetricType.L2,
            limit: 3,
            new()
            {
                OutputFields = { "title", " article_meta" },
                Expression = """article_meta["claps"] > 30 and article_meta["reading_time"] < 10""",
                ConsistencyLevel = ConsistencyLevel.Strong,
                Parameters = { { "nprobe", "10" } }
            });

        var metaField = Assert.IsType<FieldData<string>>(
            searchResults.FieldsData.First(p => p.FieldName == "article_meta"));
        metaField.DataType.Should().Be(MilvusDataType.Json);
        ArticleMeta? sampleArticleMeta = JsonSerializer.Deserialize<ArticleMeta>(metaField.Data.First());
        Assert.NotNull(sampleArticleMeta);
        sampleArticleMeta.Link.Should().Be(Link);
    }
}

internal class ArticleMeta
{
    public ArticleMeta(
        string link,
        int readingTime,
        string publication,
        int claps,
        int responses)
    {
        Link = link;
        ReadingTime = readingTime;
        Publication = publication;
        Claps = claps;
        Responses = responses;
    }

    [JsonPropertyName("link")]
    public string Link { get; }

    [JsonPropertyName("reading_time")]
    public int ReadingTime { get; }

    [JsonPropertyName("publication")]
    public string Publication { get; }

    [JsonPropertyName("claps")]
    public int Claps { get; }

    [JsonPropertyName("responses")]
    public int Responses { get; }
}
