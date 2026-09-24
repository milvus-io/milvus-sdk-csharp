using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to update a user's remark.
/// </summary>
public sealed class UpdateUserReq
{
    /// <summary>
    /// The name of the user to update.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// The new remark of the user.
    /// </summary>
    public string Description { get; set; } = "";

    internal Grpc.UpdateCredentialRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);

        return new Grpc.UpdateCredentialRequest
        {
            Username = UserName,
            Description = Description
        };
    }
}
