using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task<MilvusMutationResult> InsertAsync(
        string collectionName, 
        IList<Field> fields,
        string partitionName = "", 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Insert entities to {0}", collectionName);
        Verify.ArgNotNullOrEmpty(collectionName, "Milvus collection name cannot be null or empty");
        Verify.True(fields?.Any() == true, "Fields cannot be null or empty");

        Grpc.InsertRequest request = new Grpc.InsertRequest()
        {
            CollectionName = collectionName,
        };

        if (!string.IsNullOrEmpty(partitionName))
        {
            request.PartitionName = partitionName;
        }
        
        var count = fields.First().RowCount;
        Verify.True(fields.All(p => p.RowCount == count), "Fields length is not same");

        request.FieldsData.AddRange(fields.Select(p => p.ToGrpcFieldData()));

        request.NumRows = (uint)fields.First().RowCount;

        Grpc.MutationResult response = await _grpcClient.InsertAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Insert entities failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusMutationResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<MilvusMutationResult> DeleteAsync(
        string collectionName, 
        string expr, 
        string partitionName = "", 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Delete entities: {0}", collectionName);

        Grpc.DeleteRequest request = ApiSchema.DeleteRequest
            .Create(collectionName, expr)
            .WithPartitionName(partitionName)
            .BuildGrpc();

        Grpc.MutationResult response = await _grpcClient.DeleteAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Delete entities failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusMutationResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<MilvusSearchResult> SearchAsync(
        MilvusSearchParameters searchParameters, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Search: {0}", searchParameters.ToString());

        var request = searchParameters.BuildGrpc();

        var searchResponse = await _grpcClient.SearchAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        return MilvusSearchResult.From(searchResponse);
    }

    ///<inheritdoc/>
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public async Task<IList<object>> CalDiatanceAsync(
        MilvusVectors leftVectors, 
        MilvusVectors rightVectors, 
        MilvusMetricType milvusMetricType, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public async Task<bool> GetFlushStateAsync(
        IList<int> segmentIds, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public async Task<MilvusQueryResult> QueryAsync(
        string collectionName, 
        string expr,
        IList<string> outputFields, 
        IList<string> partitionNames = null, 
        long guarantee_timestamp = 0, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<MilvusQuerySegmentResult> GetQuerySegmentInfoAsync(
        string collectionName)
    {
        throw new NotImplementedException();
    }
}
