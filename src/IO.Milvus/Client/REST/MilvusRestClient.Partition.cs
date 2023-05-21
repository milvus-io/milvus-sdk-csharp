using IO.Milvus.ApiSchema;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Linq;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task CreatePartitionAsync(
        string collectionName,
        string partitionName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create partition {0}", collectionName);

        using HttpRequestMessage request = CreatePartitionRequest
            .Create(collectionName, partitionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Create partition failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task<bool> HasPartitionAsync(
        string collectionName,
        string partitionName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Check if {0} partition exists", collectionName);

        using HttpRequestMessage request = HasPartitionRequest
            .Create(collectionName, partitionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Check if {0} partition exists: {1}, {2}", partitionName, e.Message, responseContent);
            throw;
        }

        if (string.IsNullOrEmpty(responseContent) || responseContent == "{}")
            return false;

        var hasCollectionResponse = JsonSerializer.Deserialize<HasPartitionResponse>(responseContent);

        return hasCollectionResponse.Value;
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusPartition>> ShowPartitionsAsync(
        string collectionName,
        CancellationToken cancellationToken)
    {
        this._log.LogDebug("Show {0} partitions", collectionName);

        using HttpRequestMessage request = ShowPartitionsRequest
            .Create(collectionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Failed show partitions: {1}, {2}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<ShowPartitionsResponse>(responseContent);

        if (data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed show partitions: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data
            .ToMilvusPartitions()
            .ToList();
    }

    ///<inheritdoc/>
    public async Task LoadPartitionsAsync(
        string collectionName,
        IList<string> partitionNames,
        int replicaNumber = 1,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create partition {0}", collectionName);

        using HttpRequestMessage request = LoadPartitionsRequest
            .Create(collectionName)
            .WithPartitionNames(partitionNames)
            .WithReplicaNumber(replicaNumber)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Create partition failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task ReleasePartitionAsync(
        string collectionName,
        IList<string> partitionNames,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Release partitions {0}", collectionName);

        using HttpRequestMessage request = ReleasePartitionRequest
            .Create(collectionName)
            .WithPartitionNames(partitionNames)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Release partition failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task DropPartitionsAsync(
        string collectionName,
        string partitionName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Drop partition {0}", collectionName);

        using HttpRequestMessage request = DropPartitionRequest
            .Create(collectionName, partitionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Drop partition failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }
}
