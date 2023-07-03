using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task DeleteCredentialAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);

        _log.LogDebug("Delete credential {0}", username);

        Grpc.Status response = await _grpcClient.DeleteCredentialAsync(new Grpc.DeleteCredentialRequest()
        {
            Username = username
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Delete credential failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task UpdateCredentialAsync(
        string username,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(oldPassword);
        Verify.NotNullOrWhiteSpace(newPassword);

        _log.LogDebug("Update credential {0}", username);

        Grpc.Status response = await _grpcClient.UpdateCredentialAsync(new Grpc.UpdateCredentialRequest
        {
            NewPassword = Base64Encode(newPassword),
            OldPassword = oldPassword, // TODO: Is it correct that this isn't Base64 encoded?
            Username = username
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Update credential failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task CreateCredentialAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(password);

        _log.LogDebug("Create credential {0}", username);

        ulong timestamp = (ulong)TimestampUtils.GetNowUTCTimestamp();
        Grpc.Status response = await _grpcClient.CreateCredentialAsync(new Grpc.CreateCredentialRequest()
        {
            Username = username,
            Password = Base64Encode(password),
            ModifiedUtcTimestamps = timestamp,
            CreatedUtcTimestamps = timestamp
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Create credential failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IList<string>> ListCredUsersAsync(
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("List credential users");

        Grpc.ListCredUsersRequest request = new Grpc.ListCredUsersRequest();

        Grpc.ListCredUsersResponse response = await _grpcClient.ListCredUsersAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("List credential users failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Usernames;
    }

    #region Private ====================================================================================
    private string Base64Encode(string input)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    }
    #endregion
}
