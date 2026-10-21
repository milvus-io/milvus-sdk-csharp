using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to drop a function and its output fields from an existing collection.
/// </summary>
/// <remarks>
/// Unlike <see cref="DropCollectionFunctionReq"/>, this also removes the function's output fields from the
/// collection schema.
/// </remarks>
public sealed class DropFunctionFieldReq
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
    internal Grpc.AlterCollectionSchemaRequest ToGrpcAlterCollectionSchemaRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FunctionName);

        var dropRequest = new Grpc.AlterCollectionSchemaRequest.Types.DropRequest
        {
            FunctionName = FunctionName,
            DropFunctionOutputFields = true
        };

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
