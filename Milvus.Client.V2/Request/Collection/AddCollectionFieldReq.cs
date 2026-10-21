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

        // Milvus requires every field added to an existing collection to be nullable, unless it carries a
        // default value: the proxy's validateAddedEvolutionField rejects only nullable==false AND
        // default_value==nil, so a non-nullable field with a default is a valid request.
        if (!Field.Nullable && Field.DefaultValue is null)
        {
            throw new ArgumentException(
                $"The field '{Field.Name}' must be nullable or have a default value to be added to an existing " +
                "collection (Milvus rejects added fields with nullable=false and no default).", nameof(Field));
        }

        var grpcField = CreateCollectionReq.ConvertToGrpcFieldSchema(Field);

        var request = new Grpc.AddCollectionFieldRequest { CollectionName = CollectionName, Schema = grpcField.ToByteString() };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
