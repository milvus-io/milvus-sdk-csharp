using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to alter a function on an existing collection.
/// </summary>
public sealed class AlterCollectionFunctionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection containing the function to alter.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the function to alter.
    /// </summary>
    public string FunctionName { get; set; } = "";

    /// <summary>
    /// The new schema of the function.
    /// </summary>
    public FunctionSchema Function { get; set; } = null!;
    internal Grpc.AlterCollectionFunctionRequest ToGrpcAlterCollectionFunctionRequest(long collectionId)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FunctionName);
        Verify.NotNull(Function);
        var request = new Grpc.AlterCollectionFunctionRequest
        {
            CollectionName = CollectionName,
            CollectionID = collectionId,
            FunctionName = FunctionName,
            FunctionSchema = Function.ToGrpcFunctionSchema()
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
