using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task CreateCredentialAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(password);

        ulong timestamp = (ulong)TimestampUtils.GetNowUTCTimestamp();
        await InvokeAsync(_grpcClient.CreateCredentialAsync, new CreateCredentialRequest
        {
            Username = username,
            Password = Base64Encode(password),
            ModifiedUtcTimestamps = timestamp,
            CreatedUtcTimestamps = timestamp
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IList<string>> ListCredUsersAsync(
        CancellationToken cancellationToken = default)
    {
        ListCredUsersResponse response = await InvokeAsync(_grpcClient.ListCredUsersAsync, new ListCredUsersRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Usernames;
    }
}
