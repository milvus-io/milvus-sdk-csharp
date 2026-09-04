using Milvus.Client.V2.Types;

using System.Globalization;
using System.Text.Json;

namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a <c>DescribeCollection</c> operation.
/// </summary>
public sealed class DescribeCollectionResp
{
    private DescribeCollectionResp(
        long collectionId, string collectionName, CollectionSchema schema, int shardsNum,
        ConsistencyLevel consistencyLevel, ulong createdTimestamp, ulong updateTimestamp,
        IReadOnlyList<string> aliases, IReadOnlyDictionary<string, string> properties,
        string databaseName, long? numPartitions, ulong createdUtcTimestamp)
    {
        CollectionId = collectionId;
        CollectionName = collectionName;
        Schema = schema;
        ShardsNum = shardsNum;
        ConsistencyLevel = consistencyLevel;
        CreatedTimestamp = createdTimestamp;
        UpdateTimestamp = updateTimestamp;
        Aliases = aliases;
        Properties = properties;
        DatabaseName = databaseName;
        NumPartitions = numPartitions;
        CreatedUtcTimestamp = createdUtcTimestamp;
    }

    internal static DescribeCollectionResp FromGrpc(Grpc.DescribeCollectionResponse response)
    {
        // DescribeCollectionResponse.properties is a proto repeated KeyValuePair, so duplicate keys are legal
        // on the wire; use last-wins indexer assignment (as elsewhere in this file) so a duplicated property
        // does not abort the whole describe.
        var properties = new Dictionary<string, string>();
        foreach (Grpc.KeyValuePair property in response.Properties)
        {
            properties[property.Key] = property.Value;
        }

        return new(
            response.CollectionID,
            response.Schema.Name,
            ConvertSchema(response.Schema),
            response.ShardsNum,
            (ConsistencyLevel)response.ConsistencyLevel,
            response.CreatedTimestamp,
            response.UpdateTimestamp,
            response.Aliases.ToList(),
            properties,
            response.DbName,
            response.NumPartitions == 0 ? null : response.NumPartitions,
            response.CreatedUtcTimestamp);
    }

    /// <summary>
    /// The collection id.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// The collection name.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The collection schema.
    /// </summary>
    public CollectionSchema Schema { get; }

    /// <summary>
    /// The number of shards.
    /// </summary>
    public int ShardsNum { get; }

    /// <summary>
    /// The consistency level of the collection.
    /// </summary>
    public ConsistencyLevel ConsistencyLevel { get; }

    /// <summary>
    /// The hybrid timestamp at which the collection was created.
    /// </summary>
    public ulong CreatedTimestamp { get; }

    /// <summary>
    /// The hybrid timestamp at which the collection's schema was last updated (e.g. by a field or property
    /// change). Matching the C++ SDK's <c>CollectionDesc::UpdateTime()</c>.
    /// </summary>
    public ulong UpdateTimestamp { get; }

    /// <summary>
    /// The database the collection belongs to, as reported by the server.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// The number of default physical partitions, populated for a partition-key collection; <c>null</c> when
    /// not applicable.
    /// </summary>
    public long? NumPartitions { get; }

    /// <summary>
    /// The UTC timestamp at which the collection was created, as reported by the server.
    /// </summary>
    public ulong CreatedUtcTimestamp { get; }

    /// <summary>
    /// The aliases of the collection.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// The collection-level properties (e.g. TTL, consistency override, timezone), as set at creation time or
    /// via <c>AlterCollectionProperties</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; }

