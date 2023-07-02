using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using IO.Milvus.ApiSchema;
using System.Text.Json;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task<MilvusMutationResult> InsertAsync(
        string collectionName,
        IList<Field> fields,
        string partitionName = "",
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Insert data {0}", collectionName);

        using HttpRequestMessage request = InsertRequest
            .Create(collectionName, dbName)
            .WithPartitionName(partitionName)
            .WithFields(fields)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            e.Data["responseContent"] = responseContent;
            this._log.LogError(e, "Insert data failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<MilvusMutationResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed insert data: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return MilvusMutationResult.From(data);
    }

    ///<inheritdoc/>
    public async Task<MilvusMutationResult> DeleteAsync(
        string collectionName,
        string expr,
        string partitionName = "",
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Delete data {0}", collectionName);

        using HttpRequestMessage request = DeleteRequest
            .Create(collectionName, expr, dbName)
            .WithPartitionName(partitionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Delete data failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<MilvusMutationResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed Delete data: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return MilvusMutationResult.From(data);
    }

    ///<inheritdoc/>
    public async Task<MilvusSearchResult> SearchAsync(
        MilvusSearchParameters searchParameters,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Search: {0}", searchParameters.ToString());

        using HttpRequestMessage request = searchParameters.BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Search failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<SearchResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Search failed: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return MilvusSearchResult.From(data);
    }

    ///<inheritdoc/>
    public async Task<MilvusCalDistanceResult> CalDistanceAsync(
        MilvusVectors leftVectors,
        MilvusVectors rightVectors,
        MilvusMetricType milvusMetricType,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Cal distance: {0}", leftVectors?.ToString());

        using HttpRequestMessage request = CalcDistanceRequest
            .Create(milvusMetricType)
            .WithLeftVectors(leftVectors)
            .WithRightVectors(rightVectors)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Cal distance failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<CalDistanceResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Cal distance: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return MilvusCalDistanceResult.From(data);
    }

    ///<inheritdoc/>
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Flush: {0}", dbName);

        using HttpRequestMessage request = FlushRequest
            .Create(collectionNames, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Flush failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<FlushResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Flush failed: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return MilvusFlushResult.From(data);
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get persistent segment infos: {0}", collectionName);

        using HttpRequestMessage request = GetPersistentSegmentInfoRequest
            .Create(collectionName, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Get persistent segment infos failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetPersistentSegmentInfoResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get persistent segment infos failed: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.Infos;
    }

    ///<inheritdoc/>
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get flush state: {0}", segmentIds?.ToString());

        using HttpRequestMessage request = GetFlushStateRequest
            .Create(segmentIds)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Get flush state failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetFlushStateResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get flush state failed: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.Flushed;
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
        this._log.LogDebug("Query: {0}", collectionName);

        using HttpRequestMessage request = QueryRequest.Create(collectionName, expr, dbName)
            .WithOutputFields(outputFields)
            .WithPartitionNames(partitionNames)
            .WithConsistencyLevel(consistencyLevel)
            .WithGuaranteeTimestamp(guaranteeTimestamp)
            .WithTravelTimestamp(travelTimestamp)
            .WithOffset(offset)
            .WithLimit(limit)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Query failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<QueryResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Query failed: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return MilvusQueryResult.From(data);
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get query segment info: {0}", collectionName);

        using HttpRequestMessage request = GetQuerySegmentInfoRequest
            .Create(collectionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Get query segment info failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetQuerySegmentInfoResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get query segment info failed: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.Infos;
    }
}
