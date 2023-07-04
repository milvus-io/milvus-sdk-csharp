using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client;

/// <summary>
/// Milvus client
/// </summary>
public interface IMilvusClient : IDisposable
{
    /// <summary>
    /// Ensure to connect to Milvus server before any operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<MilvusHealthState> HealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Base address of Milvus server.
    /// </summary>
    string Address { get; }

    #region Collection
    /// <summary>
    /// Drop a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.(Required).</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DropCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Describe a collection.
    /// </summary>
    /// <param name="collectionName">collectionName</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rename a collection.
    /// </summary>
    /// <param name="oldName">The old collection name.</param>
    /// <param name="newName">The new collection name.</param>
    /// <param name="dbName">Database name, available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task RenameCollectionAsync(
        string oldName,
        string newName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="consistencyLevel">
    /// The consistency level that the collection used, modification is not supported now.</param>
    /// <param name="fieldTypes">field types that represents this collection schema</param>
    /// <param name="shardsNum">Once set, no modification is allowed (Optional).</param>
    /// <param name="enableDynamicField"><see href="https://milvus.io/docs/dynamic_schema.md#JSON-a-new-data-type"/></param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateCollectionAsync(
        string collectionName,
        IList<FieldType> fieldTypes,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session,
        int shardsNum = 1,
        bool enableDynamicField = false,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
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
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasCollectionAsync(
        string collectionName,
        DateTime? dateTime = null,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Release a collection loaded before
    /// </summary>
    /// <param name="collectionName">The collection name you want to release.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReleaseCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The collection name you want to load.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="replicaNumber">The replica number to load, default by 1.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task LoadCollectionAsync(
        string collectionName,
        int replicaNumber = 1,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a collection's statistics
    /// </summary>
    /// <param name="collectionName">The collection name you want get statistics</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IDictionary<string, string>> GetCollectionStatisticsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Show all collections.
    /// </summary>
    /// <param name="collectionNames">
    /// When type is InMemory, will return these collection's inMemory_percentages.(Optional)
    /// </param>
    /// <param name="showType">Decide return Loaded collections or All collections(Optional)</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>

    Task<IList<MilvusCollection>> ShowCollectionsAsync(
        IList<string> collectionNames = null,
        ShowType showType = ShowType.All,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get loading progress of a collection or it's partition.
    /// </summary>
    /// <remarks>
    /// Not support in restful api.
    /// </remarks>
    /// <param name="collectionName">Collection name of milvus.</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<long> GetLoadingProgressAsync(
        string collectionName,
        IList<string> partitionNames = null,
        CancellationToken cancellationToken = default);
    #endregion

    #region Alias
    /// <summary>
    /// Create an alias for a collection name.
    /// </summary>
    /// <param name="collectionName">Collection Name.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an Alias
    /// </summary>
    /// <param name="alias">Alias</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DropAliasAsync(
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Alter an alias
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="alias">Alias</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AlterAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);
    #endregion

    #region Partition
    /// <summary>
    /// Create a partition.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to create.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreatePartitionAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///  Get if a partition exists.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to check.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasPartitionAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Show all partitions.
    /// </summary>
    /// <param name="collectionName">The collection name you want to describe, 
    /// you can pass collection_name or collectionID.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IList<MilvusPartition>> ShowPartitionsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load a group of partitions for search.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionNames">The partition names you want to load.</param>
    /// <param name="replicaNumber">The replicas number you would load, 1 by default.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task LoadPartitionsAsync(
        string collectionName,
        IList<string> partitionNames,
        int replicaNumber = 1,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Release a group of loaded partitions.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionNames">The partition names you want to release.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task ReleasePartitionAsync(
        string collectionName,
        IList<string> partitionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a partition.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to drop.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DropPartitionsAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a partition's statistics.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to collect statistics.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IDictionary<string, string>> GetPartitionStatisticsAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);
    #endregion

    #region Ops
    /// <summary>
    /// Do a manual compaction.
    /// </summary>
    /// <param name="collectionId">Collection Id.</param>
    /// <param name="timeTravel">Time travel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CompactionId</returns>
    Task<long> ManualCompactionAsync(
        long collectionId,
        DateTime? timeTravel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the state of a compaction
    /// </summary>
    /// <param name="compactionId">Collection id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<MilvusCompactionState> GetCompactionStateAsync(
        long compactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the plans of a compaction.
    /// </summary>
    /// <param name="compactionId">Compaction id.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<MilvusCompactionPlans> GetCompactionPlansAsync(
        long compactionId,
        CancellationToken cancellationToken = default);

