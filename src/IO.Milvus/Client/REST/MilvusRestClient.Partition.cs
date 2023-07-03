using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    /// <inheritdoc />
    public async Task CreatePartitionAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/partition",
            new CreatePartitionRequest { CollectionName = collectionName, PartitionName = partitionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task<bool> HasPartitionAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/partition/existence", 
            new HasPartitionRequest { CollectionName = collectionName, PartitionName = partitionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        return
            !string.IsNullOrEmpty(responseContent) &&
            responseContent != "{}" &&
            JsonSerializer.Deserialize<HasPartitionResponse>(responseContent).Value;
    }

    /// <inheritdoc />
    public async Task<IList<MilvusPartition>> ShowPartitionsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/partitions", 
            new ShowPartitionsRequest { CollectionName = collectionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ShowPartitionsResponse data = JsonSerializer.Deserialize<ShowPartitionsResponse>(responseContent);
        ValidateStatus(data.Status);

        return data
            .ToMilvusPartitions()
            .ToList();
    }

    /// <inheritdoc />
    public async Task LoadPartitionsAsync(
        string collectionName,
        IList<string> partitionNames,
        int replicaNumber = 1,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);
        Verify.GreaterThanOrEqualTo(replicaNumber, 1);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/partitions/load", 
            new LoadPartitionsRequest { CollectionName = collectionName, PartitionNames = partitionNames, ReplicaNumber = replicaNumber, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task ReleasePartitionAsync(
        string collectionName,
        IList<string> partitionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/partitions/load", 
            new ReleasePartitionRequest { CollectionName = collectionName, PartitionNames = partitionNames, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task DropPartitionsAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/partition", 
            new DropPartitionRequest { CollectionName = collectionName, PartitionName = partitionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }
}
