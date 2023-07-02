using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task CreateAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Create alias {0}, {1}, {2}", collectionName, alias, dbName);

        Grpc.CreateAliasRequest request = CreateAliasRequest
            .Create(collectionName, alias, dbName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.CreateAliasAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Create alias failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task DropAliasAsync(
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Drop alias {0}, {1}", alias, dbName);

        Grpc.DropAliasRequest request = DropAliasRequest
            .Create(alias, dbName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.DropAliasAsync(request, cancellationToken: cancellationToken);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Drop alias failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task AlterAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Alter alias {0}, {1}, {2}", collectionName, alias, dbName);

        Grpc.AlterAliasRequest request = AlterAliasRequest
            .Create(collectionName, alias, dbName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.AlterAliasAsync(request, cancellationToken: cancellationToken);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Alter alias failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }
}