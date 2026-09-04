#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class UpdatePasswordReq
{
    public string UserName { get; set; } = "";
    public string OldPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    internal Grpc.UpdateCredentialRequest ToGrpcUpdateCredentialRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);
        Verify.NotNullOrWhiteSpace(OldPassword);
        Verify.NotNullOrWhiteSpace(NewPassword);
        return new Grpc.UpdateCredentialRequest { Username = UserName, OldPassword = OldPassword, NewPassword = NewPassword };
    }
}
