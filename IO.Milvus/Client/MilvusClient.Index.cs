using IO.Milvus.Grpc;
using IO.Milvus.Utils;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Create an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name you want to create index.</param>
    /// <param name="fieldName">The vector field name in this particular collection.</param>
    /// <param name="indexName">Index name</param>
    /// <param name="milvusIndexType">Milvus index type.</param>
    /// <param name="milvusMetricType"></param>
    /// <param name="extraParams">
    /// Support keys: index_type,metric_type, params.
    /// Different index_type may has different params.</param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken"></param>
    public async Task CreateIndexAsync(
        string collectionName,
        string fieldName,
        string indexName,
        MilvusIndexType milvusIndexType,
        MilvusMetricType milvusMetricType,
        IDictionary<string, string> extraParams,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);

        var request = new CreateIndexRequest
        {
            CollectionName = collectionName,
            FieldName = fieldName
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        if (!string.IsNullOrEmpty(indexName))
        {
            request.IndexName = indexName;
        }

        request.ExtraParams.Add(new Grpc.KeyValuePair
        {
            Key = "metric_type",
            Value = GetGrpcMetricType(milvusMetricType)
        });

        request.ExtraParams.Add(new Grpc.KeyValuePair
        {
            Key = "index_type",
            Value = GetGrpcIndexType(milvusIndexType)
        });

        if (extraParams.Count > 0)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = "params",
                Value = extraParams.Combine()
            });
        }

        await InvokeAsync(_grpcClient.CreateIndexAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drop an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name you want to drop index.</param>
    /// <param name="fieldName">The vector field name in this particular collection.</param>
    /// <param name="indexName">Index name. The default Index name is <see cref="Constants.DefaultIndexName"/></param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DropIndexAsync(
        string collectionName,
        string fieldName,
        string indexName = Constants.DefaultIndexName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(indexName);

        var request = new DropIndexRequest
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            IndexName = indexName
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.DropIndexAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Describe an index
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName,
        string fieldName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);

        var request = new DescribeIndexRequest { CollectionName = collectionName, FieldName = fieldName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        DescribeIndexResponse response =
            await InvokeAsync(_grpcClient.DescribeIndexAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        List<MilvusIndex> indexes = new();
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

    /// <summary>
    /// Get the build progress of an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index build progress.</returns>
    public async Task<IndexBuildProgress> GetIndexBuildProgressAsync(
        string collectionName,
        string fieldName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);

        var request = new GetIndexBuildProgressRequest { CollectionName = collectionName, FieldName = fieldName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        GetIndexBuildProgressResponse response =
            await InvokeAsync(_grpcClient.GetIndexBuildProgressAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return new IndexBuildProgress(response.IndexedRows, response.TotalRows);
    }

    /// <summary>
    /// Get the state of an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index state.</returns>
    public async Task<IndexState> GetIndexStateAsync(
        string collectionName,
        string fieldName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);

        var request = new GetIndexStateRequest { CollectionName = collectionName, FieldName = fieldName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        GetIndexStateResponse response =
            await InvokeAsync(_grpcClient.GetIndexStateAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return (IndexState)response.State;
    }
}
