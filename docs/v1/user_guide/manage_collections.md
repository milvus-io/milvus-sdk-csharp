# Manage Collections

## Create a Collection

### Prepare schema

```c#
var schema = new CollectionSchema
{
    Fields =
    {
        FieldSchema.Create<long>("book_id", isPrimaryKey: true),
        FieldSchema.CreateVarchar("book_name", maxLength: 200),
        FieldSchema.Create<long>("word_count"),
        FieldSchema.CreateFloatVector("book_intro", dimension: 2)
    },
    Description = "Test book search",
    EnableDynamicFields = true
};
```

### Create a collection with the schema

```c#
var collection = await milvusClient.CreateCollectionAsync(collectionName, schema, shardsNum: 2);
```

## Rename a Collection

```c#
var collection = milvusClient.GetCollection("old_collection").RenameAsync("new_collection");
```

## Check Collection Information

### Check if a collection exists

```c#
var collectionExists = await Client.HasCollectionAsync("book");
```

### Check collection details

```c#
var collection = milvusClient.GetCollection("book").DescribeAsync();
```

### List all collections

```c#
var collections = await milvusClient.ListCollectionsAsync();
```

## Drop a Collection

```c#
var collection = Client.GetCollection("book").DropAsync();
```

## Manage Collection Alias

### Create a collection alias

```c#
await milvusClient.CreateAliasAsync("book", "publication");
```

### Drop a collection alias

```c#
await milvusClient.DropAliasAsync("publication");
```

### Alter a collection alias

```c#
await milvusClient.AlterAliasAsync("book", "publication");
```

## Load Collection

```c#
var collection = Client.GetCollection("book").LoadAsync();
```

## Release Collection

```c#
var collection = Client.GetCollection("book").ReleaseAsync();
```
