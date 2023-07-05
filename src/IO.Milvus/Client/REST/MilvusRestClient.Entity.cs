using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
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
        InsertRequest.ValidateFields(fields);

        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/entities",
            new InsertRequest
            {
                CollectionName = collectionName,
                DbName = dbName,
                PartitionName = partitionName,
                FieldsData = fields,
                NumRows = fields[0].RowCount,
            });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        MilvusMutationResponse data = JsonSerializer.Deserialize<MilvusMutationResponse>(responseContent);
        ValidateStatus(data.Status);

        return MilvusMutationResult.From(data);
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

        using HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/entities",
            new DeleteRequest 
            {
                CollectionName = collectionName, 
                DbName = dbName, 
                Expr = expr, 
                PartitionName = !string.IsNullOrEmpty(partitionName) ? partitionName : null
            });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        MilvusMutationResponse data = JsonSerializer.Deserialize<MilvusMutationResponse>(responseContent);
        ValidateStatus(data.Status);

        return MilvusMutationResult.From(data);
    }

    /// <inheritdoc />
    public async Task<MilvusSearchResult> SearchAsync(
        MilvusSearchParameters searchParameters,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(searchParameters);
        searchParameters.Validate();

        using HttpRequestMessage request = searchParameters.BuildRest();

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        SearchResponse data = JsonSerializer.Deserialize<SearchResponse>(responseContent);
        ValidateStatus(data.Status);

        return MilvusSearchResult.From(data);
    }

    /// <inheritdoc />
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(collectionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/persist",
            new FlushRequest { CollectionNames = collectionNames, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        FlushResponse data = JsonSerializer.Deserialize<FlushResponse>(responseContent);
        ValidateStatus(data.Status);

        return MilvusFlushResult.From(data);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/persist/segment-info",
            new GetPersistentSegmentInfoRequest { CollectionName = collectionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        GetPersistentSegmentInfoResponse data = JsonSerializer.Deserialize<GetPersistentSegmentInfoResponse>(responseContent);
        ValidateStatus(data.Status);

        return data.Infos;
    }

    /// <inheritdoc />
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(segmentIds);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/persist/state",
            new GetFlushStateRequest { SegmentIds = segmentIds });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        GetFlushStateResponse data = JsonSerializer.Deserialize<GetFlushStateResponse>(responseContent);
        ValidateStatus(data.Status);

        return data.Flushed;
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

        QueryRequest payload = new()
        {
            CollectionName = collectionName,
            DbName = dbName,
            Expr = expr,
            OutFields = outputFields,
            PartitionNames = partitionNames,
            ConsistencyLevel = consistencyLevel,
            GuaranteeTimestamp = guaranteeTimestamp,
            TravelTimestamp = travelTimestamp,
        };
        if (offset > 0)
        {
            Verify.GreaterThan(limit, 0);
            payload.QueryParams.Add("offset", offset.ToString(CultureInfo.InvariantCulture));
        }
        if (limit > 0)
        {
            payload.QueryParams.Add("limit", limit.ToString(CultureInfo.InvariantCulture));
        }

        using HttpRequestMessage request = HttpRequest.CreatePostRequest($"{ApiVersion.V1}/query", payload);

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        QueryResponse data = JsonSerializer.Deserialize<QueryResponse>(responseContent);
        ValidateStatus(data.Status);

        return MilvusQueryResult.From(data);
    }

    /// <inheritdoc />
    public async Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/query-segment-info",
            new GetQuerySegmentInfoRequest { CollectionName = collectionName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        GetQuerySegmentInfoResponse data = JsonSerializer.Deserialize<GetQuerySegmentInfoResponse>(responseContent);
        ValidateStatus(data.Status);

        return data.Infos;
    }
}
