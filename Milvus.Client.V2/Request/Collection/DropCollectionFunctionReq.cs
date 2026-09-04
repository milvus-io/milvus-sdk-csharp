using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to drop a function from an existing collection.
/// </summary>
public sealed class DropCollectionFunctionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection containing the function to drop.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the function to drop.
    /// </summary>
    public string FunctionName { get; set; } = "";
    internal Grpc.DropCollectionFunctionRequest ToGrpcDropCollectionFunctionRequest(long collectionId)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FunctionName);
        var request = new Grpc.DropCollectionFunctionRequest
        {
            CollectionName = CollectionName,
            CollectionID = collectionId,
            FunctionName = FunctionName
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
