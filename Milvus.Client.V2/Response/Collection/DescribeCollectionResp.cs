using Milvus.Client.V2.Types;

using System.Globalization;

namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a <c>DescribeCollection</c> operation.
/// </summary>
public sealed class DescribeCollectionResp
{
    private DescribeCollectionResp(
        long collectionId, string collectionName, CollectionSchema schema, int shardsNum,
        ConsistencyLevel consistencyLevel, ulong createdTimestamp, IReadOnlyList<string> aliases)
    {
        CollectionId = collectionId;
        CollectionName = collectionName;
        Schema = schema;
        ShardsNum = shardsNum;
        ConsistencyLevel = consistencyLevel;
        CreatedTimestamp = createdTimestamp;
        Aliases = aliases;
    }

    internal static DescribeCollectionResp FromGrpc(Grpc.DescribeCollectionResponse response)
        => new(
            response.CollectionID,
            response.Schema.Name,
            ConvertSchema(response.Schema),
            response.ShardsNum,
            (ConsistencyLevel)response.ConsistencyLevel,
            response.CreatedTimestamp,
            response.Aliases.ToList());

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
    /// The aliases of the collection.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    internal static CollectionSchema ConvertSchema(Grpc.CollectionSchema grpcSchema)
    {
        var schema = new CollectionSchema
        {
            Name = grpcSchema.Name,
            Description = string.IsNullOrEmpty(grpcSchema.Description) ? null : grpcSchema.Description,
            EnableDynamicFields = grpcSchema.EnableDynamicField
        };

        foreach (Grpc.FieldSchema grpcField in grpcSchema.Fields)
        {
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
                IsFunctionOutput = grpcField.IsFunctionOutput
            };

            if (grpcField.DefaultValue is not null)
            {
                field.DefaultValue = ConvertDefaultValue(grpcField.DefaultValue, field.DataType);
            }

            foreach (Grpc.KeyValuePair parameter in grpcField.TypeParams)
            {
                switch (parameter.Key)
                {
                    case "max_length":
                        field.MaxLength = int.Parse(parameter.Value, CultureInfo.InvariantCulture);
                        break;
                    case "dim":
                        field.Dimension = int.Parse(parameter.Value, CultureInfo.InvariantCulture);
                        break;
                    case "max_capacity":
                        field.MaxCapacity = int.Parse(parameter.Value, CultureInfo.InvariantCulture);
                        break;
                    case "enable_analyzer":
                        field.EnableAnalyzer = bool.Parse(parameter.Value);
                        break;
                    case "analyzer_params":
                        field.AnalyzerParams = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parameter.Value);
                        break;
                }
            }

            schema.Fields.Add(field);
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
            _ => null
        };
}
