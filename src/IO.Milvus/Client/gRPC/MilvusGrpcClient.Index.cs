using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
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

        _log.LogDebug("Create index {0}", collectionName);

        Grpc.CreateIndexRequest request = CreateIndexRequest
            .Create(collectionName, fieldName, milvusIndexType, milvusMetricType, dbName)
            .WithIndexName(indexName)
            .WithExtraParams(extraParams)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.CreateIndexAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Create index failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
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

        _log.LogDebug("Drop index {0}", collectionName);

        Grpc.Status response = await _grpcClient.DropIndexAsync(new Grpc.DropIndexRequest()
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            IndexName = indexName,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Drop index failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Describe index {0}", collectionName);

        Grpc.DescribeIndexResponse response = await _grpcClient.DescribeIndexAsync(new Grpc.DescribeIndexRequest()
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            DbName = dbName,
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Describe index failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return ToMilvusIndexes(response).ToList();
    }

    ///<inheritdoc/>
    public async Task<IndexBuildProgress> GetIndexBuildProgressAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Get index build progress {0}", collectionName);

        Grpc.GetIndexBuildProgressResponse response = await _grpcClient.GetIndexBuildProgressAsync(new Grpc.GetIndexBuildProgressRequest()
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Get index build progress failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return new IndexBuildProgress(response.IndexedRows, response.TotalRows);
    }

    ///<inheritdoc/>
    public async Task<IndexState> GetIndexStateAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Get index state {0}, {1}", collectionName, fieldName);

        Grpc.GetIndexStateResponse response = await _grpcClient.GetIndexStateAsync(new Grpc.GetIndexStateRequest()
        {
            CollectionName = collectionName,
            FieldName = fieldName,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Get index state: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return (IndexState)response.State;
    }

    #region Private =========================================================================
    private static IEnumerable<MilvusIndex> ToMilvusIndexes(Grpc.DescribeIndexResponse response)
    {
        if (response.IndexDescriptions is not { Count: > 0 })
        {
            yield break;
        }

        foreach (var index in response.IndexDescriptions)
        {
            yield return new MilvusIndex(
                index.FieldName,
                index.IndexName,
                index.IndexID,
                index.Params.ToDictionary(p => p.Key, p => p.Value));
        }
    }
    #endregion
}
