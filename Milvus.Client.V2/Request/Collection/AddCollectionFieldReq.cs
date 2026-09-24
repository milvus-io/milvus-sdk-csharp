using System.Globalization;
using System.Text.Json;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to add a new field to an existing collection.
/// </summary>
public sealed class AddCollectionFieldReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to add the field to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The schema of the field to add.
    /// </summary>
    public FieldSchema Field { get; set; } = null!;
    internal Grpc.AddCollectionFieldRequest ToGrpcAddCollectionFieldRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(Field);

        // Milvus requires every field added to an existing collection to be nullable, so that old segments
        // without the field can return NULL rather than causing a schema inconsistency at query time
        // (proxy addCollectionFieldTask / alterCollectionSchema reject non-nullable added fields).
        if (!Field.Nullable)
        {
            throw new ArgumentException(
                $"The field '{Field.Name}' must be nullable to be added to an existing collection " +
                "(Milvus rejects added fields with nullable=false).", nameof(Field));
        }

        var grpcField = new Grpc.FieldSchema
        {
            Name = Field.Name,
            DataType = (Grpc.DataType)(int)Field.DataType,
            ElementType = Field.ElementDataType is { } edt ? (Grpc.DataType)(int)edt : Grpc.DataType.None,
            IsPrimaryKey = Field.IsPrimaryKey,
            AutoID = Field.AutoId,
            IsPartitionKey = Field.IsPartitionKey,
            Description = Field.Description,
            Nullable = Field.Nullable,
            IsClusteringKey = Field.IsClusteringKey
        };

        if (Field.DefaultValue is not null)
        {
            grpcField.DefaultValue = CreateCollectionReq.ConvertToValueField(Field.DefaultValue, Field.DataType);
        }

        if (Field.EnableAnalyzer)
        {
            grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = Constants.EnableAnalyzer, Value = "true" });
        }

        if (Field.EnableMatch)
        {
            grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = Constants.EnableMatch, Value = "true" });
        }

        if (Field.AnalyzerParams is not null)
        {
            grpcField.TypeParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.AnalyzerParams,
                Value = JsonSerializer.Serialize(Field.AnalyzerParams)
            });
        }

        if (Field.MultiAnalyzerParams is not null)
        {
            grpcField.TypeParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.MultiAnalyzerParams,
                Value = JsonSerializer.Serialize(Field.MultiAnalyzerParams)
            });
        }

        if (Field.MaxLength is not null)
        {
            grpcField.TypeParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.VarcharMaxLength,
                Value = Field.MaxLength.Value.ToString(CultureInfo.InvariantCulture)
            });
        }

        if (Field.Dimension is not null)
        {
            grpcField.TypeParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.VectorDim,
                Value = Field.Dimension.Value.ToString(CultureInfo.InvariantCulture)
            });
        }

        if (Field.MaxCapacity is not null)
        {
            grpcField.TypeParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.MaxCapacity,
                Value = Field.MaxCapacity.Value.ToString(CultureInfo.InvariantCulture)
            });
        }

        var request = new Grpc.AddCollectionFieldRequest { CollectionName = CollectionName, Schema = grpcField.ToByteString() };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
