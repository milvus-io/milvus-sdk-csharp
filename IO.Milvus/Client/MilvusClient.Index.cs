using IO.Milvus.Grpc;
using IO.Milvus.Utils;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates an index.
    /// </summary>
    /// <param name="collectionName">The name of the collection for which the index will be created.</param>
    /// <param name="fieldName">The name of the field in the collection for which the index will be created.</param>
    /// <param name="milvusIndexType">The type of the index to be created.</param>
    /// <param name="milvusMetricType"></param>
    /// <param name="extraParams">
    /// Extra parameters specific to each index type; consult the documentation for your index type for more details.
    /// </param>
    /// <param name="indexName">An optional name for the index to be created.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task CreateIndexAsync(string collectionName,
        string fieldName,
        MilvusIndexType? milvusIndexType = null,
        MilvusSimilarityMetricType? milvusMetricType = null,
        IDictionary<string, string>? extraParams = null,
        string? indexName = null,
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

        if (indexName is not null)
        {
            request.IndexName = indexName;
        }

        if (milvusMetricType is not null)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = "metric_type",
                Value = GetGrpcMetricType(milvusMetricType.Value)
            });
        }

        if (milvusIndexType is not null)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = "index_type",
                Value = GetGrpcIndexType(milvusIndexType.Value)
            });
        }

        if (extraParams is not null)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = "params",
                Value = extraParams.Combine()
            });
        }

        await InvokeAsync(_grpcClient.CreateIndexAsync, request, cancellationToken).ConfigureAwait(false);

        static string GetGrpcIndexType(MilvusIndexType indexType)
            => indexType switch
            {
                MilvusIndexType.Invalid => "INVALID",
                MilvusIndexType.Flat => "FLAT",
                MilvusIndexType.IvfFlat => "IVF_FLAT",
                MilvusIndexType.IvfPq => "IVF_PQ",
                MilvusIndexType.IvfSq8 => "IVF_SQ8",
                MilvusIndexType.Hnsw => "HNSW",
                MilvusIndexType.RhnswFlat => "RHNSW_FLAT",
                MilvusIndexType.RhnswPq => "RHNSW_PQ",
                MilvusIndexType.RhnswSq => "RHNSW_SQ",
                MilvusIndexType.Annoy => "ANNOY",
                MilvusIndexType.BinFlat => "BIN_FLAT",
                MilvusIndexType.BinIvfFlat => "BIN_IVF_FLAT",
                MilvusIndexType.AutoIndex => "AUTOINDEX",

                _ => throw new ArgumentOutOfRangeException(nameof(indexType), indexType, null)
            };

        static string GetGrpcMetricType(MilvusSimilarityMetricType similarityMetricType)
            => similarityMetricType switch
            {
                MilvusSimilarityMetricType.Invalid => "INVALID",
                MilvusSimilarityMetricType.L2 => "L2",
                MilvusSimilarityMetricType.Ip => "IP",
                MilvusSimilarityMetricType.Jaccard => "JACCARD",
                MilvusSimilarityMetricType.Tanimoto => "TANIMOTO",
                MilvusSimilarityMetricType.Hamming => "HAMMING",
                MilvusSimilarityMetricType.Superstructure => "SUPERSTRUCTURE",
                MilvusSimilarityMetricType.Substructure => "SUBSTRUCTURE",

                _ => throw new ArgumentOutOfRangeException(nameof(similarityMetricType), similarityMetricType, null)
            };
    }

    /// <summary>
    /// Drops an index.
    /// </summary>
    /// <param name="collectionName">The name of the collection containing the index to be dropped.</param>
    /// <param name="fieldName">The name of the field which has the index to be dropped.</param>
    /// <param name="indexName">An optional name of the index to be dropped.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DropIndexAsync(
        string collectionName,
        string fieldName,
        string? indexName,
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
    /// Describes an index, returning information about it's configuration.
    /// </summary>
    /// <param name="collectionName">The name of the collection containing the index to be described.</param>
    /// <param name="fieldName">The name of the field which has the index to be described.</param>
    /// <param name="indexName">An optional name of the index to be described.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName,
        string fieldName,
        string? indexName = null,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);

        var request = new DescribeIndexRequest { CollectionName = collectionName, FieldName = fieldName };

        if (indexName is not null)
        {
            request.IndexName = indexName;
        }

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
                    index.Params.ToDictionary(static p => p.Key, static p => p.Value)));
            }
        }

        return indexes;
    }

    /// <summary>
    /// Gets the build progress of an index.
    /// </summary>
    /// <param name="collectionName">The name of the collection containing the index.</param>
    /// <param name="fieldName">The name of the field which has the index.</param>
    /// <param name="indexName">An optional name of the index.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>Index build progress.</returns>
    public async Task<IndexBuildProgress> GetIndexBuildProgressAsync(
        string collectionName,
        string fieldName,
        string? indexName = null,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);

        var request = new GetIndexBuildProgressRequest { CollectionName = collectionName, FieldName = fieldName };

        if (indexName is not null)
        {
            request.IndexName = indexName;
        }

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
    /// Gets the state of an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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

        return response.State switch
        {
            Grpc.IndexState.None => IndexState.None,
            Grpc.IndexState.Unissued => IndexState.Unissued,
            Grpc.IndexState.InProgress => IndexState.InProgress,
            Grpc.IndexState.Finished => IndexState.Finished,
            Grpc.IndexState.Failed => IndexState.Failed,
            Grpc.IndexState.Retry => IndexState.Retry,

            _ => throw new InvalidOperationException($"Unknown {nameof(Grpc.IndexState)}: {response.State}")
        };
    }
}
