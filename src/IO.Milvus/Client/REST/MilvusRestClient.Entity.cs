using System;
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
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Insert data {0}", collectionName);

        using HttpRequestMessage request = InsertRequest
            .Create(collectionName)
            .WithPartitionName(partitionName)
            .WithFields(fields)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
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
        string partitionName, 
        CancellationToken cancellationToken)
    {
        this._log.LogDebug("Delete data {0}", collectionName);

        using HttpRequestMessage request = DeleteRequest
            .Create(collectionName,expr)
            .WithPartitionName(partitionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Delete index failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<MilvusMutationResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed describe index: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return MilvusMutationResult.From(data);
    }

    ///<inheritdoc/>
    public async Task<MilvusSearchResult> SearchAsync(
        SearchParameters searchParameters, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
