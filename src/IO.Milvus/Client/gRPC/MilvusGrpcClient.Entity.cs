using IO.Milvus.ApiSchema;
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

        var response = await _grpcClient.SearchAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Delete entities failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusSearchResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Flush: {0}", collectionNames.ToString());
        Verify.True(collectionNames?.Any() == true, "Milvus collection names cannot be null or empty");

        Grpc.FlushRequest request = new Grpc.FlushRequest();
        request.CollectionNames.AddRange(collectionNames);

        var response = await _grpcClient.FlushAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Flush failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusFlushResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<MilvusCalDistanceResult> CalDiatanceAsync(
        MilvusVectors leftVectors, 
        MilvusVectors rightVectors, 
        MilvusMetricType milvusMetricType, 
        CancellationToken cancellationToken)
    {
        this._log.LogDebug("Cal Diatance: {0}", leftVectors.ToString());

        Grpc.CalcDistanceRequest request = CalcDistanceRequest
            .Create(milvusMetricType)
            .WithLeftVectors(leftVectors)
            .WithRightVectors(rightVectors)
            .BuildGrpc();

        Grpc.CalcDistanceResults response = await _grpcClient.CalcDistanceAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Cal Diatance failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusCalDistanceResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get persistent segment infos: {0}", collectionName);

        Grpc.GetPersistentSegmentInfoRequest request = new Grpc.GetPersistentSegmentInfoRequest()
        {
            CollectionName = collectionName,
        };

        Grpc.GetPersistentSegmentInfoResponse response = await _grpcClient.GetPersistentSegmentInfoAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get persistent segment infos: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusPersistentSegmentInfo.From(response.Infos);
    }

    ///<inheritdoc/>
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get flush state: {0}", segmentIds.ToString());
        Verify.True(segmentIds?.Any() == true, "Milvus segment ids cannot be null or empty");

        Grpc.GetFlushStateRequest request = new Grpc.GetFlushStateRequest();
        request.SegmentIDs.AddRange(segmentIds);

        Grpc.GetFlushStateResponse response = await _grpcClient.GetFlushStateAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get flush state: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Flushed;
    }

    ///<inheritdoc/>
    public async Task<MilvusQueryResult> QueryAsync(
        string collectionName, 
        string expr,
        IList<string> outputFields, 
        IList<string> partitionNames = null, 
        long guaranteeTimestamp = 0,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Query: {0}", collectionName);

        Grpc.QueryRequest request = QueryRequest.Create(collectionName, expr)
            .WithOutputFields(outputFields)
            .WithPartitionNames(partitionNames)
            .WithGuaranteeTimestamp(guaranteeTimestamp)
            .BuildGrpc();

        Grpc.QueryResults response = await _grpcClient.QueryAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Query: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusQueryResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get query segment info: {0}", collectionName);

        Grpc.GetQuerySegmentInfoRequest request = GetQuerySegmentInfoRequest
            .Create(collectionName)
            .BuildGrpc();

        Grpc.GetQuerySegmentInfoResponse response = await _grpcClient.GetQuerySegmentInfoAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Query: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }
        
        return MilvusQuerySegmentInfoResult.From(response).ToList();
    }
}
