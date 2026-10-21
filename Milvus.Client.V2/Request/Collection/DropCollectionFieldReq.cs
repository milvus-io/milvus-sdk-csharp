using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to drop a field from an existing collection, identified by either a field name or a
/// field id.
/// </summary>
public sealed class DropCollectionFieldReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to drop the field from.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the field to drop. Mutually exclusive with <see cref="FieldId" />: exactly one of the two must
    /// be provided.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// The id of the field to drop. Must be a positive field id. Mutually exclusive with
    /// <see cref="FieldName" />: exactly one of the two must be provided.
    /// </summary>
    public long? FieldId { get; set; }
    internal Grpc.AlterCollectionSchemaRequest ToGrpcAlterCollectionSchemaRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        bool hasFieldName = !string.IsNullOrWhiteSpace(FieldName);
        bool hasFieldId = FieldId is not null;
        if (hasFieldId && FieldId!.Value <= 0)
        {
            throw new ArgumentException("FieldId must be a positive field id.", nameof(FieldId));
        }

        if (hasFieldName == hasFieldId)
        {
            throw new ArgumentException(
                "Exactly one of FieldName or FieldId must be provided.", nameof(FieldId));
        }

        var dropRequest = new Grpc.AlterCollectionSchemaRequest.Types.DropRequest();
        if (hasFieldName)
        {
            dropRequest.FieldName = FieldName!;
        }
        else
        {
            dropRequest.FieldId = FieldId!.Value;
        }

        var request = new Grpc.AlterCollectionSchemaRequest
        {
            CollectionName = CollectionName,
            Action = new Grpc.AlterCollectionSchemaRequest.Types.Action
            {
                DropRequest = dropRequest
            }
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
