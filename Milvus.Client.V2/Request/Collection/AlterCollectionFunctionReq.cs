#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;
public sealed class AlterCollectionFunctionReq
{
    public string CollectionName { get; set; } = "";
    public string FunctionName { get; set; } = "";
    public FunctionSchema Function { get; set; } = null!;
    internal Grpc.AlterCollectionFunctionRequest ToGrpcAlterCollectionFunctionRequest(long collectionId)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FunctionName);
        Verify.NotNull(Function);
        return new Grpc.AlterCollectionFunctionRequest
        {
            CollectionName = CollectionName,
            CollectionID = collectionId,
            FunctionName = FunctionName,
            FunctionSchema = Function.ToGrpcFunctionSchema()
        };
    }
}
