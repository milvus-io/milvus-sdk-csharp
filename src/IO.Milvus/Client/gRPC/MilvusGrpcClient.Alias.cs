using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
    public async Task CreateAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.CreateAliasAsync, new CreateAliasRequest
        {
            CollectionName = collectionName,
            Alias = alias,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DropAliasAsync(
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropAliasAsync, new DropAliasRequest
        {
            Alias = alias,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AlterAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.AlterAliasAsync, new AlterAliasRequest
        {
            CollectionName = collectionName,
            Alias = alias,
            DbName = dbName
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}