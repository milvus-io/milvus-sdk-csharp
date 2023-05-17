using IO.Milvus.ApiSchema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client;

/// <summary>
/// Milvus client
/// </summary>
public interface IMilvusClient2
{
    #region Collection
    /// <summary>
    /// Drop a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.(Required).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task DropCollectionAsync(string collectionName,CancellationToken cancellationToken);

    /// <summary>
    /// Describe a collection.
    /// </summary>
    /// <param name="collectionName">collectionName</param>
    /// <param name="collectionId">The collection ID you want to describe.</param>
    /// <param name="dateTime">If time_stamp is not null, will describe collection success when time_stamp >= created collection timestamp, otherwise will throw error.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<DescribeCollectionResponse> DescribeCollectionAsync(string collectionName, int collectionId, DateTime? dateTime = null,CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="consistencyLevel">The consistency level that the collection used, modification is not supported now.</param>
    /// <param name="fieldTypes">fieldtypes that represents this collection shema</param>
    /// <param name="shards_num">Once set, no modification is allowed (Optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public Task CreateCollectionAsync(string collectionName, ConsistencyLevel consistencyLevel, IList<FieldType> fieldTypes,
        int shards_num = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get if a collection's existence
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="dateTime">If time_stamp is not zero, will return true when time_stamp >= created collection timestamp, otherwise will return false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public Task<bool> HasCollectionAsync(string collectionName, DateTime? dateTime, CancellationToken cancellationToken);

    /// <summary>
    /// Release a collection loaded before
    /// </summary>
    /// <param name="collectionName">The collection name you want to release.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task ReleaseCollectionAsync(string collectionName,CancellationToken cancellationToken= default);

    /// <summary>
    /// The collection name you want to load.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="replicNumber">The replica number to load, default by 1.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public Task LoadCollectionAsync(string collectionName, int replicNumber = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a collection's statistics
    /// </summary>
    /// <param name="collectionName">The collection name you want get statistics</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public Task GetCollectionStatisticsAsync(string collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Show all collections
    /// </summary>
    /// <param name="collectionNames">When type is InMemory, will return these collection's inMemory_percentages.(Optional)</param>
    /// <param name="type">Decide return Loaded collections or All collections(Optional)</param>
    /// <returns></returns>
    public Task ShowCollectionsAsync(IList<string> collectionNames = null, int? type = null,CancellationToken cancellationToken = default);
    #endregion
}