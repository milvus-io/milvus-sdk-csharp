# Manage Partitions

## Create a Partition

```c#
var collection = milvusClient.GetCollection("book").CreatePartitionAsync("novel");
```

## Check Partition Information

### Verify if a partition exists

```c#
var exists = await milvusClient.GetCollection("book").HasPartitionAsync("novel");
```

### List all partitions

```c#
var partitions = await milvusClient.GetCollection("book").ShowPartitionsAsync();
```

## Drop Partitions

```c#
await milvusClient.GetCollection("book").DropPartitionsAsync("novel");
```

## Load a Partition

```c#
await milvusClient.GetCollection("book").LoadPartitionAsync("novel");
```

## Release a Partition

```c#
await milvusClient.GetCollection("book").ReleasePartitionAsync("novel");
```
