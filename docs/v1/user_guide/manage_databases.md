# Manage Databases

# Create database

```c#
await milvusClient.CreateDatabaseAsync("book");
```

# Use a database

```c#
var milvusClient = new MilvusClient("localhost", database: "book");
```

# List database

```c#
var databases = await milvusClient.ListDatabasesAsync();
```

# Drop database

```c#
await milvusClient.DropDatabaseAsync("book");
```
