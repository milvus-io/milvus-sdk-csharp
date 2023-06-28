using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task CreateDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(dbName, "Milvus dbName cannot be null or empty");

        this._log.LogDebug("Create database {0}", dbName);
        Grpc.Status response = await _grpcClient.CreateDatabaseAsync(new Grpc.CreateDatabaseRequest()
        {
            DbName = dbName,
        },_callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Create database failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<string>> ListDatabasesAsync(CancellationToken cancellation = default)
    {
        this._log.LogDebug("List databases");

        Grpc.ListDatabasesResponse response = await _grpcClient.ListDatabasesAsync(
            new Grpc.ListDatabasesRequest(), 
            _callOptions.WithCancellationToken(cancellation));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("List databases failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.DbNames;
    }

    ///<inheritdoc/>
    public async Task DropDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(dbName, "Milvus dbName cannot be null or empty");

        this._log.LogDebug("Drop database {0}", dbName);
        var response = await _grpcClient.DropDatabaseAsync(new Grpc.DropDatabaseRequest()
        {
            DbName = dbName,
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Drop database failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }
}