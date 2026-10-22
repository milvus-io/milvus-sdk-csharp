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
/// partition-key collection. Struct fields (<see cref="CollectionSchema.StructFields" />) can be declared here
/// at creation time or added to an existing collection via
/// <see cref="Milvus.Client.V2.MilvusClientV2.AddCollectionStructFieldAsync" />.
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

        // Fail-fast bounds matching the Java SDK: shards must not be negative (0 means "server default" per the
        // proto contract and sibling SDKs), partitions at least one, and a VarChar primary key's max length
        // within the server cap.
        if (ShardsNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ShardsNum), ShardsNum,
                "ShardsNum must be 0 (server default) or a positive number.");
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
            grpcSchema.Fields.Add(ConvertToGrpcFieldSchema(field));
        }

        // Struct fields: each sub-field is encoded as a proto FieldSchema whose data type is Array (scalar
        // sub-fields) or ArrayOfVector (vector sub-fields), with the real type in element_type and the struct's
        // max capacity carried as a max_capacity type param. Mirrors the Java/C++ SDK struct encoding.
        foreach (StructFieldSchema structField in Schema.StructFields)
        {
            grpcSchema.StructArrayFields.Add(ConvertToGrpcStructFieldSchema(structField, structField.Nullable));
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

    // Encodes a field as a proto FieldSchema: the base properties plus the typed type params (analyzer,
    // max_length, dim, max_capacity) and the caller-supplied TypeParams (skipping keys that map to a typed
    // property). Shared by the create-collection path, AddCollectionField and AddFunctionField so the wire
    // encoding stays in sync as FieldSchema evolves. When isFunctionOutput is set, the field is stamped as a
    // function output field and text-analyzer configuration is rejected (analyzers belong on a function's
    // input text field, not on its vector output field, matching the struct sub-field convention).
    internal static Grpc.FieldSchema ConvertToGrpcFieldSchema(FieldSchema field, bool isFunctionOutput = false)
    {
        var grpcField = new Grpc.FieldSchema
        {
            Name = field.Name,
            DataType = (Grpc.DataType)(int)field.DataType,
            ElementType = field.ElementDataType is { } edt ? (Grpc.DataType)(int)edt : Grpc.DataType.None,
            IsPrimaryKey = field.IsPrimaryKey,
            IsPartitionKey = field.IsPartitionKey,
            AutoID = field.AutoId,
            Description = field.Description,
            Nullable = field.Nullable,
            IsClusteringKey = field.IsClusteringKey,
            IsFunctionOutput = isFunctionOutput
        };

        if (isFunctionOutput && (field.EnableAnalyzer || field.EnableMatch
            || field.AnalyzerParams is { Count: > 0 } || field.MultiAnalyzerParams is { Count: > 0 }))
        {
            throw new ArgumentException(
                $"Function output field '{field.Name}' does not support text analyzers " +
                "(EnableAnalyzer/EnableMatch/AnalyzerParams/MultiAnalyzerParams); analyzers belong on the " +
                "function's input text field.", nameof(field));
        }

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

        // Mirror the C++ FieldSchema::SetTypeParams/AddTypeParam: any additional caller-supplied type params
        // ride along, except keys that map to the typed properties already emitted above.
        foreach (KeyValuePair<string, string> typeParam in field.TypeParams)
        {
            if (typeParam.Key is Constants.VarcharMaxLength or Constants.VectorDim or Constants.MaxCapacity
                or Constants.EnableAnalyzer or Constants.EnableMatch or Constants.AnalyzerParams
                or Constants.MultiAnalyzerParams)
            {
                continue;
            }

            grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = typeParam.Key, Value = typeParam.Value });
        }

        return grpcField;
    }

    // Encodes a struct field as a proto StructArrayFieldSchema. Each sub-field becomes a proto FieldSchema
    // whose data type is Array (scalar sub-fields) or ArrayOfVector (vector sub-fields), with the real type in
    // element_type, the struct's max capacity carried as a max_capacity type param, and the struct's nullable
    // flag applied to every sub-field (a struct is nullable as a whole). Mirrors the Java/C++/Rust SDK struct
    // encoding. Shared by the create-collection path and AddCollectionStructField.
    internal static Grpc.StructArrayFieldSchema ConvertToGrpcStructFieldSchema(StructFieldSchema structField, bool nullable)
    {
        if (structField.MaxCapacity <= 0)
        {
            throw new ArgumentException(
                $"Struct field '{structField.Name}' must have a positive MaxCapacity.", nameof(structField));
        }

        if (structField.Fields.Count == 0)
        {
            throw new ArgumentException(
                $"Struct field '{structField.Name}' must have at least one sub-field.", nameof(structField));
        }

        var grpcStruct = new Grpc.StructArrayFieldSchema
        {
            Name = structField.Name,
            Description = structField.Description,
            Nullable = nullable
        };

        // Struct-level type_params (e.g. warmup, mmap.enabled) ride along verbatim, matching the Java SDK's
        // StructFieldSchema.typeParams and the FieldSchema.TypeParams convention in this repo.
        foreach (KeyValuePair<string, string> typeParam in structField.TypeParams)
        {
            grpcStruct.TypeParams.Add(new Grpc.KeyValuePair { Key = typeParam.Key, Value = typeParam.Value });
        }

        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (FieldSchema subField in structField.Fields)
        {
            if (subField.IsPrimaryKey || subField.IsPartitionKey || subField.IsClusteringKey || subField.AutoId
                || subField.DefaultValue is not null)
            {
                throw new ArgumentException(
                    $"Struct sub-field '{structField.Name}.{subField.Name}' cannot be a primary/partition/" +
                    "clustering key, auto-id, or have a default value.", nameof(structField));
            }

            // A struct is nullable as a whole: a sub-field cannot be nullable independently of the struct.
            if (subField.Nullable && !nullable)
            {
                throw new ArgumentException(
                    $"Struct sub-field '{structField.Name}.{subField.Name}' cannot be nullable; set the " +
                    "struct's Nullable instead.", nameof(structField));
            }

            if (!names.Add(subField.Name))
            {
                throw new ArgumentException(
                    $"Struct field '{structField.Name}' contains duplicate sub-field name '{subField.Name}'.",
                    nameof(structField));
            }

            // Struct sub-fields are encoded as Array/ArrayOfVector FieldSchemas; the server does not accept
            // text-analyzer configuration on them. Reject rather than silently dropping the settings so a
            // caller's EnableAnalyzer/EnableMatch/AnalyzerParams/MultiAnalyzerParams cannot be lost.
            if (subField.EnableAnalyzer || subField.EnableMatch
                || subField.AnalyzerParams is { Count: > 0 } || subField.MultiAnalyzerParams is { Count: > 0 })
            {
                throw new ArgumentException(
                    $"Struct sub-field '{structField.Name}.{subField.Name}' does not support text analyzers " +
                    "(EnableAnalyzer/EnableMatch/AnalyzerParams/MultiAnalyzerParams).", nameof(structField));
            }

            Grpc.FieldSchema grpcSubField = new()
            {
                Name = subField.Name,
                Description = subField.Description,
                ElementType = (Grpc.DataType)(int)subField.DataType,
                Nullable = nullable
            };
            // The server's ArrayOfVector validation only accepts fixed-dimension element types
            // (IsFixDimVectorType); a sparse vector has no dimension and is always rejected server-side, so
            // fail fast here instead of letting the caller hit an opaque server error.
            if (subField.DataType == DataType.SparseFloatVector)
            {
                throw new ArgumentException(
                    $"Struct sub-field '{structField.Name}.{subField.Name}' does not support " +
                    $"{nameof(DataType.SparseFloatVector)}: the server only accepts fixed-dimension vector " +
                    "element types in a struct.", nameof(structField));
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

            // Mirror FieldSchema::TypeParams for struct sub-fields (C++ AddTypeParam), skipping typed keys.
            foreach (KeyValuePair<string, string> typeParam in subField.TypeParams)
            {
                if (typeParam.Key is Constants.VarcharMaxLength or Constants.VectorDim or Constants.MaxCapacity
                    or Constants.EnableAnalyzer or Constants.EnableMatch or Constants.AnalyzerParams
                    or Constants.MultiAnalyzerParams)
                {
                    continue;
                }

                grpcSubField.TypeParams.Add(new Grpc.KeyValuePair { Key = typeParam.Key, Value = typeParam.Value });
            }

            grpcStruct.Fields.Add(grpcSubField);
        }

        return grpcStruct;
    }

    internal static Grpc.ValueField ConvertToValueField(object value, DataType dataType)
    {
        var result = new Grpc.ValueField();
        switch (dataType)
        {
            case DataType.Bool:
                // Coerce like the numeric branches (Convert.ToBoolean(1) == true, ToBoolean("true") == true)
                // instead of a raw (bool) cast that throws InvalidCastException with no field context.
                result.BoolData = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
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
                // Convert like the numeric branches rather than a raw (string) cast; the string_data slot is a
                // proto string, so a non-string default is coerced to its invariant string form.
                result.StringData = Convert.ToString(value, CultureInfo.InvariantCulture)
                    ?? throw new ArgumentException(
                        $"VarChar/String default value cannot be null.", nameof(value));
                break;
            case DataType.Json:
                // JSON defaults travel in the string_data slot (Java serializes JsonObject via toString);
                // accept any JSON-serializable value (a dictionary, list, string or primitive) and encode it.
                // A string default is treated as JSON, so validate it parses rather than writing invalid JSON.
                result.StringData = value switch
                {
                    string jsonString when IsValidJson(jsonString) => jsonString,
                    string => throw new ArgumentException(
                        $"JSON default value '{value}' is not valid JSON.", nameof(value)),
                    _ => System.Text.Json.JsonSerializer.Serialize(value)
                };
                break;
            case DataType.Timestamptz:
                // Accept an ISO-8601 string (the form used elsewhere for timestamptz values) or an epoch-microsecond
                // number, converting to the proto's int64 microsecond slot. A raw Convert.ToInt64 would throw a
                // FormatException for an ISO string, diverging from the Java SDK.
                result.TimestamptzData = value switch
                {
                    string iso => ToUnixMicroseconds(
                        DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)),
                    long micros => micros,
                    int intMicros => intMicros,
                    DateTimeOffset dto => ToUnixMicroseconds(dto),
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

    // The proto ValueField.timestamptz_data is int64 microseconds; convert with full sub-millisecond precision
    // (ToUnixTimeMilliseconds() * 1000 would truncate, e.g. "...123456" -> "...123000").
    private static long ToUnixMicroseconds(DateTimeOffset value)
        => (value - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).Ticks / 10;

    private static bool IsValidJson(string value)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
