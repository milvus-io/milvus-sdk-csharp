using System.Globalization;
using System.Text.Json;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to create a collection.
/// </summary>
/// <remarks>
/// <see cref="ConsistencyLevel" /> defaults to <see cref="ConsistencyLevel.BoundedStaleness" /> and
/// <see cref="ShardsNum" /> to 1, matching the Java/C++ SDKs. <see cref="NumPartitions" /> is only used for a
/// partition-key collection. Struct fields (<see cref="CollectionSchema.StructFields" />) can only be declared
/// at creation time.
/// </remarks>
public sealed class CreateCollectionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to create.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The schema definition for the collection.
    /// </summary>
    public CollectionSchema? Schema { get; set; }

    /// <summary>
    /// The consistency level to be used by the collection. Defaults to <see cref="ConsistencyLevel.BoundedStaleness" />,
    /// matching the Java and C++ SDKs.
    /// </summary>
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.BoundedStaleness;

    /// <summary>
    /// Number of the shards for the collection to create.
    /// </summary>
    public int ShardsNum { get; set; } = 1;

    /// <summary>
    /// Collection-level properties (e.g. TTL, consistency override, timezone, warmup), applied at creation time.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The number of default physical partitions, only used in partition-key mode.
    /// </summary>
    public long? NumPartitions { get; set; }

    /// <summary>
    /// Optional indexes to create immediately after the collection is created. When set, the collection is
    /// also loaded automatically after the indexes are built.
    /// </summary>
    public IReadOnlyList<IndexParam> Indexes { get; set; } = Array.Empty<IndexParam>();

    internal Grpc.CreateCollectionRequest ToGrpcCreateCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(Schema);

        // Fail-fast bounds matching the Java SDK: shards must be positive, partitions at least one, and a
        // VarChar primary key's max length within the server cap.
        if (ShardsNum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ShardsNum), ShardsNum, "ShardsNum must be greater than 0.");
        }

        if (NumPartitions is { } partitionCount && partitionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(NumPartitions), partitionCount,
                "NumPartitions must be at least 1.");
        }

        foreach (FieldSchema field in Schema.Fields)
        {
            if (field.DataType == DataType.VarChar && field.MaxLength is { } maxLength && maxLength > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(Schema), maxLength,
                    $"VarChar field '{field.Name}' max length {maxLength} exceeds the server cap of 65535.");
            }

            // Binary vectors store one bit per dimension; the server requires the dimension to be a multiple
            // of 8. Fail fast instead of waiting for a create-time server error.
            if (field.DataType == DataType.BinaryVector
                && field.Dimension is { } binaryDim
                && (binaryDim < 8 || binaryDim % 8 != 0))
            {
                throw new ArgumentOutOfRangeException(nameof(Schema), binaryDim,
                    $"Binary vector field '{field.Name}' dimension {binaryDim} must be a multiple of 8 (and at least 8).");
            }
        }

        Grpc.CollectionSchema grpcSchema = new()
        {
            // Always stamp the schema with the request's collection name (matching the Java SDK, which forces
            // setName(collectionName)). The server validates the schema name but never cross-checks it against
            // the request's CollectionName, so a caller-supplied different/empty Schema.Name would otherwise
            // create a collection whose described name differs from the one actually created.
            Name = CollectionName,
            EnableDynamicField = Schema.EnableDynamicFields
        };

        if (Schema.Description is not null)
        {
            grpcSchema.Description = Schema.Description;
        }

        foreach (FunctionSchema function in Schema.Functions)
        {
            grpcSchema.Functions.Add(function.ToGrpcFunctionSchema());
        }

        foreach (FieldSchema field in Schema.Fields)
        {
            Grpc.FieldSchema grpcField = new()
            {
                Name = field.Name,
                DataType = (Grpc.DataType)(int)field.DataType,
                ElementType = field.ElementDataType is { } edt ? (Grpc.DataType)(int)edt : Grpc.DataType.None,
                IsPrimaryKey = field.IsPrimaryKey,
                IsPartitionKey = field.IsPartitionKey,
                AutoID = field.AutoId,
                Description = field.Description,
                Nullable = field.Nullable,
                IsClusteringKey = field.IsClusteringKey
            };

            if (field.DefaultValue is not null)
            {
                grpcField.DefaultValue = ConvertToValueField(field.DefaultValue, field.DataType);
            }

            if (field.EnableAnalyzer)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = Constants.EnableAnalyzer, Value = "true" });
            }

            if (field.EnableMatch)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = Constants.EnableMatch, Value = "true" });
            }

            if (field.AnalyzerParams is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.AnalyzerParams,
                    Value = JsonSerializer.Serialize(field.AnalyzerParams)
                });
            }

            if (field.MultiAnalyzerParams is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.MultiAnalyzerParams,
                    Value = JsonSerializer.Serialize(field.MultiAnalyzerParams)
                });
            }

            if (field.MaxLength is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.VarcharMaxLength,
                    Value = field.MaxLength.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (field.Dimension is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.VectorDim,
                    Value = field.Dimension.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (field.MaxCapacity is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.MaxCapacity,
                    Value = field.MaxCapacity.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            grpcSchema.Fields.Add(grpcField);
        }

        // Struct fields: each sub-field is encoded as a proto FieldSchema whose data type is Array (scalar
        // sub-fields) or ArrayOfVector (vector sub-fields), with the real type in element_type and the struct's
        // max capacity carried as a max_capacity type param. Mirrors the Java/C++ SDK struct encoding.
        foreach (StructFieldSchema structField in Schema.StructFields)
        {
            if (structField.MaxCapacity <= 0)
            {
                throw new ArgumentException(
                    $"Struct field '{structField.Name}' must have a positive MaxCapacity.", nameof(Schema));
            }

            if (structField.Fields.Count == 0)
            {
                throw new ArgumentException(
                    $"Struct field '{structField.Name}' must have at least one sub-field.", nameof(Schema));
            }

            var grpcStruct = new Grpc.StructArrayFieldSchema
            {
                Name = structField.Name,
                Description = structField.Description
            };

            foreach (FieldSchema subField in structField.Fields)
            {
                if (subField.IsPrimaryKey || subField.IsPartitionKey || subField.IsClusteringKey || subField.AutoId
                    || subField.Nullable || subField.DefaultValue is not null)
                {
                    throw new ArgumentException(
                        $"Struct sub-field '{structField.Name}.{subField.Name}' cannot be a primary/partition/" +
                        "clustering key, auto-id, nullable, or have a default value.", nameof(Schema));
                }

                // Struct sub-fields are encoded as Array/ArrayOfVector FieldSchemas; the server does not accept
                // text-analyzer configuration on them. Reject rather than silently dropping the settings so a
                // caller's EnableAnalyzer/EnableMatch/AnalyzerParams/MultiAnalyzerParams cannot be lost.
                if (subField.EnableAnalyzer || subField.EnableMatch
                    || subField.AnalyzerParams is { Count: > 0 } || subField.MultiAnalyzerParams is { Count: > 0 })
                {
                    throw new ArgumentException(
                        $"Struct sub-field '{structField.Name}.{subField.Name}' does not support text analyzers " +
                        "(EnableAnalyzer/EnableMatch/AnalyzerParams/MultiAnalyzerParams).", nameof(Schema));
                }

                Grpc.FieldSchema grpcSubField = new()
                {
                    Name = subField.Name,
                    Description = subField.Description,
                    ElementType = (Grpc.DataType)(int)subField.DataType
                };
                // The server's ArrayOfVector validation only accepts fixed-dimension element types
                // (IsFixDimVectorType); a sparse vector has no dimension and is always rejected server-side, so
                // fail fast here instead of letting the caller hit an opaque server error.
                if (subField.DataType == DataType.SparseFloatVector)
                {
                    throw new ArgumentException(
                        $"Struct sub-field '{structField.Name}.{subField.Name}' does not support " +
                        $"{nameof(DataType.SparseFloatVector)}: the server only accepts fixed-dimension vector " +
                        "element types in a struct.", nameof(Schema));
                }

                bool isVector = IsVectorType(subField.DataType);
                grpcSubField.DataType = isVector ? Grpc.DataType.ArrayOfVector : Grpc.DataType.Array;
                grpcSubField.TypeParams.Add(new Grpc.KeyValuePair { Key = Constants.MaxCapacity, Value = structField.MaxCapacity.ToString(CultureInfo.InvariantCulture) });

                if (subField.MaxLength is { } maxLength)
                {
                    grpcSubField.TypeParams.Add(new Grpc.KeyValuePair { Key = Constants.VarcharMaxLength, Value = maxLength.ToString(CultureInfo.InvariantCulture) });
                }

                if (subField.Dimension is { } dimension)
                {
                    grpcSubField.TypeParams.Add(new Grpc.KeyValuePair { Key = Constants.VectorDim, Value = dimension.ToString(CultureInfo.InvariantCulture) });
                }

                grpcStruct.Fields.Add(grpcSubField);
            }

            grpcSchema.StructArrayFields.Add(grpcStruct);
        }

        var result = new Grpc.CreateCollectionRequest
        {
            CollectionName = CollectionName,
            ConsistencyLevel = (Grpc.ConsistencyLevel)(int)ConsistencyLevel,
            ShardsNum = ShardsNum,
            Schema = grpcSchema.ToByteString()
        };

        foreach (KeyValuePair<string, string> property in Properties)
        {
            result.Properties.Add(new Grpc.KeyValuePair { Key = property.Key, Value = property.Value });
        }

        if (NumPartitions is { } numPartitions)
        {
            result.NumPartitions = numPartitions;
        }

        result.DbName = DatabaseName ?? "";
        return result;
    }

    internal static Grpc.ValueField ConvertToValueField(object value, DataType dataType)
    {
        var result = new Grpc.ValueField();
        switch (dataType)
        {
            case DataType.Bool:
                result.BoolData = (bool)value;
                break;
            case DataType.Int8:
            case DataType.Int16:
            case DataType.Int32:
                result.IntData = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                break;
            case DataType.Int64:
                result.LongData = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                break;
            case DataType.Float:
                result.FloatData = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                break;
            case DataType.Double:
                result.DoubleData = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                break;
            case DataType.VarChar:
            case DataType.String:
                result.StringData = (string)value;
                break;
            case DataType.Json:
                // JSON defaults travel in the string_data slot (Java serializes JsonObject via toString);
                // accept any JSON-serializable value (a dictionary, list, string or primitive) and encode it.
                result.StringData = value is string jsonString
                    ? jsonString
                    : System.Text.Json.JsonSerializer.Serialize(value);
                break;
            case DataType.Timestamptz:
                // Accept an ISO-8601 string (the form used elsewhere for timestamptz values) or an epoch-microsecond
                // number, converting to the proto's int64 microsecond slot. A raw Convert.ToInt64 would throw a
                // FormatException for an ISO string, diverging from the Java SDK.
                result.TimestamptzData = value switch
                {
                    string iso => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)
                        .ToUnixTimeMilliseconds() * 1000,
                    long micros => micros,
                    int intMicros => intMicros,
                    DateTimeOffset dto => dto.ToUnixTimeMilliseconds() * 1000,
                    _ => throw new ArgumentException(
                        $"Timestamptz default value must be an ISO-8601 string or an epoch-microsecond number, got '{value}'.", nameof(value))
                };
                break;
            default:
                throw new NotSupportedException($"Default value is not supported for data type {dataType}");
        }

        return result;
    }

    private static bool IsVectorType(DataType dataType)
        => dataType is DataType.FloatVector or DataType.Float16Vector or DataType.BFloat16Vector
            or DataType.BinaryVector or DataType.Int8Vector or DataType.SparseFloatVector;
}
