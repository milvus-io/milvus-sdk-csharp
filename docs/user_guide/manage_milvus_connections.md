# Manage Milvus Connections

## Connect to a Milvus server

```c#
var milvusClient = new MilvusClient("localhost", username: "username", password: "password");
```

## Disconnect from a Milvus server

```c#
milvusClient.Dispose();
```