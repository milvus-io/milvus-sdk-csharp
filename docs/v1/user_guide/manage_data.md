# Manage Data

## Insert Entities

### Prepare data

```c#
var bookIds = new long[2000];
var wordCounts = new long[2000];
var bookIntros = new ReadOnlyMemory<float>[2000];

for (var i = 0; i < 2000; i++)
{
    bookIds[i] = i;
    wordCounts[i] = i + 10000;
    bookIntros[i] = new[] { Random.Shared.NextSingle(), Random.Shared.NextSingle() };
}
```

### Insert data to Milvus

```c#
await milvusClient.GetCollection("book").InsertAsync(new FieldData[]
{
    FieldData.Create("book_id", bookIds),
    FieldData.Create("word_count", wordCounts),
    FieldData.CreateFloatVector("book_intro", bookIntros)
});
```

## Delete Entities

### Prepare boolean expression

```c#
var expression = "book_id in [0,1]"; 
```

### Delete entities

```c#
await milvusClient.GetCollection("book").DeleteAsync(expression);
```

## Compact Data

### Compact data manually

```c#
await milvusClient.GetCollection("book").CompactAsync();
```
