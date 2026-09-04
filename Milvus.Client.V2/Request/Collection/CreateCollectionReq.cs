using System.Globalization;
using System.Text.Json;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to create a collection.
/// </summary>
public sealed class CreateCollectionReq
{
    /// <summary>
    /// The name of the collection to create.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The schema definition for the collection.
    /// </summary>
    public CollectionSchema? Schema { get; set; }

    /// <summary>
    /// The consistency level to be used by the collection. Defaults to <see cref="ConsistencyLevel.Session" />.
    /// </summary>
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Session;

    /// <summary>
    /// Number of the shards for the collection to create.
    /// </summary>
    public int ShardsNum { get; set; } = 1;

    internal Grpc.CreateCollectionRequest ToGrpcCreateCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(Schema);

        Grpc.CollectionSchema grpcSchema = new()
        {
            Name = Schema.Name ?? CollectionName,
            EnableDynamicField = Schema.EnableDynamicFields
        };

        if (Schema.Description is not null)
        {
            grpcSchema.Description = Schema.Description;
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
                Nullable = field.Nullable
            };

            if (field.DefaultValue is not null)
            {
                grpcField.DefaultValue = ConvertToValueField(field.DefaultValue, field.DataType);
            }

            if (field.EnableAnalyzer)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = Constants.EnableAnalyzer, Value = "true" });
            }

            if (field.AnalyzerParams is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.AnalyzerParams,
                    Value = JsonSerializer.Serialize(field.AnalyzerParams)
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

        return new Grpc.CreateCollectionRequest
        {
            CollectionName = CollectionName,
            ConsistencyLevel = (Grpc.ConsistencyLevel)(int)ConsistencyLevel,
            ShardsNum = ShardsNum,
            Schema = grpcSchema.ToByteString()
        };
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
            default:
                throw new NotSupportedException($"Default value is not supported for data type {dataType}");
        }

        return result;
    }
}
