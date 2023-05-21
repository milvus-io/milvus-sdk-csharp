using IO.Milvus.ApiSchema;
using System;
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
    public Task DropCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Describe a collection.
    /// </summary>
    /// <param name="collectionName">collectionName</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="consistencyLevel">
    /// The consistency level that the collection used, modification is not supported now.</param>
    /// <param name="fieldTypes">field types that represents this collection schema</param>
    /// <param name="shards_num">Once set, no modification is allowed (Optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task CreateCollectionAsync(
        string collectionName, 
        IList<FieldType> fieldTypes,
        ConsistencyLevel consistencyLevel = ConsistencyLevel.Session,
        int shards_num = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get if a collection's existence
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="dateTime">
    /// If time_stamp is not zero,
    /// will return true when time_stamp >= created collection timestamp,
    /// otherwise will return false.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<bool> HasCollectionAsync(
        string collectionName, 
        DateTime? dateTime = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Release a collection loaded before
    /// </summary>
    /// <param name="collectionName">The collection name you want to release.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task ReleaseCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken= default);

    /// <summary>
    /// The collection name you want to load.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="replicNumber">The replica number to load, default by 1.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task LoadCollectionAsync(
        string collectionName, 
        int replicNumber = 1, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a collection's statistics
    /// </summary>
    /// <param name="collectionName">The collection name you want get statistics</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<IDictionary<string,string>> GetCollectionStatisticsAsync(
        string collectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Show all collections
    /// </summary>
    /// <param name="collectionNames">
    /// When type is InMemory, will return these collection's inMemory_percentages.(Optional)
    /// </param>
    /// <param name="showType">Decide return Loaded collections or All collections(Optional)</param>
    /// <param name="cancellationToken">Cancellation token.</param>

    public Task<IList<MilvusCollection>> ShowCollectionsAsync(
        IList<string> collectionNames = null, 
        ShowType showType = ShowType.All,
        CancellationToken cancellationToken = default);
    #endregion

    #region Alias
    /// <summary>
    /// Create an alias for a collection name.
    /// </summary>
    /// <param name="collectionName">Collection Name.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task CreateAliasAsync(
        string collectionName,
        string alias, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an Alias
    /// </summary>
    /// <param name="alias">Alias</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task DropAliasAsync(
        string alias,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Alter an alias
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="alias">Alias</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task AlterAliasAsync(
        string collectionName,
        string alias,
        CancellationToken cancellationToken = default);
    #endregion

    #region Partition
    /// <summary>
    /// Create a partition.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreatePartitionAsync(
        string collectionName, 
        string partitionName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    ///  Get if a partition exists.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasPartitionAsync(
        string collectionName, 
        string partitionName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Show all partitions.
    /// </summary>
    /// <param name="collectionName">The collection name you want to describe, 
    /// you can pass collection_name or collectionID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IList<MilvusPartition>> ShowPartitionsAsync(
        string collectionName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Load a group of partitions for search.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionNames">The partition names you want to load.</param>
    /// <param name="replicaNumber">The replicas number you would load, 1 by default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task LoadPartitionsAsync(
        string collectionName, 
        IList<string> partitionNames, 
        int replicaNumber = 1, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Release a group of loaded partitions.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionNames">The partition names you want to release.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task ReleasePartitionAsync(
        string collectionName, 
        IList<string> partitionNames, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a partition.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to drop.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DropPartitionsAsync(
        string collectionName,
        string partitionName, 
        CancellationToken cancellationToken = default);
    #endregion

    #region Ops
    /// <summary>
    /// Do a manual compaction.
    /// </summary>
    /// <param name="collectionId">Collection Id.</param>
    /// <param name="timetravel">Time travel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CompactionId</returns>
    public Task<long> ManualCompactionAsync(
        long collectionId, 
        DateTime? timetravel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the state of a compaction
    /// </summary>
    /// <param name="compactionId">Collection id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public Task<CompactionState> GetCompactionStateAsync(
        long compactionId, 
        CancellationToken cancellationToken = default);
    #endregion

    #region Index
    /// <summary>
    /// Create an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name you want to create index.</param>
    /// <param name="fieldName">The vector field name in this particular collection.</param>
    /// <param name="indexName">Version before 2.0.2 doesn't contain index_name, we use default index name.</param>
    /// <param name="extraParams">
    /// Support keys: index_type,metric_type, params. 
    /// Different index_type may has different params.</param>
    /// <param name="cancellationToken"></param>
    public Task CreateIndexAsync(
        string collectionName,
        string fieldName,
        IDictionary<string, string> extraParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drop an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name you want to drop index.</param>
    /// <param name="fieldName">The vector field name in this particular collection.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task DropIndexAsync(
        string collectionName, 
        string fieldName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Describe an index
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName, 
        string fieldName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the build progress of an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public Task<IndexBuildProgress> GetIndexBuildProgress(
        string collectionName,
        string fieldName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the state of an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public Task<IndexState> GetIndexState(
        string collectionName,
        string fieldName,
        CancellationToken cancellationToken = default);
    #endregion

    #region Entity
    /// <summary>
    /// Insert rows of data entities into a collection.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fields">Fields</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public Task<MilvusMutationResult> InsertAsync(
        string collectionName,
        IList<Field> fields,
        string partitionName = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete rows of data entities from a collection by given expression.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="expr">Expression.</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public Task<MilvusMutationResult> DeleteAsync(
        string collectionName,
        string expr,
        string partitionName = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Do a k nearest neighbors search with bool expression.
    /// </summary>
    /// <param name="searchParameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<MilvusSearchResult> SearchAsync(
        SearchParameters searchParameters, 
        CancellationToken cancellationToken = default); 
    #endregion

    #region Metric
    /// <summary>
    /// Get metrics.
    /// </summary>
    /// <param name="request">request is of jsonic format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>metrics from which component.</returns>
    public Task<MilvusMetrics> GetMetricsAsync(
        string request,
        CancellationToken cancellationToken = default);
    #endregion
}