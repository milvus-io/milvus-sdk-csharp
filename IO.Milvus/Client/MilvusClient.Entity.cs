using System.Buffers;
using System.Buffers.Binary;
using IO.Milvus.Grpc;
using System.Globalization;
using System.Runtime.InteropServices;
using IO.Milvus.Utils;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Insert rows of data entities into a collection.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fields">Fields</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<MilvusMutationResult> InsertAsync(
        string collectionName,
        IList<Field> fields,
        string partitionName = "",
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(fields);

        InsertRequest request = new() { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

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

    /// <summary>
    /// Delete rows of data entities from a collection by given expression.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="expr">A predicate expression outputs a boolean value. <see href="https://milvus.io/docs/boolean.md"/></param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusMutationResult> DeleteAsync(
        string collectionName,
        string expr,
        string? partitionName = null,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(expr);

        var request = new DeleteRequest
        {
            CollectionName = collectionName,
            Expr = expr,
            PartitionName = !string.IsNullOrEmpty(partitionName) ? partitionName : string.Empty
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        MutationResult response =
            await InvokeAsync(_grpcClient.DeleteAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return MilvusMutationResult.From(response);
    }

    /// <summary>
    /// Do a k nearest neighbors search with bool expression.
    /// </summary>
    /// <param name="collectionName">The name of the collection to be searched.</param>
    /// <param name="vectorFieldName">The name of the vector field to search in.</param>
    /// <param name="vectors">The set of vectors to send as input for the similarity search.</param>
    /// <param name="metricType">
    /// Method used to measure the distance between vectors during search. Must correspond to the metric type specified
    /// when building the index.
    /// </param>
    /// <param name="limit">
    /// The maximum number of records to return, also known as 'topk'. Must be between 1 and 16384.
    /// </param>
    /// <param name="searchParameters">Various additional parameters to configure the similarity search.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusSearchResults> SearchAsync<T>(
        string collectionName,
        string vectorFieldName,
        IReadOnlyList<ReadOnlyMemory<T>> vectors,
        MilvusSimilarityMetricType metricType,
        int limit,
        SearchParameters? searchParameters = null,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        searchParameters ??= new();

        Grpc.SearchRequest request = new() { CollectionName = collectionName };

        if (searchParameters.ConsistencyLevel is not null)
        {
            request.ConsistencyLevel = (Grpc.ConsistencyLevel)(int)searchParameters.ConsistencyLevel.Value;
        }

        if (searchParameters.PartitionNames.Count > 0)
        {
            request.PartitionNames.AddRange(searchParameters.PartitionNames);
        }

        if (searchParameters.OutputFields.Count > 0)
        {
            request.OutputFields.AddRange(searchParameters.OutputFields);
        }

        if (searchParameters.TravelTimestamp is not null)
        {
            request.TravelTimestamp = (ulong)searchParameters.TravelTimestamp.Value;
        }

        if (searchParameters.GuaranteeTimestamp is not null)
        {
            request.GuaranteeTimestamp = (ulong)GetGuaranteeTimestamp(
                searchParameters.ConsistencyLevel, searchParameters.GuaranteeTimestamp.Value, 0);
        }

        Grpc.PlaceholderValue placeholderValue = new() { Tag = Constants.VectorTag };

        switch (vectors)
        {
            case IReadOnlyList<ReadOnlyMemory<float>> floatVectors:
                PopulateFloatVectorData(floatVectors, placeholderValue);
                break;
            case IReadOnlyList<ReadOnlyMemory<byte>> binaryVectors:
                PopulateBinaryVectorData(binaryVectors, placeholderValue);
                break;
            default:
                throw new ArgumentException("Only vectors of float or byte are supported", nameof(vectors));
        }

        request.PlaceholderGroup = new Grpc.PlaceholderGroup { Placeholders = { placeholderValue } }.ToByteString();

        request.SearchParams.AddRange(
            new[]
            {
                new Grpc.KeyValuePair { Key = Constants.VectorField, Value = vectorFieldName },
                new Grpc.KeyValuePair { Key = Constants.TopK, Value = limit.ToString(CultureInfo.InvariantCulture) },
                // TODO: Offset
                new Grpc.KeyValuePair { Key = Constants.MetricType, Value = metricType.ToString().ToUpperInvariant() },
                new Grpc.KeyValuePair { Key = Constants.Params, Value = searchParameters.Parameters.Combine() },
            });

        if (searchParameters.RoundDecimal is not null)
        {
            request.SearchParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.RoundDecimal,
                Value = searchParameters.RoundDecimal.Value.ToString(CultureInfo.InvariantCulture)
            });
        }

        if (searchParameters.IgnoreGrowing is not null)
        {
            request.SearchParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.IgnoreGrowing, Value = searchParameters.IgnoreGrowing.Value.ToString()
            });
        }

        request.DslType = Grpc.DslType.BoolExprV1;

        if (searchParameters.Expr is not null)
        {
            request.Dsl = searchParameters.Expr;
        }

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        Grpc.SearchResults response =
            await InvokeAsync(_grpcClient.SearchAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return new MilvusSearchResults
        {
            CollectionName = response.CollectionName,
            FieldsData = response.Results.FieldsData.Select(Field.FromGrpcFieldData).ToList(),
            Ids = MilvusIds.FromGrpc(response.Results.Ids),
            NumQueries = response.Results.NumQueries,
            Scores = response.Results.Scores,
            Limit = response.Results.TopK,
            Limits = response.Results.Topks,
        };

        static long GetGuaranteeTimestamp(
            MilvusConsistencyLevel? consistencyLevel,
            long guaranteeTimestamp,
            long gracefulTime)
        {
            if (consistencyLevel == null)
            {
                return guaranteeTimestamp;
            }

            return consistencyLevel switch
            {
                MilvusConsistencyLevel.Strong => 0L,
                MilvusConsistencyLevel.BoundedStaleness => DateTime.UtcNow.ToUtcTimestamp() - gracefulTime,
                MilvusConsistencyLevel.Eventually => 1L,
                _ => guaranteeTimestamp
            };
        }

        static void PopulateFloatVectorData(
            IReadOnlyList<ReadOnlyMemory<float>> vectors, Grpc.PlaceholderValue placeholderValue)
        {
            placeholderValue.Type = Grpc.PlaceholderType.FloatVector;

            foreach (ReadOnlyMemory<float> milvusVector in vectors)
            {
#if NET6_0_OR_GREATER
                if (BitConverter.IsLittleEndian)
                {
                    placeholderValue.Values.Add(ByteString.CopyFrom(MemoryMarshal.AsBytes(milvusVector.Span)));
                    continue;
                }
#endif

                int length = milvusVector.Length * sizeof(float);

                byte[] bytes = ArrayPool<byte>.Shared.Rent(length);

                for (int i = 0; i < milvusVector.Length; i++)
                {
                    Span<byte> destination = bytes.AsSpan(i * sizeof(float));
                    float f = milvusVector.Span[i];
#if NET6_0_OR_GREATER
                    BinaryPrimitives.WriteSingleLittleEndian(destination, f);
#else
                    if (!BitConverter.IsLittleEndian)
                    {
                        unsafe
                        {
                            int tmp = BinaryPrimitives.ReverseEndianness(*(int*)&f);
                            f = *(float*)&tmp;
                        }
                    }
                    MemoryMarshal.Write(destination, ref f);
#endif
                }

                placeholderValue.Values.Add(ByteString.CopyFrom(bytes.AsSpan(0, length)));

                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        static void PopulateBinaryVectorData(
            IReadOnlyList<ReadOnlyMemory<byte>> vectors, Grpc.PlaceholderValue placeholderValue)
        {
            placeholderValue.Type = Grpc.PlaceholderType.BinaryVector;

            foreach (ReadOnlyMemory<byte> milvusVector in vectors)
            {
                placeholderValue.Values.Add(ByteString.CopyFrom(milvusVector.Span));
            }
        }
    }

    /// <summary>
    /// Flush a collection's data to disk. Milvus data will be auto flushed.
    /// Flush is only required when you want to get up to date entities numbers in statistics due to some internal mechanism.
    /// It will be removed in the future.
    /// </summary>
    /// <param name="collectionNames">Collection names.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(collectionNames);

        FlushRequest request = new();

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        request.CollectionNames.AddRange(collectionNames);

        FlushResponse response = await InvokeAsync(_grpcClient.FlushAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusFlushResult.From(response);
    }

    /// <summary>
    /// Returns sealed segments information of a collection.
    /// </summary>
    /// <param name="collectionName">Milvus collection name.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new GetPersistentSegmentInfoRequest { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        GetPersistentSegmentInfoResponse response = await InvokeAsync(_grpcClient.GetPersistentSegmentInfoAsync,
            request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusPersistentSegmentInfo.From(response.Infos);
    }

    /// <summary>
    /// Get the flush state of multiple segments.
    /// </summary>
    /// <param name="segmentIds">Segment ids</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>If segments flushed.</returns>
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(segmentIds);

        GetFlushStateRequest request = new();
        request.SegmentIDs.AddRange(segmentIds);

        GetFlushStateResponse response =
            await InvokeAsync(_grpcClient.GetFlushStateAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Flushed;
    }

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
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusQueryResult> QueryAsync(
        string collectionName,
        string expr,
        IList<string> outputFields,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.BoundedStaleness,
        IList<string>? partitionNames = null,
        long travelTimestamp = 0,
        long guaranteeTimestamp = Constants.GuaranteeEventuallyTs,
        long offset = 0,
        long limit = 0,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(outputFields);
        Verify.NotNullOrWhiteSpace(expr);
        Verify.GreaterThanOrEqualTo(guaranteeTimestamp, 0);
        Verify.GreaterThanOrEqualTo(travelTimestamp, 0);
        Verify.GreaterThanOrEqualTo(offset, 0);
        Verify.GreaterThanOrEqualTo(limit, 0);

        QueryRequest request = new()
        {
            CollectionName = collectionName,
            Expr = expr,
            GuaranteeTimestamp = (ulong)guaranteeTimestamp,
            TravelTimestamp = (ulong)travelTimestamp,
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        request.OutputFields.AddRange(outputFields);

        if (partitionNames?.Count > 0)
        {
            request.PartitionNames.AddRange(partitionNames);
        }

        if (offset > 0)
        {
            Verify.GreaterThan(limit, 0);
            request.QueryParams.Add(new Grpc.KeyValuePair
            {
                Key = "offset", Value = offset.ToString(CultureInfo.InvariantCulture)
            });
        }

        if (limit > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair
            {
                Key = "limit", Value = limit.ToString(CultureInfo.InvariantCulture)
            });
        }

        QueryResults response =
            await InvokeAsync(_grpcClient.QueryAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return new MilvusQueryResult
        {
            CollectionName = response.CollectionName,
            FieldsData = response.FieldsData.Select(Field.FromGrpcFieldData).ToList()
        };
    }

    /// <summary>
    /// Get query segment information.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns><see cref="MilvusQuerySegmentInfoResult"/></returns>
    public async Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new GetQuerySegmentInfoRequest { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        GetQuerySegmentInfoResponse response =
            await InvokeAsync(_grpcClient.GetQuerySegmentInfoAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return MilvusQuerySegmentInfoResult.From(response).ToList();
    }
}