    internal static CollectionSchema ConvertSchema(Grpc.CollectionSchema grpcSchema)
    {
        if (grpcSchema is null)
        {
            // An empty/partial response may omit the schema message; degrade instead of aborting the describe.
            return new CollectionSchema();
        }

        var schema = new CollectionSchema
        {
            Name = grpcSchema.Name,
            Description = string.IsNullOrEmpty(grpcSchema.Description) ? null : grpcSchema.Description,
            EnableDynamicFields = grpcSchema.EnableDynamicField
        };

        foreach (Grpc.KeyValuePair property in grpcSchema.Properties)
        {
            schema.Properties[property.Key] = property.Value;
        }

        foreach (Grpc.FunctionSchema grpcFunction in grpcSchema.Functions)
        {
            schema.Functions.Add(FunctionSchema.FromGrpc(grpcFunction));
        }

        foreach (Grpc.FieldSchema grpcField in grpcSchema.Fields)
        {
            var rawTypeParams = new Dictionary<string, string>(StringComparer.Ordinal);
            var field = new FieldSchema(
                grpcField.Name,
                (DataType)grpcField.DataType,
                grpcField.IsPrimaryKey,
                grpcField.AutoID,
                grpcField.IsPartitionKey,
                grpcField.Description)
            {
                ElementDataType = grpcField.ElementType == Grpc.DataType.None ? null : (DataType)grpcField.ElementType,
                Nullable = grpcField.Nullable,
                IsFunctionOutput = grpcField.IsFunctionOutput,
                IsClusteringKey = grpcField.IsClusteringKey,
                IsDynamic = grpcField.IsDynamic,
                FieldId = grpcField.FieldID,
                RawTypeParams = rawTypeParams
            };

            if (grpcField.DefaultValue is not null)
            {
                field.DefaultValue = ConvertDefaultValue(grpcField.DefaultValue, field.DataType);
            }

            foreach (Grpc.KeyValuePair parameter in grpcField.TypeParams)
            {
                rawTypeParams[parameter.Key] = parameter.Value;

                // The kernel does not validate type_params, so a malformed value must degrade to the default
                // rather than abort the whole DescribeCollection (and any SchemaCache population) -- the Java
                // SDK deliberately try/catches these same parses.
                switch (parameter.Key)
                {
                    // Range-check the long parse against int.MaxValue so a malformed oversized value degrades
                    // to the default instead of silently wrapping negative.
                    case "max_length" when long.TryParse(parameter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long maxLength) && maxLength <= int.MaxValue:
                        field.MaxLength = (int)maxLength;
                        break;
                    case "dim" when long.TryParse(parameter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long dim) && dim <= int.MaxValue:
                        field.Dimension = (int)dim;
                        break;
                    case "max_capacity" when long.TryParse(parameter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long maxCapacity) && maxCapacity <= int.MaxValue:
                        field.MaxCapacity = (int)maxCapacity;
                        break;
                    case "enable_analyzer" when bool.TryParse(parameter.Value, out bool enableAnalyzer):
                        field.EnableAnalyzer = enableAnalyzer;
                        break;
                    case "analyzer_params":
                        try
                        {
                            field.AnalyzerParams = DeserializeAnalyzerParams(parameter.Value);
                        }
                        catch (JsonException)
                        {
                            // Malformed analyzer params degrade to null instead of aborting the describe.
                        }

                        break;
                    case "enable_match" when bool.TryParse(parameter.Value, out bool enableMatch):
                        field.EnableMatch = enableMatch;
                        break;
                    case "multi_analyzer_params":
                        try
                        {
                            field.MultiAnalyzerParams = DeserializeAnalyzerParams(parameter.Value);
                        }
                        catch (JsonException)
                        {
                            // Malformed multi-analyzer params degrade to null instead of aborting the describe.
                        }

                        break;
                }
            }

            schema.Fields.Add(field);
        }

        foreach (Grpc.StructArrayFieldSchema grpcStruct in grpcSchema.StructArrayFields)
        {
            var structField = new StructFieldSchema(
                grpcStruct.Name,
                string.IsNullOrEmpty(grpcStruct.Description) ? "" : grpcStruct.Description);

            // Read the struct-level capacity once from the wire (every sub-field carries the same max_capacity
            // type param, but relying on that invariant would let the last sub-field win), parsing a single time.
            string? capacityParam = grpcStruct.Fields
                .SelectMany(f => f.TypeParams)
                .Where(p => p.Key == "max_capacity")
                .Select(p => p.Value)
                .FirstOrDefault();
            if (capacityParam is not null
                && long.TryParse(capacityParam, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedCapacity)
                && parsedCapacity <= int.MaxValue)
            {
                structField.MaxCapacity = (int)parsedCapacity;
            }

            foreach (Grpc.FieldSchema grpcSubField in grpcStruct.Fields)
            {
                // The wire type is Array (scalar) or ArrayOfVector (vector); the real type lives in
                // element_type. A missing element_type is a malformed schema: skip the sub-field rather than
                // silently mislabeling it as an int8 scalar (Int8 is a real type, not an "unknown" sentinel).
                if (grpcSubField.ElementType == Grpc.DataType.None)
                {
                    continue;
                }

                var subField = new FieldSchema(
                    grpcSubField.Name,
                    (DataType)grpcSubField.ElementType,
                    description: string.IsNullOrEmpty(grpcSubField.Description) ? "" : grpcSubField.Description)
                {
                    IsDynamic = grpcSubField.IsDynamic
                };

                foreach (Grpc.KeyValuePair parameter in grpcSubField.TypeParams)
                {
                    switch (parameter.Key)
                    {
                        case "max_length" when long.TryParse(parameter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long maxLength):
                            subField.MaxLength = (int)maxLength;
                            break;
                        case "dim" when long.TryParse(parameter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long dim):
                            subField.Dimension = (int)dim;
                            break;
                        case "max_capacity":
                            // Consumed above for the struct's MaxCapacity; not a sub-field property.
                            break;
                    }
                }

                structField.Fields.Add(subField);
            }

            schema.StructFields.Add(structField);
        }

        return schema;
    }

    private static object? ConvertDefaultValue(Grpc.ValueField value, DataType dataType)
        => dataType switch
        {
            DataType.Bool => value.BoolData,
            DataType.Int8 or DataType.Int16 or DataType.Int32 => value.IntData,
            DataType.Int64 => value.LongData,
            DataType.Float => value.FloatData,
            DataType.Double => value.DoubleData,
            DataType.VarChar or DataType.String => value.StringData,
            // JSON defaults travel in the StringData slot (the create path encodes them as a JSON string, and the
            // proxy round-trips them there); surface the JSON string so write/read stay symmetric.
            DataType.Json => value.StringData,
            // The proxy rewrites timestamptz defaults to the StringData slot as an ISO-8601 string (see Milvus
            // timestamptz.go RewriteTimestampTzDefaultValueToString), so read that first; fall back to the
            // epoch-microsecond slot only when the proxy did not rewrite it.
            DataType.Timestamptz => !string.IsNullOrEmpty(value.StringData)
                ? value.StringData
                : DateTimeOffset.FromUnixTimeMilliseconds(value.TimestamptzData / 1000).ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            _ => null
        };

    // Analyzer params are round-tripped through JSON. Deserialize into the same CLR value types the request
    // accepts (string/long/double/bool and nested dictionaries) so that, unlike raw JsonElement values,
    // comparisons like AnalyzerParams["type"] == "english" work and re-serialization keeps the same shape.
    private static Dictionary<string, object>? DeserializeAnalyzerParams(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind != JsonValueKind.Object
            ? null
            : document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value));
    }

    private static object ConvertJsonValue(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number when element.TryGetInt64(out long longValue) => longValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            _ => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value))
        };
}
