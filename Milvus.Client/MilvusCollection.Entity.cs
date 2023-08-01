using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Milvus.Client;

public partial class MilvusCollection
{
    /// <summary>
    /// Inserts rows of data into a collection.
    /// </summary>
    /// <param name="data">The field data to insert; each field contains a list of row values.</param>
    /// <param name="partitionName">An optional name of a partition to insert into.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<MilvusMutationResult> InsertAsync(
        IReadOnlyList<FieldData> data,
        string? partitionName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(data);

        InsertRequest request = new() { CollectionName = Name };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        if (partitionName is not null)
        {
            request.PartitionName = partitionName;
        }

        long count = data[0].RowCount;
        foreach (FieldData field in data)
        {
            if (field.RowCount != count)
            {
                throw new MilvusException("All fields must have the same number of rows.");
            }

            request.FieldsData.Add(field.ToGrpcFieldData());
        }

        request.NumRows = (uint)count;

        MutationResult response =
            await _client.InvokeAsync(_client.GrpcClient.InsertAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        _client.CollectionLastMutationTimestamps[Name] = response.Timestamp;

        return new MilvusMutationResult(response);
    }

    /// <summary>
    /// Deletes rows from a collection by given expression.
    /// </summary>
    /// <param name="expression">A boolean expression determining which rows are to be deleted.</param>
    /// <param name="partitionName">An optional name of a partition from which rows are to be deleted..</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<MilvusMutationResult> DeleteAsync(
        string expression,
        string? partitionName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(expression);

        var request = new DeleteRequest
        {
            CollectionName = Name,
            Expr = expression,
            PartitionName = !string.IsNullOrEmpty(partitionName) ? partitionName : string.Empty
        };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        MutationResult response =
            await _client.InvokeAsync(_client.GrpcClient.DeleteAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        _client.CollectionLastMutationTimestamps[Name] = response.Timestamp;

        return new MilvusMutationResult(response);
    }

    /// <summary>
    /// Perform a vector similarity search.
    /// </summary>
    /// <param name="vectorFieldName">The name of the vector field to search in.</param>
    /// <param name="vectors">The set of vectors to send as input for the similarity search.</param>
    /// <param name="metricType">
    /// Method used to measure the distance between vectors during search. Must correspond to the metric type specified
    /// when building the index.
    /// </param>
    /// <param name="limit">
    /// The maximum number of records to return, also known as 'topk'. Must be between 1 and 16384.
    /// </param>
    /// <param name="parameters">
    /// Various additional optional parameters to configure the similarity search.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusSearchResults> SearchAsync<T>(
        string vectorFieldName,
        IReadOnlyList<ReadOnlyMemory<T>> vectors,
        MilvusSimilarityMetricType metricType,
        int limit,
        SearchParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Grpc.SearchRequest request = new()
        {
            CollectionName = Name,
            DslType = Grpc.DslType.BoolExprV1
        };

        if (parameters is not null)
        {
            if (parameters.PartitionNamesInternal?.Count > 0)
            {
                request.PartitionNames.AddRange(parameters.PartitionNamesInternal);
            }

            if (parameters.OutputFieldsInternal?.Count > 0)
            {
                request.OutputFields.AddRange(parameters.OutputFieldsInternal);
            }

            if (parameters.Expression is not null)
            {
                request.Dsl = parameters.Expression;
            }

            if (parameters.TimeTravelTimestamp is not null)
            {
                request.TravelTimestamp = parameters.TimeTravelTimestamp.Value;
            }

            if (parameters.Offset is not null)
            {
                request.SearchParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.Offset,
                    Value = parameters.Offset.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (parameters.RoundDecimal is not null)
            {
                request.SearchParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.RoundDecimal,
                    Value = parameters.RoundDecimal.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (parameters.IgnoreGrowing is not null)
            {
                request.SearchParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.IgnoreGrowing, Value = parameters.IgnoreGrowing.Value.ToString()
                });
            }
        }