    //TODO:1.LoadBalance; 2.GetReplicas
    #endregion

    #region Import
    //TODO:
    //1.ListImportTasks
    //2.Import
    //3.GetImportState

    #endregion

    #region Credential
    /// <summary>
    /// Delete a user.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task DeleteCredentialAsync(string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update password for a user.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="oldPassword">Old password.</param>
    /// <param name="newPassword">New password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task UpdateCredentialAsync(
        string username,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a user.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="password">Password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task CreateCredentialAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all users in milvus.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IList<string>> ListCredUsersAsync(
        CancellationToken cancellationToken = default);
    #endregion

    #region Entity
    /// <summary>
    /// Insert rows of data entities into a collection.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fields">Fields</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<MilvusMutationResult> InsertAsync(
        string collectionName,
        IList<Field> fields,
        string partitionName = "",
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete rows of data entities from a collection by given expression.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="expr">A predicate expression outputs a boolean value. <see href="https://milvus.io/docs/boolean.md"/></param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<MilvusMutationResult> DeleteAsync(
        string collectionName,
        string expr,
        string partitionName = "",
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Do a k nearest neighbors search with bool expression.
    /// </summary>
    /// <param name="searchParameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MilvusSearchResult> SearchAsync(
        MilvusSearchParameters searchParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate distance between vectors with Milvus.
    /// </summary>
    /// <remarks>
    /// It's a deny api for zilliz cloud.
    /// </remarks>
    /// <param name="leftVectors">Vectors on the left side of the operator</param>
    /// <param name="rightVectors">Vectors on the right side of the operator</param>
    /// <param name="milvusMetricType"><see cref="MilvusMetricType"/>
    /// <para>
    /// <term>For floating-point vectors:</term> 
    /// </para>
    /// <list type="bullet">
    /// <item>L2 (Euclidean distance)</item>
    /// <item>IP (Inner product)</item>
    /// </list>
    /// <para>
    /// <term>For binary vectors:</term> 
    /// </para>
    /// <list type="bullet">
    /// <item>JACCARD (Jaccard distance)</item>
    /// <item>TANIMOTO (Tanimoto distance)</item>
    /// <item>HAMMING (Hamming distance)</item>
    /// <item>SUPERSTRUCTURE (Superstructure)</item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MilvusCalDistanceResult> CalDistanceAsync(
        MilvusVectors leftVectors,
        MilvusVectors rightVectors,
        MilvusMetricType milvusMetricType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flush a collection's data to disk. Milvus data will be auto flushed.
    /// Flush is only required when you want to get up to date entities numbers in statistics due to some internal mechanism.
    /// It will be removed in the future.
    /// </summary>
    /// <param name="collectionNames">Collection names.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns sealed segments information of a collection.
    /// </summary>
    /// <param name="collectionName">Milvus collection name.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the flush state of multiple segments.
    /// </summary>
    /// <param name="segmentIds">Segment ids</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>If segments flushed.</returns>
    Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Do a explicit record query by given expression. 
    /// For example when you want to query by primary key.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="expr"></param>
    /// <param name="outputFields"></param>
    /// <param name="consistencyLevel"></param>
    /// <param name="partitionNames">Partitions names.(Optional)</param>
    /// <param name="guaranteeTimestamp">
    /// guarantee_timestamp.
    /// (Optional)Instructs server to see insert/delete operations performed before a provided timestamp.
    /// If no such timestamp is specified, the server will wait for the latest operation to finish and query.
    /// </param>
    /// <param name="offset">
    /// offset a value to define the position.
    /// Specify a position to return results. Only take effect when the 'limit' value is specified.
    /// Default value is 0, start from begin.
    /// </param>
    /// <param name="limit">
    /// limit a value to define the limit of returned entities
    /// Specify a value to control the returned number of entities. Must be a positive value.
    /// Default value is 0, will return without limit.
    /// </param>
    /// <param name="travelTimestamp">Travel time.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<MilvusQueryResult> QueryAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get query segment information.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="MilvusQuerySegmentInfoResult"/></returns>
    Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        CancellationToken cancellationToken = default);
    #endregion

