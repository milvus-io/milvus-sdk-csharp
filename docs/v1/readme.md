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
using Milvus.Client;

string Host = "localhost";
int Port = 19530; // This is Milvus's default port
bool UseSsl = false; // Default value is false
string Database = "my_database"; // Defaults to the default Milvus database

// See documentation for other constructor paramters
MilvusClient milvusClient = new MilvusClient(Host, Port, UseSsl);
MilvusHealthState result = await milvusClient.HealthAsync();
```

### Create a collection.

```csharp
string collectionName = "book";
MilvusCollection collection = milvusClient.GetCollection(collectionName);

//Check if this collection exists
var hasCollection = await milvusClient.HasCollectionAsync(collectionName);

if(hasCollection){
    await collection.DropAsync();
    Console.WriteLine("Drop collection {0}",collectionName);
}

await milvusClient.CreateCollectionAsync(
            collectionName,
            new[] {
                FieldSchema.Create<long>("book_id", isPrimaryKey:true),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.CreateVarchar("book_name", 256),
                FieldSchema.CreateFloatVector("book_intro", 2)
            }
        );
```

### Insert vectors.

```csharp
Random ran = new ();
List<long> bookIds = new ();
List<long> wordCounts = new ();
List<ReadOnlyMemory<float>> bookIntros = new ();
List<string> bookNames = new ();
for (long i = 0L; i < 2000; ++i)
{
    bookIds.Add(i);
    wordCounts.Add(i + 10000);
    bookNames.Add($"Book Name {i}");

    float[] vector = new float[2];
    for (int k = 0; k < 2; ++k)
    {
        vector[k] = ran.Next();
    }
    bookIntros.Add(vector);
}

MilvusCollection collection = milvusClient.GetCollection(collectionName);

MutationResult result = await collection.InsertAsync(
    new FieldData[]
    {
        FieldData.Create("book_id", bookIds),
        FieldData.Create("word_count", wordCounts),
        FieldData.Create("book_name", bookNames),
        FieldData.CreateFloatVector("book_intro", bookIntros),
    });

// Check result
Console.WriteLine("Insert status: {0},", result.ToString());
```

### Create a index and load collection.

```csharp
MilvusCollection collection = milvusClient.GetCollection(collectionName);
await collection.CreateIndexAsync(
    "book_intro",
    //MilvusIndexType.IVF_FLAT,//Use MilvusIndexType.IVF_FLAT.
    IndexType.AutoIndex,//Use MilvusIndexType.AUTOINDEX when you are using zilliz cloud.
    SimilarityMetricType.L2);

// Check index status
IList<MilvusIndexInfo> indexInfos = await collection.DescribeIndexAsync("book_intro");

foreach(var info in indexInfos){
    Console.WriteLine("FieldName:{0}, IndexName:{1}, IndexId:{2}", info.FieldName , info.IndexName,info.IndexId);
}

// Then load it
await collection.LoadAsync();
```

### Search and query.

```csharp
// Search
List<string> search_output_fields = new() { "book_id" };
List<List<float>> search_vectors = new() { new() { 0.1f, 0.2f } };
SearchResults searchResult = await collection.SearchAsync(
    "book_intro",
    new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
    SimilarityMetricType.L2,
    limit: 2);

// Query
string expr = "book_id in [2,4,6,8]";

QueryParameters queryParameters = new ();
queryParameters.OutputFields.Add("book_id");
queryParameters.OutputFields.Add("word_count");

IReadOnlyList<FieldData> queryResult = await collection.QueryAsync(
    expr,
    queryParameters);
```
