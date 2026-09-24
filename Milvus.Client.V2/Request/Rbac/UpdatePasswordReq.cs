using System.Text;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to update a user's password.
/// </summary>
public sealed class UpdatePasswordReq
{
    /// <summary>
    /// The name of the user whose password to update.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// The current password of the user.
    /// </summary>
    public string OldPassword { get; set; } = "";

    /// <summary>
    /// The new password for the user.
    /// </summary>
    public string NewPassword { get; set; } = "";

    /// <summary>
    /// An optional description for the user, updated together with the password.
    /// </summary>
    public string? Description { get; set; }

    internal Grpc.UpdateCredentialRequest ToGrpcUpdateCredentialRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);
        Verify.NotNullOrWhiteSpace(OldPassword);
        Verify.NotNullOrWhiteSpace(NewPassword);

        var request = new Grpc.UpdateCredentialRequest
        {
            Username = UserName,
            OldPassword = Base64Encode(OldPassword),
            NewPassword = Base64Encode(NewPassword)
        };

        // description is optional on the wire: when the caller leaves it unset, leave the field cleared so
        // the server preserves the user's existing remark (setting it to "" would wipe it).
        if (Description is not null)
        {
            request.Description = Description;
        }

        return request;
    }

    private static string Base64Encode(string input)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
}
