#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;
public sealed class DropCollectionFunctionReq
{
    public string CollectionName { get; set; } = "";
    public string FunctionName { get; set; } = "";
    internal Grpc.DropCollectionFunctionRequest ToGrpcDropCollectionFunctionRequest(long collectionId)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FunctionName);
        return new Grpc.DropCollectionFunctionRequest
        {
            CollectionName = CollectionName,
            CollectionID = collectionId,
            FunctionName = FunctionName
        };
    }
}
