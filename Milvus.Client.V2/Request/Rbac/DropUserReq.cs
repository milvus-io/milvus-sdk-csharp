#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class DropUserReq
{
    public string UserName { get; set; } = "";
    internal Grpc.DeleteCredentialRequest ToGrpcDeleteCredentialRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);
        return new Grpc.DeleteCredentialRequest { Username = UserName };
    }
}
