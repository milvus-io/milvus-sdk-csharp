# Manage Indexes

## Build an Index on Vectors

### Prepare index parameter

```c#
var indexType = IndexType.IvfFlat;
var metricType = SimilarityMetricType.L2;
var extraParams = new Dictionary<string, string> { ["nlist"] = "1024" };
```

### Build index

```c#
await milvusClient.GetCollection("book").CreateIndexAsync("book_intro", indexType, metricType, extraParams: extraParams);
```

## Build an Index on Scalars

```c#
var collection = milvusClient.GetCollection("book");
await collection.CreateIndexAsync("book_name", indexName: "scalar_index");
await collection.LoadAsync();
```

## Drop an Index

```c#
await milvusClient.GetCollection("book").DropIndexAsync("book_intro");
```