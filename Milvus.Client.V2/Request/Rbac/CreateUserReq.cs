#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class CreateUserReq
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    internal Grpc.CreateCredentialRequest ToGrpcCreateCredentialRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);
        Verify.NotNullOrWhiteSpace(Password);
        return new Grpc.CreateCredentialRequest { Username = UserName, Password = Password };
    }
}
