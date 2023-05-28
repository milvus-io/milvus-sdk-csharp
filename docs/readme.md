# milvus-sdk-c#

[Github Repository](https://github.com/milvus-io/milvus-sdk-csharp)

[Milvus docs](https://milvus.io/docs)

## Overview

This is the C# SDK for Milvus.
It is implemented using the gRPC protocol and provides most of the features of the Milvus server.
And restful api support is ready now.

### Create a client.

> Grpc

```csharp
using IO.Milvus.Client.gRPC;

IMilvusClient client = new MilvusGrpcClient("{Endpoint}", "{Port}","{Username}","Password");
MilvusHealthState result = await milvusClient.HealthAsync();
```

> Restful

```csharp
using IO.Milvus.Client.gRPC;

IMilvusClient client = new MilvusRestClient("{Endpoint}", "{Port}","{Username}","Password");
MilvusHealthState result = await milvusClient.HealthAsync();
```

### Create a collection.

```csharp

string collectionName = "test_collection";
await client.CreateCollectionAsync(
            collectionName,
            new[] {
                FieldType.Create<long>("book_id",isPrimaryKey:true),
                FieldType.Create<long>("word_count"),
                FieldType.CreateVarchar("book_name",256),
                FieldType.CreateFloatVector("book_intro",2),
            }
        );

//Check if the collection exists.
bool collectionExist = await milvusClient.HasCollectionAsync(collectionName);
```

### Insert vectors.

```csharp
Random ran = new Random();
List<long> bookIds = new();
List<long> wordCounts = new();
List<List<float>> bookIntros = new();
List<string> bookNames = new();
for (long i = 0L; i < 2000; ++i)
{
    bookIds.Add(i);
    wordCounts.Add(i + 10000);
    bookNames.Add($"Book Name {i}");

    List<float> vector = new();
    for (int k = 0; k < 2; ++k)
    {
        vector.Add(ran.Next());
    }
    bookIntros.Add(vector);
}

MilvusMutationResult result = await milvusClient.InsertAsync(collectionName,
    new Field[]
    {
        Field.Create("book_id",bookIds),
        Field.Create("word_count",wordCounts),
        Field.Create("book_name",bookNames),
        Field.CreateFloatVector("book_intro",bookIntros),
    },
    partitionName);
```

### Create a index and load collection.

```csharp
await milvusClient.CreateIndexAsync(
    collectionName,
    "book_intro",
    "default",
    MilvusIndexType.IVF_FLAT,//Use MilvusIndexType.AUTOINDEX when you are using zilliz cloud.
    MilvusMetricType.L2,
    new Dictionary<string, string> { { "nlist", "1024" } });

//Then load it
await milvusClient.LoadCollectionAsync(collectionName);
```

### Search vectors.

```csharp

//Search
List<string> search_output_fields = new() { "book_id" };
List<List<float>> search_vectors = new() { new() { 0.1f, 0.2f } };
var searchResult = await milvusClient.SearchAsync(
    MilvusSearchParameters.Create(collectionName, "book_intro", search_output_fields)
    .WithVectors(search_vectors)
    .WithConsistencyLevel(MilvusConsistencyLevel.Strong)
    .WithMetricType(MilvusMetricType.IP)
    .WithTopK(topK: 2)
    .WithParameter("nprobe", "10")
    .WithParameter("offset", "5"));
```