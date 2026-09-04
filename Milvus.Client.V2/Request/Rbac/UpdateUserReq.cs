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
    /// The new remark of the user. When left <c>null</c>, the server preserves the user's existing remark
    /// (the field is optional on the wire); set a non-null value (including <c>""</c>) to change or clear it.
    /// </summary>
    public string? Description { get; set; }

    internal Grpc.UpdateCredentialRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);

        var request = new Grpc.UpdateCredentialRequest { Username = UserName };

        // description is optional on the wire: when the caller leaves it unset, leave the field cleared so
        // the server preserves the user's existing remark (setting it to "" would wipe it).
        if (Description is not null)
        {
            request.Description = Description;
        }

        return request;
    }
}
