using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to drop the properties of a field in a collection's schema.
/// </summary>
public sealed class DropCollectionFieldPropertiesReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the field whose properties are to be dropped.
    /// </summary>
    public string FieldName { get; set; } = "";

    /// <summary>
    /// The names of the properties to remove from the field.
    /// </summary>
    public IReadOnlyList<string> DeleteKeys { get; set; } = Array.Empty<string>();

    internal Grpc.AlterCollectionFieldRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FieldName);
        Verify.NotNullOrEmpty(DeleteKeys);

        var request = new Grpc.AlterCollectionFieldRequest
        {
            CollectionName = CollectionName,
            FieldName = FieldName
        };
        request.DeleteKeys.AddRange(DeleteKeys);
        return request;
    }
}
