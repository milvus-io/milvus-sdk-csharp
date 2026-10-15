using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to drop a user.
/// </summary>
public sealed class DropUserReq
{
    /// <summary>
    /// The name of the user to drop.
    /// </summary>
    public string UserName { get; set; } = "";
    internal Grpc.DeleteCredentialRequest ToGrpcDeleteCredentialRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);
        return new Grpc.DeleteCredentialRequest { Username = UserName };
    }
}
