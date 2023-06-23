using IO.Milvus.Client;
using Xunit;
using IO.Milvus;
using IO.Milvus.Client.gRPC;
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
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task JsonTest(IMilvusClient milvusClient)
    {
        var version = await milvusClient.GetMilvusVersionAsync();
        if (!version.GreaterThan(2,2,8))
        {
            return;
        }

        var collectionName = milvusClient.GetType().Name + "Json";

        //Check if collection exists.
        var collectionExist =  await milvusClient.HasCollectionAsync(collectionName);
        if (collectionExist)
        {
            await milvusClient.DropCollectionAsync(collectionName);
        }

        //Define fields.
        var fields = new[]
            {
                FieldType.Create<long>("id",isPrimaryKey: true, autoId: true),
                FieldType.CreateVarchar("title",maxLength:512),
                FieldType.CreateFloatVector("title_vector",dim:2),
                FieldType.CreateJson("article_meta")
            };

        //Create collection.
        await milvusClient.CreateCollectionAsync(
            collectionName,
            fields
            );

        //Create index.
        await milvusClient.CreateIndexAsync(
            collectionName,
            "title_vector",
            Constants.DEFAULT_INDEX_NAME,
            MilvusIndexType.AUTOINDEX,
            MilvusMetricType.L2);

        await milvusClient.LoadCollectionAsync(collectionName);

        if (milvusClient is MilvusGrpcClient)
        {
            await milvusClient.WaitForLoadingProgressCollectionAsync(
                collectionName,
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10));
        }
        else
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        List<long> ids = new();
        List<List<float>> vectors = new ();
        List<ArticleMeta> articleMetas = new ();
        List<string> titles = new ();

        Random r = new();
        for (int i = 0; i < 100; i++)
        {
            var vector = new List<float>(2);
            float value = r.Next(0, 1);
            vector.Add(value);
            vector.Add(1 - value);

            ids.Add(i);
            titles.Add("title" + i.ToString());
            articleMetas.Add(new ArticleMeta(
                        link: Link,
                        readingTime: r.Next(0,40),
                        publication: "The Startup",
                        claps: r.Next(20,50),
                        responses: 18
                        ));

            vectors.Add(vector);
        }

        var count = articleMetas
            .Where(p => p.ReadingTime < 10 && p.Claps > 30)
            .Count();
        List<string> metaList = articleMetas
            .Select(p => JsonSerializer.Serialize(p))
            .ToList();

        await milvusClient.InsertAsync(
            collectionName,
            new Field[]
            {
                Field.Create<long>("id",ids),
                Field.Create<string>("title", titles),
                Field.CreateFloatVector("title_vector",vectors),
                Field.CreateJson("article_meta",metaList)
            }
            );

        await milvusClient.FlushAsync(new[] { collectionName });

        MilvusSearchResult searchResult = await milvusClient.SearchAsync(MilvusSearchParameters.Create(
            collectionName,
            vectorFieldName: "title_vector",
            outFields: new[] { "title", " article_meta" })
            .WithTopK(3)
            .WithExpr("article_meta[\"claps\"] > 30 and article_meta[\"reading_time\"] < 10")
            .WithMetricType(MilvusMetricType.L2)
            .WithParameter("nprobe",10.ToString())
            .WithVectors(new[] { new List<float> { 0.5f, 0.5f } })
            );

        searchResult.Results.NumQueries.Should().BeLessThanOrEqualTo(count);

        var metaField = searchResult.Results.FieldsData.First(p => p.FieldName == "article_meta")
            as Field<string>;
        metaField.DataType.Should().Be(MilvusDataType.Json);
        ArticleMeta? sampleArticleMeta = JsonSerializer.Deserialize<ArticleMeta>(metaField.Data.First());
        sampleArticleMeta.Should().NotBeNull();
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