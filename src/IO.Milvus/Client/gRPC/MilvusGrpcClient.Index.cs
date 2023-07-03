using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
    public async Task CreateIndexAsync(
        string collectionName,
        string fieldName,
        string indexName,
        MilvusIndexType milvusIndexType,
        MilvusMetricType milvusMetricType,
        IDictionary<string, string> extraParams,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.CreateIndexAsync, ApiSchema.CreateIndexRequest
            .Create(collectionName, fieldName, milvusIndexType, milvusMetricType, dbName)
            .WithIndexName(indexName)
            .WithExtraParams(extraParams)
            .BuildGrpc(), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DropIndexAsync(
        string collectionName,
        string fieldName,
        string indexName = Constants.DEFAULT_INDEX_NAME,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(indexName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropIndexAsync, new DropIndexRequest
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            IndexName = indexName,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        DescribeIndexResponse response = await InvokeAsync(_grpcClient.DescribeIndexAsync, new DescribeIndexRequest
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            DbName = dbName,
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        List<MilvusIndex> indexes = new List<MilvusIndex>();
        if (response.IndexDescriptions is not null)
        {
            foreach (IndexDescription index in response.IndexDescriptions)
            {
                indexes.Add(new MilvusIndex(
                    index.FieldName,
                    index.IndexName,
                    index.IndexID,
                    index.Params.ToDictionary()));
            }
        }

        return indexes;
    }

    /// <inheritdoc />
    public async Task<IndexBuildProgress> GetIndexBuildProgressAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        GetIndexBuildProgressResponse response = await InvokeAsync(_grpcClient.GetIndexBuildProgressAsync, new GetIndexBuildProgressRequest
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return new IndexBuildProgress(response.IndexedRows, response.TotalRows);
    }

    /// <inheritdoc />
    public async Task<IndexState> GetIndexStateAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        GetIndexStateResponse response = await InvokeAsync(_grpcClient.GetIndexStateAsync, new GetIndexStateRequest
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return (IndexState)response.State;
    }
}
