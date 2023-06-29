using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using System.Text;
using System;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task DeleteCredentialAsync(
    string username,
    CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Delete credential {0}, {1}", username);

        Grpc.DeleteCredentialRequest request = DeleteCredentialRequest
            .Create(username)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.DeleteCredentialAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Delete credential failed: {0}, {1}", response.ErrorCode, response.Reason);
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
        this._log.LogDebug("Update credential {0}, {1}", username);

        Grpc.UpdateCredentialRequest request = UpdateCredentialRequest
            .Create(username,oldPassword, Base64Encode(newPassword))
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.UpdateCredentialAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Update credential failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task CreateCredentialAsync(
        string username, 
        string password, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create credential {0}, {1}", username);

        Grpc.CreateCredentialRequest request = CreateCredentialRequest
            .Create(username, Base64Encode(password))
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.CreateCredentialAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Create credential failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IList<string>> ListCredUsersAsync(
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("List credential users");

        Grpc.ListCredUsersRequest request = new Grpc.ListCredUsersRequest();

        Grpc.ListCredUsersResponse response = await _grpcClient.ListCredUsersAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("List credential users failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
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