        // Note that we send both the consistency level and the guarantee timestamp, although the latter is derived
        // from the former and should be sufficient. TODO: Confirm this.
        if (parameters?.ConsistencyLevel is null)
        {
            if (parameters?.GuaranteeTimestamp is null)
            {
                request.UseDefaultConsistency = true;
            }
            else
            {
                request.ConsistencyLevel = Grpc.ConsistencyLevel.Customized;
                request.GuaranteeTimestamp = parameters.GuaranteeTimestamp.Value;
            }
        }
        else
        {
            request.ConsistencyLevel = (Grpc.ConsistencyLevel)parameters.ConsistencyLevel.Value;
            request.GuaranteeTimestamp =
                CalculateGuaranteeTimestamp(
                    Name, parameters.ConsistencyLevel.Value, parameters.GuaranteeTimestamp);
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
                new Grpc.KeyValuePair { Key = Constants.MetricType, Value = metricType.ToString().ToUpperInvariant() },
                new Grpc.KeyValuePair
                {
                    Key = Constants.Params,
                    Value = parameters is null ? "{}" : Combine(parameters.Parameters)
                }
            });


        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        Grpc.SearchResults response =
            await _client.InvokeAsync(_client.GrpcClient.SearchAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return new MilvusSearchResults
        {
            CollectionName = response.CollectionName,
            FieldsData = response.Results.FieldsData.Select(FieldData.FromGrpcFieldData).ToList(),
            Ids = MilvusIds.FromGrpc(response.Results.Ids),
            NumQueries = response.Results.NumQueries,
            Scores = response.Results.Scores,
            Limit = response.Results.TopK,
            Limits = response.Results.Topks,
        };

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
    /// Flushes collection data to disk, required only in order to get up-to-date statistics.
    /// </summary>
    /// <remarks>
    /// This method will be removed in a future version.
    /// </remarks>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public Task<MilvusFlushResult> FlushAsync(CancellationToken cancellationToken = default)
        => _client.FlushAsync(new[] { Name }, DatabaseName, cancellationToken);

    /// <summary>
    /// Returns sealed segments information of a collection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<IReadOnlyList<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new GetPersistentSegmentInfoRequest { CollectionName = Name };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        GetPersistentSegmentInfoResponse response = await _client.InvokeAsync(
            _client.GrpcClient.GetPersistentSegmentInfoAsync,
            request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Infos.Select(i => new MilvusPersistentSegmentInfo(
                i.CollectionID,
                i.PartitionID,
                i.SegmentID,
                i.NumRows,
                i.State))
            .ToList();
    }

    /// <summary>
    /// Retrieves rows from a collection via scalar filtering based on a boolean expression
    /// </summary>
    /// <param name="expression">A boolean expression determining which rows are to be returned.</param>
    /// <param name="parameters">Various additional optional parameters to configure the query.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<IReadOnlyList<FieldData>> QueryAsync(
        string expression,
        QueryParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(expression);

        QueryRequest request = new()
        {
            CollectionName = Name,
            Expr = expression
        };

        if (parameters is not null)
        {
            if (parameters.TimeTravelTimestamp is not null)
            {
                request.TravelTimestamp = parameters.TimeTravelTimestamp.Value;
            }

            if (parameters.PartitionNamesInternal?.Count > 0)
            {
                request.PartitionNames.AddRange(parameters.PartitionNamesInternal);
            }

            if (parameters.OutputFieldsInternal?.Count > 0)
            {
                request.OutputFields.AddRange(parameters.OutputFieldsInternal);
            }

            if (parameters.Offset is not null)
            {
                request.QueryParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.Offset,
                    Value = parameters.Offset.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (parameters.Limit is not null)
            {
                request.QueryParams.Add(new Grpc.KeyValuePair
                {
                    Key = "limit", Value = parameters.Limit.Value.ToString(CultureInfo.InvariantCulture)
                });
            }
        }

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        // Note that we send both the consistency level and the guarantee timestamp, although the latter is derived
        // from the former and should be sufficient. TODO: Confirm this.
        if (parameters?.ConsistencyLevel is null)
        {
            if (parameters?.GuaranteeTimestamp is null)
            {
                request.UseDefaultConsistency = true;
            }
            else
            {
                request.ConsistencyLevel = Grpc.ConsistencyLevel.Customized;
                request.GuaranteeTimestamp = parameters.GuaranteeTimestamp.Value;
            }
        }
        else
        {
            request.ConsistencyLevel = (Grpc.ConsistencyLevel)parameters.ConsistencyLevel.Value;
            request.GuaranteeTimestamp =
                CalculateGuaranteeTimestamp(Name, parameters.ConsistencyLevel.Value,
                    parameters.GuaranteeTimestamp);
        }

        QueryResults response =
            await _client.InvokeAsync(_client.GrpcClient.QueryAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.FieldsData.Select(FieldData.FromGrpcFieldData).ToList();
    }

    /// <summary>
    /// Get query segment information.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns><see cref="MilvusQuerySegmentInfoResult"/></returns>
    public async Task<IReadOnlyList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new GetQuerySegmentInfoRequest { CollectionName = Name };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        GetQuerySegmentInfoResponse response =
            await _client.InvokeAsync(_client.GrpcClient.GetQuerySegmentInfoAsync, request, static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        return response.Infos.Select(i => new MilvusQuerySegmentInfoResult(
                i.CollectionID, i.IndexName, i.IndexID, i.MemSize, i.NodeID, i.NumRows, i.PartitionID, i.SegmentID,
                (MilvusSegmentState)i.State))
            .ToList();
    }

    /// <summary>
    /// Flush and polls Milvus for the flush state util it is fully flush.
    /// </summary>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">How long to poll for before throwing a <see cref="TimeoutException" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task WaitForFlushAsync(
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        MilvusFlushResult response = await FlushAsync(cancellationToken).ConfigureAwait(false);

        foreach (IReadOnlyList<long> ids in response.CollSegIDs.Values.Where(p => p.Count > 0))
        {
            await _client.WaitForFlushAsync(ids, waitingInterval, timeout, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    ulong CalculateGuaranteeTimestamp(
        string collectionName, ConsistencyLevel consistencyLevel, ulong? userProvidedGuaranteeTimestamp)
    {
        if (userProvidedGuaranteeTimestamp is not null && consistencyLevel != ConsistencyLevel.Customized)
        {
            throw new ArgumentException(
                $"A guarantee timestamp can only be specified with consistency level {ConsistencyLevel.Customized}");
        }

        ulong guaranteeTimestamp = consistencyLevel switch
        {
            ConsistencyLevel.Strong => 0,

            ConsistencyLevel.Session
                => _client.CollectionLastMutationTimestamps.TryGetValue(collectionName, out ulong lastMutationTimestamp)
                    ? lastMutationTimestamp
                    : 1,

            // TODO: This follows pymilvus, but confirm.
            // TODO: The Java SDK subtracts a graceful period from the current timestamp instead.
            ConsistencyLevel.BoundedStaleness => 2,

            ConsistencyLevel.Eventually => 1,

            ConsistencyLevel.Customized => userProvidedGuaranteeTimestamp
                ?? throw new ArgumentException(
                    $"A guarantee timestamp is required with consistency level {ConsistencyLevel.Customized}"),

            _ => throw new ArgumentOutOfRangeException(
                nameof(consistencyLevel), consistencyLevel, "Invalid consistency level: " + consistencyLevel)
        };

        return guaranteeTimestamp;
    }
}
