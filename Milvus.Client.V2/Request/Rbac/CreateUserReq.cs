using System.Text;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to create a user.
/// </summary>
public sealed class CreateUserReq
{
    /// <summary>
    /// The name of the user to create.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// The password of the user to create. Sent to the server base64-encoded.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// An optional description for the user.
    /// </summary>
    public string? Description { get; set; }

    internal Grpc.CreateCredentialRequest ToGrpcCreateCredentialRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);
        Verify.NotNullOrWhiteSpace(Password);
        return new Grpc.CreateCredentialRequest
        {
            Username = UserName,
            Password = Base64Encode(Password),
            Description = Description ?? ""
        };
    }

    private static string Base64Encode(string input)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
}
