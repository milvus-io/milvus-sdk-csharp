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
    /// <param name="searchParameters">Various additional parameters to configure the similarity search.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusSearchResults> SearchAsync<T>(
        string vectorFieldName,
        IReadOnlyList<ReadOnlyMemory<T>> vectors,
        MilvusSimilarityMetricType metricType,
        int limit,
        SearchParameters? searchParameters = null,
        CancellationToken cancellationToken = default)
    {
        searchParameters ??= new();

        Grpc.SearchRequest request = new() { CollectionName = Name };

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
            request.TravelTimestamp = searchParameters.TravelTimestamp.Value;
        }

        // Note that we send both the consistency level and the guarantee timestamp, although the latter is derived
        // from the former and should be sufficient. TODO: Confirm this.
        if (searchParameters.ConsistencyLevel is null)
        {
            if (searchParameters.GuaranteeTimestamp is null)
            {
                request.UseDefaultConsistency = true;
            }
            else
            {
                request.ConsistencyLevel = Grpc.ConsistencyLevel.Customized;
                request.GuaranteeTimestamp = searchParameters.GuaranteeTimestamp.Value;
            }
        }
        else
        {
            request.ConsistencyLevel = (Grpc.ConsistencyLevel)searchParameters.ConsistencyLevel.Value;
            request.GuaranteeTimestamp =
                CalculateGuaranteeTimestamp(
                    Name, searchParameters.ConsistencyLevel.Value, searchParameters.GuaranteeTimestamp);
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
                new Grpc.KeyValuePair { Key = Constants.Params, Value = Combine(searchParameters.Parameters) },
            });

        if (searchParameters.Offset is not null)
        {
            request.SearchParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.Offset,
                Value = searchParameters.Offset.Value.ToString(CultureInfo.InvariantCulture)
            });
        }

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

        if (searchParameters.Expression is not null)
        {
            request.Dsl = searchParameters.Expression;
        }

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
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
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

        return MilvusPersistentSegmentInfo.From(response.Infos);
    }

    /// <summary>
    /// Retrieves rows from a collection via scalar filtering based on a boolean expression
    /// </summary>
    /// <param name="expression">A boolean expression determining which rows are to be returned.</param>
    /// <param name="outputFields">
    /// The names of fields to be returned from the search. Vector fields currently cannot be returned.
    /// </param>
    /// <param name="consistencyLevel">The consistency level to be used in the query.</param>
    /// <param name="partitionNames">An optional list of partitions names which are to be queried.</param>
    /// <param name="guaranteeTimestamp">
    /// If set, guarantee that the search operation will be performed after any updates up to the provided timestamp.
    /// If a query node isn't yet up to date for the timestamp, it waits until the missing data is received.
    /// If unset, the server executes the search immediately.
    /// </param>
    /// <param name="limit">
    /// The maximum number of records to return, also known as 'topk'. Must be between 1 and 16384.
    /// </param>
    /// <param name="offset">
    /// Number of entities to skip during the search. The sum of this parameter and <paramref name="limit" /> should be
    /// less than 16384.
    /// </param>
    /// <param name="timeTravelTimestamp">
    /// Specifies an optional travel timestamp; the search will get results based on the data at that point in time.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<MilvusQueryResult> QueryAsync(
        string expression,
        IReadOnlyList<string>? outputFields = null,
        ConsistencyLevel? consistencyLevel = null,
        IReadOnlyList<string>? partitionNames = null,
        ulong timeTravelTimestamp = 0,
        ulong? guaranteeTimestamp = null,
        long limit = 0,
        long offset = 0,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(expression);
        Verify.GreaterThanOrEqualTo(offset, 0);
        Verify.GreaterThanOrEqualTo(limit, 0);

        QueryRequest request = new()
        {
            CollectionName = Name,
            Expr = expression,
            TravelTimestamp = timeTravelTimestamp,
        };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        if (outputFields is not null)
        {
            request.OutputFields.AddRange(outputFields);
        }

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

        // Note that we send both the consistency level and the guarantee timestamp, although the latter is derived
        // from the former and should be sufficient. TODO: Confirm this.
        if (consistencyLevel is null)
        {
            if (guaranteeTimestamp is null)
            {
                request.UseDefaultConsistency = true;
            }
            else
            {
                request.ConsistencyLevel = Grpc.ConsistencyLevel.Customized;
                request.GuaranteeTimestamp = guaranteeTimestamp.Value;
            }
        }
        else
        {
            request.ConsistencyLevel = (Grpc.ConsistencyLevel)consistencyLevel.Value;
            request.GuaranteeTimestamp =
                CalculateGuaranteeTimestamp(Name, consistencyLevel.Value, guaranteeTimestamp);
        }

        QueryResults response =
            await _client.InvokeAsync(_client.GrpcClient.QueryAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return new MilvusQueryResult
        {
            CollectionName = response.CollectionName,
            FieldsData = response.FieldsData.Select(FieldData.FromGrpcFieldData).ToList()
        };
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

        return MilvusQuerySegmentInfoResult.From(response).ToList();
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
