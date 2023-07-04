using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using System;
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
    public async Task DropCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/collection", 
            new DropCollectionRequest { CollectionName = collectionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collection",
            new DescribeCollectionRequest { CollectionName = collectionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        DescribeCollectionResponse data = JsonSerializer.Deserialize<DescribeCollectionResponse>(responseContent);
        ValidateStatus(data.Status);

        return data.ToDetailedMilvusCollection();
    }

    /// <inheritdoc />
    public Task RenameCollectionAsync(string oldName, string newName, string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Renaming collections isn't supported via REST.");

    /// <inheritdoc />
    public async Task CreateCollectionAsync(
        string collectionName,
        IList<FieldType> fieldTypes,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session,
        int shardsNum = 1,
        bool enableDynamicField = false,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);
        CreateCollectionRequest.ValidateFieldTypes(fieldTypes);

        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/collection",
            new CreateCollectionRequest
            {
                CollectionName = collectionName,
                DbName = dbName,
                Schema = new CollectionSchema() { Name = collectionName, Fields = fieldTypes, EnableDynamicField = enableDynamicField },
                ShardsNum = shardsNum,
                ConsistencyLevel = consistencyLevel,
            });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task<bool> HasCollectionAsync(
        string collectionName,
        DateTime? dateTime = null,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collection/existence",
            new HasCollectionRequest
            {
                CollectionName = collectionName,
                DbName = dbName,
                Timestamp = dateTime is not null ? dateTime.Value.ToUtcTimestamp() : 0,
            });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        return
            !string.IsNullOrEmpty(responseContent) &&
            responseContent != "{}" &&
            JsonSerializer.Deserialize<HasCollectionResponse>(responseContent).Value;
    }

    /// <inheritdoc />
    public async Task ReleaseCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/collection/load",
            new ReleaseCollectionRequest { CollectionName = collectionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task LoadCollectionAsync(
        string collectionName,
        int replicaNumber = 1,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.GreaterThanOrEqualTo(replicaNumber, 1);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/collection/load",
            new LoadCollectionRequest {  CollectionName = collectionName, DbName = dbName, ReplicaNumber = replicaNumber });

        await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> GetCollectionStatisticsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collection/statistics",
            new GetCollectionStatisticsRequest { CollectionName = collectionName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        return JsonSerializer.Deserialize<GetCollectionStatisticsResponse>(responseContent).Statistics;
    }

    /// <inheritdoc />
    public async Task<IList<MilvusCollection>> ShowCollectionsAsync(
        IList<string> collectionNames = null,
        ShowType showType = ShowType.All,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collections",
            new ShowCollectionsRequest
            {
                DbName = dbName,
                CollectionNames = collectionNames,
                Type = showType,
            });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ShowCollectionsResponse data = JsonSerializer.Deserialize<ShowCollectionsResponse>(responseContent);
        ValidateStatus(data.Status);

        return data.ToCollections().ToList();
    }

    /// <inheritdoc />
    public Task<long> GetLoadingProgressAsync(
        string collectionName,
        IList<string> partitionNames = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"Not supported in Milvus restful API");
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> GetPartitionStatisticsAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/partition/statistics",
            new GetPartitionStatisticsRequest { CollectionName= collectionName, DbName = dbName, PartitionName = partitionName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        GetPartitionStatisticsResponse data = JsonSerializer.Deserialize<GetPartitionStatisticsResponse>(responseContent);
        ValidateStatus(data.Status);

        return data.Stats;
    }
}
