using System.Text;

namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="username">The username of the user to be created.</param>
    /// <param name="password">The password of the user to be created..</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task CreateUserAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(password);

        await InvokeAsync(GrpcClient.CreateCredentialAsync, new CreateCredentialRequest
        {
            Username = username,
            Password = Base64Encode(password),
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="username">The username of the user to delete.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task DeleteUserAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);

        await InvokeAsync(GrpcClient.DeleteCredentialAsync, new DeleteCredentialRequest
        {
            Username = username
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the password for a user.
    /// </summary>
    /// <param name="username">The username for which to change the password.</param>
    /// <param name="oldPassword">The user's old password.</param>
    /// <param name="newPassword">The new password to set for the user.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task UpdatePassword(
        string username,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(oldPassword);
        Verify.NotNullOrWhiteSpace(newPassword);

        await InvokeAsync(GrpcClient.UpdateCredentialAsync, new UpdateCredentialRequest
        {
            NewPassword = Base64Encode(newPassword),
            OldPassword = Base64Encode(oldPassword),
            Username = username
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all users.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<IReadOnlyList<string>> ListUsernames(
        CancellationToken cancellationToken = default)
    {
        ListCredUsersResponse response = await InvokeAsync(GrpcClient.ListCredUsersAsync, new ListCredUsersRequest(),
            static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Usernames;
    }

    private static string Base64Encode(string input)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
}