    #region Index
    /// <summary>
    /// Create an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name you want to create index.</param>
    /// <param name="fieldName">The vector field name in this particular collection.</param>
    /// <param name="indexName">Index name</param>
    /// <param name="milvusIndexType">Milvus index type.</param>
    /// <param name="milvusMetricType"></param>
    /// <param name="extraParams">
    /// Support keys: index_type,metric_type, params. 
    /// Different index_type may has different params.</param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken"></param>
    Task CreateIndexAsync(
        string collectionName,
        string fieldName,
        string indexName,
        MilvusIndexType milvusIndexType,
        MilvusMetricType milvusMetricType,
        IDictionary<string, string> extraParams = null,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drop an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name you want to drop index.</param>
    /// <param name="fieldName">The vector field name in this particular collection.</param>
    /// <param name="indexName">Index name. The default Index name is <see cref="Constants.DEFAULT_INDEX_NAME"/></param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DropIndexAsync(
        string collectionName,
        string fieldName,
        string indexName = Constants.DEFAULT_INDEX_NAME,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Describe an index
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the build progress of an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index build progress.</returns>
    Task<IndexBuildProgress> GetIndexBuildProgressAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the state of an index.
    /// </summary>
    /// <param name="collectionName">The particular collection name in Milvus</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="dbName">Database name. available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index state.</returns>
    Task<IndexState> GetIndexStateAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);
    #endregion

    #region Metric
    /// <summary>
    /// Get metrics.
    /// </summary>
    /// <param name="request">request is of jsonic format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>metrics from which component.</returns>
    Task<MilvusMetrics> GetMetricsAsync(
        string request,
        CancellationToken cancellationToken = default);
    #endregion

    #region Database
    /// <summary>
    /// Create a database in milvus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="dbName">Database name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task CreateDatabaseAsync(string dbName, CancellationToken cancellationToken = default);

    /// <summary>
    /// List databases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <returns>Databases</returns>
    Task<IEnumerable<string>> ListDatabasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops a database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// <para>
    /// Note that this method drops all data in the database.
    /// </para>
    /// </remarks>
    /// <param name="dbName">Database name.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    Task DropDatabaseAsync(string dbName, CancellationToken cancellationToken = default);
    #endregion

    #region Role
    /// <summary>
    /// Create a role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name that will be created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drop a role.
    /// </summary>
    /// <remarks>
    ///  <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="roleName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DropRoleAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add user to role.
    /// </summary>
    /// <remarks>
    ///<para>
    /// The user will get permissions that the role are allowed to perform operations.
    ///</para>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="username">Username.</param>
    /// <param name="roleName">Role name.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddUserToRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove user from role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The user will remove permissions that the role are allowed to perform operations.
    /// </para>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="username">Username.</param>
    /// <param name="roleName">RoleName.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task RemoveUserFromRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users who are added to the role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name.</param>
    /// <param name="includeUserInfo">Include user information.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    Task<IEnumerable<MilvusRoleResult>> SelectRoleAsync(
        string roleName,
        bool includeUserInfo = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all roles the user has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="username">User name.</param>
    /// <param name="includeRoleInfo">Include user information</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IEnumerable<MilvusUserResult>> SelectUserAsync(
        string username,
        bool includeRoleInfo = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grant Role Privilege.
    /// <see href="https://milvus.io/docs/users_and_roles.md"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name. </param>
    /// <param name="object">object.</param>
    /// <param name="objectName">object name.</param>
    /// <param name="privilege">privilege.</param>
    /// <param name="dbName">Database name.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    Task GrantRolePrivilegeAsync(
        string roleName,
        string @object,
        string objectName,
        string privilege,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke Role Privilege.
    /// <see href="https://milvus.io/docs/users_and_roles.md"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name. </param>
    /// <param name="object">object.</param>
    /// <param name="objectName">object name.</param>
    /// <param name="privilege">privilege.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    Task RevokeRolePrivilegeAsync(
        string roleName,
        string @object,
        string objectName,
        string privilege,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///  List a grant info for the role and the specific object
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name. RoleName cannot be empty or null.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    Task<IEnumerable<MilvusGrantEntity>> SelectGrantForRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List a grant info for the role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="roleName">RoleName cannot be empty or null.</param>
    /// <param name="object">object. object cannot be empty or null.</param>
    /// <param name="objectName">objectName. objectName cannot be empty or null.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    Task<IEnumerable<MilvusGrantEntity>> SelectGrantForRoleAndObjectAsync(
        string roleName,
        string @object,
        string objectName,
        CancellationToken cancellationToken = default);
    #endregion

    /// <summary>
    /// Get Milvus version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Not support <see cref="REST.MilvusRestClient"/>
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Milvus version</returns>
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
}
