using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
    public async Task CreateDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.CreateDatabaseAsync, new CreateDatabaseRequest
        {
            DbName = dbName,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListDatabasesAsync(CancellationToken cancellationToken = default)
    {
        ListDatabasesResponse response = await InvokeAsync(_grpcClient.ListDatabasesAsync, new ListDatabasesRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.DbNames;
    }

    /// <inheritdoc />
    public async Task DropDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropDatabaseAsync, new DropDatabaseRequest
        {
            DbName = dbName,
        }, cancellationToken).ConfigureAwait(false);
    }
}