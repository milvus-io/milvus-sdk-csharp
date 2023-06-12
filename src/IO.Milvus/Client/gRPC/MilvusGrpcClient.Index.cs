using IO.Milvus.ApiSchema;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using IO.Milvus.Diagnostics;
using System.Linq;

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
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create index {0}", collectionName);

        Grpc.CreateIndexRequest request = CreateIndexRequest
            .Create(collectionName, fieldName,milvusIndexType, milvusMetricType)
            .WithIndexName(indexName)
            .WithExtraParams(extraParams)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.CreateIndexAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Create index failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task DropIndexAsync(
        string collectionName, 
        string fieldName, 
        string indexName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Drop index {0}", collectionName);

        Grpc.DropIndexRequest request = DropIndexRequest
            .Create(collectionName, fieldName, indexName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.DropIndexAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Drop index failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName, 
        string fieldName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Describe index {0}", collectionName);

        Grpc.DescribeIndexRequest request = DescribeIndexRequest
            .Create(collectionName, fieldName)
            .BuildGrpc();

        Grpc.DescribeIndexResponse response = await _grpcClient.DescribeIndexAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Describe index failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return ToMilvusIndexes(response).ToList();
    }

    ///<inheritdoc/>
    public async Task<IndexBuildProgress> GetIndexBuildProgressAsync(
        string collectionName, 
        string fieldName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get index build progress {0}", collectionName);

        Grpc.GetIndexBuildProgressRequest request = GetIndexBuildProgressRequest
            .Create(collectionName, fieldName)
            .BuildGrpc();

        Grpc.GetIndexBuildProgressResponse response = await _grpcClient.GetIndexBuildProgressAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get index build progress failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return new IndexBuildProgress(response.IndexedRows,response.TotalRows);
    }

    ///<inheritdoc/>
    public async Task<IndexState> GetIndexState(
        string collectionName, 
        string fieldName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get index state {0}, {1}", collectionName, fieldName);

        Grpc.GetIndexStateRequest request = GetIndexStateRequest
            .Create(collectionName, fieldName)
            .BuildGrpc();

        Grpc.GetIndexStateResponse response = await _grpcClient.GetIndexStateAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get index state: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return (IndexState)response.State; ;
    }

    #region Private ================================================================================================================================
    private IEnumerable<MilvusIndex> ToMilvusIndexes(Grpc.DescribeIndexResponse response)
    {
        if (response.IndexDescriptions?.Any() != true)
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
