#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;
public sealed class AddCollectionFunctionReq
{
    public string CollectionName { get; set; } = "";
    public FunctionSchema Function { get; set; } = null!;
    internal Grpc.AddCollectionFunctionRequest ToGrpcAddCollectionFunctionRequest(long collectionId)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(Function);
        return new Grpc.AddCollectionFunctionRequest
        {
            CollectionName = CollectionName,
            CollectionID = collectionId,
            FunctionSchema = Function.ToGrpcFunctionSchema()
        };
    }
}
