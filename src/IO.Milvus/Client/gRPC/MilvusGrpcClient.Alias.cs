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
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create alias {0}, {1}", collectionName);

        Grpc.CreateAliasRequest request = CreateAliasRequest
            .Create(collectionName,alias)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.CreateAliasAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Create alias failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task DropAliasAsync(
        string alias, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Drop alias {0}, {1}", alias);

        Grpc.DropAliasRequest request = DropAliasRequest
            .Create(alias)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.DropAliasAsync(request);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Drop alias failed: {0}, {1}", response.ErrorCode, response.Reason);
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
        this._log.LogDebug("Alter alias {0}, {1}", collectionName);

        Grpc.AlterAliasRequest request = AlterAliasRequest
            .Create(collectionName, alias,dbName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.AlterAliasAsync(request);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Alter alias failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }
}