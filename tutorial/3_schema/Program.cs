using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;

string uri = Environment.GetEnvironmentVariable("MILVUS_URI") ?? "localhost:19530";
string token = Environment.GetEnvironmentVariable("MILVUS_TOKEN") ?? "root:Milvus";
string prefix = TutorialCollectionPrefix();
string[] collections = [$"{prefix}_SCALAR", $"{prefix}_VECTOR", $"{prefix}_STRUCT"];

// MilvusClientV2 connects to the configured endpoint and authenticates the schema operations.
Console.WriteLine("Calling MilvusClientV2: connect to Milvus");
using var client = CreateClient(uri, token);
await client.ConnectAsync();
Console.WriteLine("MilvusClientV2 connected");

// Capture the tutorial result so cleanup still runs if a later operation fails.
Exception? tutorialError = await DemonstrateSchemas(client, collections);
Exception? cleanupError = await CleanupCollections(client, collections);

if (cleanupError is not null)
{
    Console.WriteLine($"Failed to clean up the schema tutorial: {cleanupError.Message}");
}

if (tutorialError is not null)
{
    throw tutorialError;
}

if (cleanupError is not null)
{
    throw cleanupError;
}

async Task<Exception?> DemonstrateSchemas(MilvusClientV2 client, IReadOnlyList<string> collections)
{
    try
    {
        await CreateAndDescribe(client, collections[0], "Scalar and container data types", ScalarSchema());
        await CreateAndDescribe(client, collections[1], "Vector data types", VectorSchema());
        await CreateAndDescribe(client, collections[2], "Sparse, Int8, and struct data types", StructSchema());
        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

async Task CreateAndDescribe(MilvusClientV2 client, string collection, string heading, CollectionSchema schema)
{
    Console.WriteLine($"\n{heading}: {collection}");
    Console.WriteLine("Calling CreateCollectionAsync");
    await client.CreateCollectionAsync(new CreateCollectionReq
    {
        CollectionName = collection,
        Schema = schema
    });
    Console.WriteLine("CreateCollectionAsync completed");

    Console.WriteLine("Calling DescribeCollectionAsync: read the schema back");
    DescribeCollectionResp response = await client.DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = collection });
    Console.WriteLine("DescribeCollectionAsync completed");
    foreach (FieldSchema field in response.Schema.Fields)
    {
        Console.WriteLine($"  field={field.Name,-16} type={field.DataType}" +
            (field.Dimension is { } dim ? $" dim={dim}" : "") +
            (field.MaxLength is { } maxLen ? $" max_length={maxLen}" : "") +
            (field.ElementDataType is { } elem ? $" element={elem}" : "") +
            (field.Nullable ? " nullable" : "") +
            (field.DefaultValue is not null ? $" default={field.DefaultValue}" : ""));
    }
}

CollectionSchema ScalarSchema() => new()
{
    Description = "Scalar and container field examples",
    EnableDynamicFields = false,
    Fields =
    {
        new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
        FieldSchema.CreateFloatVector("required_vector", dimension: 2),
        new FieldSchema("bool_value", DataType.Bool) { Nullable = true },
        new FieldSchema("int8_value", DataType.Int8) { DefaultValue = (sbyte)0 },
        new FieldSchema("int16_value", DataType.Int16),
        new FieldSchema("int32_value", DataType.Int32),
        new FieldSchema("int64_value", DataType.Int64),
        new FieldSchema("float_value", DataType.Float),
        new FieldSchema("double_value", DataType.Double),
        FieldSchema.CreateVarchar("varchar_value", maxLength: 512),
        new FieldSchema("json_value", DataType.Json),
        new FieldSchema("geometry_value", DataType.Geometry),
        new FieldSchema("timestamp_value", DataType.Timestamptz),
        new FieldSchema("int64_array", DataType.Array)
        {
            ElementDataType = DataType.Int64,
            MaxCapacity = 32
        }
    }
};

CollectionSchema VectorSchema() => new()
{
    Description = "Dense, binary, and sparse vector examples",
    EnableDynamicFields = false,
    Fields =
    {
        new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
        FieldSchema.CreateFloatVector("float_vector", dimension: 8),
        new FieldSchema("binary_vector", DataType.BinaryVector) { Dimension = 64 },
        new FieldSchema("float16_vector", DataType.Float16Vector) { Dimension = 8 },
        new FieldSchema("bfloat16_vector", DataType.BFloat16Vector) { Dimension = 8 }
    }
};

CollectionSchema StructSchema() => new()
{
    Description = "Sparse, Int8, and struct field examples",
    EnableDynamicFields = false,
    Fields =
    {
        new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
        FieldSchema.CreateSparseFloatVector("sparse_vector"),
        FieldSchema.CreateFloatVector("required_vector", dimension: 4),
        new FieldSchema("int8_vector", DataType.Int8Vector) { Dimension = 8 }
    },
    StructFields =
    {
        new StructFieldSchema("events", "An entity may contain up to 16 event records")
        {
            MaxCapacity = 16,
            Fields =
            {
                FieldSchema.CreateVarchar("label", maxLength: 128),
                new FieldSchema("ts", DataType.Int64)
            }
        }
    }
};

async Task<Exception?> CleanupCollections(MilvusClientV2 client, IReadOnlyList<string> collections)
{
    Exception? firstError = null;
    foreach (string name in collections)
    {
        try
        {
            HasCollectionResp exists = await client.HasCollectionAsync(new HasCollectionReq { CollectionName = name });
            if (!exists.Has)
            {
                continue;
            }

            Console.WriteLine($"\nCalling DropCollectionAsync: remove {name}");
            await client.DropCollectionAsync(new DropCollectionReq { CollectionName = name });
            Console.WriteLine("DropCollectionAsync completed");
        }
        catch (Exception ex)
        {
            firstError ??= ex;
        }
    }

    return firstError;
}

static MilvusClientV2 CreateClient(string uri, string token)
{
    var config = new ConnectConfig { Uri = uri };
    int colon = token.IndexOf(':');
    if (colon > 0)
    {
        config.Username = token[..colon];
        config.Password = token[(colon + 1)..];
    }
    else
    {
        config.ApiKey = token;
    }

    return new MilvusClientV2(config);
}

static string TutorialCollectionPrefix()
{
    return $"CSHARP_V2_SCHEMA_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Environment.ProcessId}";
}
