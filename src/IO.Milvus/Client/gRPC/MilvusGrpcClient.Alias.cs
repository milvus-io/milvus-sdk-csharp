using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Create alias {0}, {1}, {2}", collectionName, alias, dbName);

        Grpc.Status response = await _grpcClient.CreateAliasAsync(new Grpc.CreateAliasRequest()
        {
            CollectionName = collectionName,
            Alias = alias,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

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
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Drop alias {0}, {1}", alias, dbName);

        Grpc.Status response = await _grpcClient.DropAliasAsync(new()
        {
            Alias = alias,
            DbName = dbName
        }, cancellationToken: cancellationToken);

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Alter alias {0}, {1}, {2}", collectionName, alias, dbName);

        Grpc.Status response = await _grpcClient.AlterAliasAsync(new()
        {
            CollectionName = collectionName,
            Alias = alias,
            DbName = dbName
        }, cancellationToken: cancellationToken);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Alter alias failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }
}