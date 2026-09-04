using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to add a new function to an existing collection.
/// </summary>
public sealed class AddCollectionFunctionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to add the function to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The schema of the function to add.
    /// </summary>
    public FunctionSchema Function { get; set; } = null!;
    internal Grpc.AddCollectionFunctionRequest ToGrpcAddCollectionFunctionRequest(long collectionId)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(Function);
        var request = new Grpc.AddCollectionFunctionRequest
        {
            CollectionName = CollectionName,
            CollectionID = collectionId,
            FunctionSchema = Function.ToGrpcFunctionSchema()
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
