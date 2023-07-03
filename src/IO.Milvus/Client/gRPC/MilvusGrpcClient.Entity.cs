using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Insert entities to {0}", collectionName);
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(fields);
        Verify.NotNullOrWhiteSpace(dbName);

        Grpc.InsertRequest request = new Grpc.InsertRequest()
        {
            CollectionName = collectionName,
            DbName = dbName
        };
        if (!string.IsNullOrEmpty(partitionName))
        {
            request.PartitionName = partitionName;
        }

        long count = fields[0].RowCount;
        for (int i = 1; i < fields.Count; i++)
        {
            if (fields[i].RowCount != count)
            {
                throw new ArgumentOutOfRangeException($"{nameof(fields)}[{i}])", "Fields length is not same");
            }
        }

        request.FieldsData.AddRange(fields.Select(p => p.ToGrpcFieldData()));

        request.NumRows = (uint)count;

        Grpc.MutationResult response = await _grpcClient.InsertAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Insert entities failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusMutationResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<MilvusMutationResult> DeleteAsync(
        string collectionName,
        string expr,
        string partitionName = null,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(expr);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Delete entities: {0}", collectionName);

        Grpc.MutationResult response = await _grpcClient.DeleteAsync(new Grpc.DeleteRequest()
        {
            CollectionName = collectionName,
            Expr = expr,
            DbName = dbName,
            PartitionName = !string.IsNullOrEmpty(partitionName) ? partitionName : string.Empty
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Delete entities failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusMutationResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<MilvusSearchResult> SearchAsync(
        MilvusSearchParameters searchParameters,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(searchParameters);
        searchParameters.Validate();

        _log.LogDebug("Search: {0}", searchParameters.ToString());

        var request = searchParameters.BuildGrpc();

        var response = await _grpcClient.SearchAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Delete entities failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusSearchResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(collectionNames);
        Verify.NotNullOrWhiteSpace(dbName);
        _log.LogDebug("Flush: {0}", dbName);

        Grpc.FlushRequest request = new Grpc.FlushRequest()
        {
            DbName = dbName
        };
        request.CollectionNames.AddRange(collectionNames);

        var response = await _grpcClient.FlushAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Flush failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusFlushResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<MilvusCalDistanceResult> CalDistanceAsync(
        MilvusVectors leftVectors,
        MilvusVectors rightVectors,
        MilvusMetricType milvusMetricType,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(leftVectors);
        Verify.NotNull(rightVectors);
        if (milvusMetricType is not (MilvusMetricType.L2 or MilvusMetricType.IP or MilvusMetricType.Hamming or MilvusMetricType.Tanimoto))
        {
            throw new ArgumentOutOfRangeException(nameof(milvusMetricType));
        }

        _log.LogDebug("Cal distance: {0}", leftVectors.ToString());

        Grpc.CalcDistanceRequest request = new Grpc.CalcDistanceRequest()
        {
            OpLeft = leftVectors.ToVectorsArray(),
            OpRight = rightVectors.ToVectorsArray(),
        };
        request.Params.Add(new Grpc.KeyValuePair() { Key = "metric", Value = milvusMetricType.ToString().ToUpperInvariant() });

        Grpc.CalcDistanceResults response = await _grpcClient.CalcDistanceAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Cal distance failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusCalDistanceResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Get persistent segment infos failed: {0}", collectionName);

        Grpc.GetPersistentSegmentInfoResponse response = await _grpcClient.GetPersistentSegmentInfoAsync(new Grpc.GetPersistentSegmentInfoRequest()
        {
            CollectionName = collectionName,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Get persistent segment infos failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusPersistentSegmentInfo.From(response.Infos);
    }

    ///<inheritdoc/>
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Get flush state: {0}", segmentIds?.ToString());
        Verify.NotNullOrEmpty(segmentIds);

        Grpc.GetFlushStateRequest request = new Grpc.GetFlushStateRequest();
        request.SegmentIDs.AddRange(segmentIds);

        Grpc.GetFlushStateResponse response = await _grpcClient.GetFlushStateAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Get flush state: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Flushed;
    }

    ///<inheritdoc/>
    public async Task<MilvusQueryResult> QueryAsync(
        string collectionName,
        string expr,
        IList<string> outputFields,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Bounded,
        IList<string> partitionNames = null,
        long travelTimestamp = 0,
        long guaranteeTimestamp = Constants.GUARANTEE_EVENTUALLY_TS,
        long offset = 0,
        long limit = 0,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(outputFields);
        Verify.NotNullOrWhiteSpace(expr);
        Verify.GreaterThanOrEqualTo(guaranteeTimestamp, 0);
        Verify.GreaterThanOrEqualTo(travelTimestamp, 0);
        Verify.GreaterThanOrEqualTo(offset, 0);
        Verify.GreaterThanOrEqualTo(limit, 0);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Query: {0}", collectionName);

        Grpc.QueryRequest request = new Grpc.QueryRequest()
        {
            CollectionName = collectionName,
            Expr = expr,
            GuaranteeTimestamp = (ulong)guaranteeTimestamp,
            TravelTimestamp = (ulong)travelTimestamp,
            DbName = dbName,
        };
        request.OutputFields.AddRange(outputFields);
        if (partitionNames?.Count > 0)
        {
            request.PartitionNames.AddRange(partitionNames);
        }
        if (offset > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair() { Key = "offset", Value = offset.ToString(CultureInfo.InvariantCulture) });
        }
        if (limit > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair() { Key = "limit", Value = limit.ToString(CultureInfo.InvariantCulture) });
        }

        Grpc.QueryResults response = await _grpcClient.QueryAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Query: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusQueryResult.From(response);
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        _log.LogDebug("Query: {0}", collectionName);

        Grpc.GetQuerySegmentInfoResponse response = await _grpcClient.GetQuerySegmentInfoAsync(new Grpc.GetQuerySegmentInfoRequest()
        {
            CollectionName = collectionName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Query: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusQuerySegmentInfoResult.From(response).ToList();
    }
}
