# Search and Query

## Search

### Load collection

```c#
var collection = milvusClient.GetCollection("book").LoadAsync();
```

### Prepare search parameters

```c#
var parameters = new SearchParameters
{
    OutputFields = { "title" },
    ConsistencyLevel = ConsistencyLevel.Strong,
    Offset = 5,
    ExtraParameters = { ["nprobe"] = "1024" }
};
```

### Conduct a vector search

```c#
var results = await milvusClient.GetCollection("book").SearchAsync(
    vectorFieldName: "book_intro",
    vectors: new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
    SimilarityMetricType.L2,
    // the sum of `offset` in `parameters` and `limit` should be less than 16384.
    limit: 10,
    parameters);
```

```c#
var collection = milvusClient.GetCollection("book").ReleaseAsync();
```

## Hybrid Search

### Load collection

```c#
var collection = milvusClient.GetCollection("book").LoadAsync();
```

### Conduct a hybrid vector search

```c#
var results = await milvusClient.GetCollection("book").SearchAsync(
    vectorFieldName: "book_intro",
    vectors: new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
    SimilarityMetricType.L2,
    limit: 10,
    new SearchParameters
    {
        OutputFields = { "title" },
        ConsistencyLevel = ConsistencyLevel.Strong,
        Offset = 5,
        Expression = "word_count <= 11000",
        ExtraParameters = { ["nprobe"] = "1024" }
    });
```

## Query

### Load collection

```c#
var collection = milvusClient.GetCollection("book").LoadAsync();
```

### Conduct a vector query

```c#
var results = await Client.GetCollection("book").QueryAsync(
    expression: "book_id in [2,4,6,8]",
    new QueryParameters
    {
        Offset = 0,
        Limit = 10,
        OutputFields = { "book_id", "book_intro" }
    });
```
