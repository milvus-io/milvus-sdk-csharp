using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
    public async Task<MilvusMutationResult> InsertAsync(
        string collectionName,
        IList<Field> fields,
        string partitionName = "",
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(fields);
        Verify.NotNullOrWhiteSpace(dbName);

        InsertRequest request = new()
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

        request.FieldsData.AddRange(fields.Select(static p => p.ToGrpcFieldData()));
        request.NumRows = (uint)count;

        MutationResult response = await InvokeAsync(_grpcClient.InsertAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusMutationResult.From(response);
    }

    /// <inheritdoc />
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

        MutationResult response = await InvokeAsync(_grpcClient.DeleteAsync, new DeleteRequest
        {
            CollectionName = collectionName,
            Expr = expr,
            DbName = dbName,
            PartitionName = !string.IsNullOrEmpty(partitionName) ? partitionName : string.Empty
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusMutationResult.From(response);
    }

    /// <inheritdoc />
    public async Task<MilvusSearchResult> SearchAsync(
        MilvusSearchParameters searchParameters,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(searchParameters);
        searchParameters.Validate();

        SearchResults response = await InvokeAsync(_grpcClient.SearchAsync, searchParameters.BuildGrpc(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusSearchResult.From(response);
    }

    /// <inheritdoc />
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(collectionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        FlushRequest request = new()
        {
            DbName = dbName
        };
        request.CollectionNames.AddRange(collectionNames);

        FlushResponse response = await InvokeAsync(_grpcClient.FlushAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusFlushResult.From(response);
    }

    /// <inheritdoc />
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

        CalcDistanceRequest request = new()
        {
            OpLeft = leftVectors.ToVectorsArray(),
            OpRight = rightVectors.ToVectorsArray(),
        };
        request.Params.Add(new Grpc.KeyValuePair() { Key = "metric", Value = milvusMetricType.ToString().ToUpperInvariant() });

        CalcDistanceResults response = await InvokeAsync(_grpcClient.CalcDistanceAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusCalDistanceResult.From(response);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        GetPersistentSegmentInfoResponse response = await InvokeAsync(_grpcClient.GetPersistentSegmentInfoAsync, new GetPersistentSegmentInfoRequest
        {
            CollectionName = collectionName,
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusPersistentSegmentInfo.From(response.Infos);
    }

    /// <inheritdoc />
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(segmentIds);

        GetFlushStateRequest request = new();
        request.SegmentIDs.AddRange(segmentIds);

        GetFlushStateResponse response = await InvokeAsync(_grpcClient.GetFlushStateAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Flushed;
    }

    /// <inheritdoc />
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

        QueryRequest request = new()
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
            Verify.GreaterThan(limit, 0);
            request.QueryParams.Add(new Grpc.KeyValuePair() { Key = "offset", Value = offset.ToString(CultureInfo.InvariantCulture) });
        }
        if (limit > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair() { Key = "limit", Value = limit.ToString(CultureInfo.InvariantCulture) });
        }

        QueryResults response = await InvokeAsync(_grpcClient.QueryAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusQueryResult.From(response);
    }

    /// <inheritdoc />
    public async Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        GetQuerySegmentInfoResponse response = await InvokeAsync(_grpcClient.GetQuerySegmentInfoAsync, new GetQuerySegmentInfoRequest
        {
            CollectionName = collectionName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusQuerySegmentInfoResult.From(response).ToList();
    }
}
