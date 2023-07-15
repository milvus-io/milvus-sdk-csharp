using IO.Milvus.Grpc;
using IO.Milvus.Utils;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Delete a user.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task DeleteCredentialAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);

        await InvokeAsync(_grpcClient.DeleteCredentialAsync, new DeleteCredentialRequest
        {
            Username = username
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Update password for a user.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="oldPassword">Old password.</param>
    /// <param name="newPassword">New password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task UpdateCredentialAsync(
        string username,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(oldPassword);
        Verify.NotNullOrWhiteSpace(newPassword);

        await InvokeAsync(_grpcClient.UpdateCredentialAsync, new UpdateCredentialRequest
        {
            NewPassword = Base64Encode(newPassword),
            OldPassword = oldPassword, // TODO: Is it correct that this isn't Base64 encoded?
            Username = username
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Create a user.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="password">Password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task CreateCredentialAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(password);

        ulong timestamp = (ulong)TimestampUtils.GetNowUtcTimestamp();
        await InvokeAsync(_grpcClient.CreateCredentialAsync, new CreateCredentialRequest
        {
            Username = username,
            Password = Base64Encode(password),
            ModifiedUtcTimestamps = timestamp,
            CreatedUtcTimestamps = timestamp
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// List all users in milvus.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task<IList<string>> ListCredUsersAsync(
        CancellationToken cancellationToken = default)
    {
        ListCredUsersResponse response = await InvokeAsync(_grpcClient.ListCredUsersAsync, new ListCredUsersRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Usernames;
    }
}
