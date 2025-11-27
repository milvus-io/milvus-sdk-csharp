using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Google.Protobuf.Collections;
using KeyValuePair = Milvus.Client.Grpc.KeyValuePair;

namespace Milvus.Client;

public partial class MilvusCollection
{
    private const ulong GuaranteeTimestampStrong = 0;
    private const ulong GuaranteeTimestampEventually = 1;
    private const ulong GuaranteeTimestampBounded = 2;

    /// <summary>
    /// Inserts rows of data into a collection.
    /// </summary>
    /// <param name="data">The field data to insert; each field contains a list of row values.</param>
    /// <param name="partitionName">An optional name of a partition to insert into.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<MutationResult> InsertAsync(
        IReadOnlyList<FieldData> data,
        string? partitionName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(data);

        InsertRequest request = new() { CollectionName = Name };

        if (partitionName is not null)
        {
            request.PartitionName = partitionName;
        }

        PopulateData(data, request.FieldsData);

        request.NumRows = (uint)data[0].RowCount;

        Grpc.MutationResult response =
            await _client.InvokeAsync(_client.GrpcClient.InsertAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        _client.CollectionLastMutationTimestamps[Name] = response.Timestamp;

        return new MutationResult(response);
    }

    /// <summary>
    /// Upserts rows of data into a collection.
    /// </summary>
    /// <param name="data">The field data to upsert; each field contains a list of row values.</param>
    /// <param name="partitionName">An optional name of a partition to upsert into.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<MutationResult> UpsertAsync(
        IReadOnlyList<FieldData> data,
        string? partitionName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(data);

        UpsertRequest request = new() { CollectionName = Name };

        if (partitionName is not null)
        {
            request.PartitionName = partitionName;
        }

        PopulateData(data, request.FieldsData);

        request.NumRows = (uint)data[0].RowCount;

        Grpc.MutationResult response =
            await _client.InvokeAsync(_client.GrpcClient.UpsertAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        _client.CollectionLastMutationTimestamps[Name] = response.Timestamp;

        return new MutationResult(response);
    }

    private static void PopulateData(IReadOnlyList<FieldData> fieldsData, RepeatedField<Grpc.FieldData> grpcFieldsData)
    {
        Dictionary<string, object?>?[]? dynamicFieldsData = null;

        long count = fieldsData[0].RowCount;
        foreach (FieldData field in fieldsData)
        {
            if (field.RowCount != count)
            {
                throw new MilvusException("All fields must have the same number of rows.");
            }

            if (field.IsDynamic)
            {
                dynamicFieldsData ??= new Dictionary<string, object?>[count];

                for (int rowNum = 0; rowNum < count; rowNum++)
                {
                    Dictionary<string, object?> rowDynamicData =
                        dynamicFieldsData[rowNum] ?? (dynamicFieldsData[rowNum] = new Dictionary<string, object?>());

                    rowDynamicData[field.FieldName!] = field.GetValueAsObject(rowNum);
                }
            }
            else
            {
                grpcFieldsData.Add(field.ToGrpcFieldData());
            }
        }

        if (dynamicFieldsData is not null)
        {
            string[] encodedJsonStrings = new string[count];
            for (int rowNum = 0; rowNum < count; rowNum++)
            {
                encodedJsonStrings[rowNum] = JsonSerializer.Serialize(dynamicFieldsData[rowNum]);
            }

            FieldData metaFieldData = new FieldData<string>(encodedJsonStrings, MilvusDataType.Json, isDynamic: true);
            grpcFieldsData.Add(metaFieldData.ToGrpcFieldData());
        }
    }

    /// <summary>
    /// Deletes rows from a collection by given expression.
    /// </summary>
    /// <param name="expression">A boolean expression determining which rows are to be deleted.</param>
    /// <param name="partitionName">An optional name of a partition from which rows are to be deleted.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>A <see cref="MutationResult" /> containing information about the drop operation.</returns>
    public async Task<MutationResult> DeleteAsync(
        string expression,
        string? partitionName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(expression);

        DeleteRequest request = new DeleteRequest
        {
            CollectionName = Name,
            Expr = expression,
            PartitionName = !string.IsNullOrEmpty(partitionName) ? partitionName : string.Empty
        };

        Grpc.MutationResult response =
            await _client.InvokeAsync(_client.GrpcClient.DeleteAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        _client.CollectionLastMutationTimestamps[Name] = response.Timestamp;

        return new MutationResult(response);
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
    public async Task<SearchResults> SearchAsync<T>(
        string vectorFieldName,
        IReadOnlyList<ReadOnlyMemory<T>> vectors,
        SimilarityMetricType metricType,
        int limit,
        SearchParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(vectorFieldName);
        Verify.NotNull(vectors);

        Grpc.PlaceholderValue placeholderValue = new() { Tag = Constants.VectorTag };

        switch (vectors)
        {
            case IReadOnlyList<ReadOnlyMemory<float>> floatVectors:
                PopulateFloatVectorData(floatVectors, placeholderValue);
                break;
            case IReadOnlyList<ReadOnlyMemory<byte>> binaryVectors:
                PopulateBinaryVectorData(binaryVectors, placeholderValue);
                break;
#if NET8_0_OR_GREATER
            case IReadOnlyList<ReadOnlyMemory<Half>> float16Vectors:
                PopulateFloat16VectorData(float16Vectors, placeholderValue);
                break;
#endif
            default:
                throw new ArgumentException("Only vectors of float, byte, or Half are supported", nameof(vectors));
        }

        return await SearchInternalAsync(vectorFieldName, placeholderValue, metricType, limit, parameters, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Perform a sparse vector similarity search. Available since Milvus v2.4.
    /// </summary>
    /// <param name="vectorFieldName">The name of the sparse vector field to search in.</param>
    /// <param name="vectors">The set of sparse vectors to send as input for the similarity search.</param>
    /// <param name="metricType">
    /// Method used to measure the distance between vectors during search. Must correspond to the metric type specified
    /// when building the index. For sparse vectors, typically <see cref="SimilarityMetricType.Ip" /> is used.
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
    public async Task<SearchResults> SearchAsync<T>(
        string vectorFieldName,
        IReadOnlyList<MilvusSparseVector<T>> vectors,
        SimilarityMetricType metricType,
        int limit,
        SearchParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(vectorFieldName);
        Verify.NotNull(vectors);

        Grpc.PlaceholderValue placeholderValue = new()
        {
            Tag = Constants.VectorTag,
            Type = Grpc.PlaceholderType.SparseFloatVector
        };

        foreach (MilvusSparseVector<T> sparseVector in vectors)
        {
            placeholderValue.Values.Add(ByteString.CopyFrom(sparseVector.ToBytes()));
        }

        return await SearchInternalAsync(vectorFieldName, placeholderValue, metricType, limit, parameters, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<SearchResults> SearchInternalAsync(
        string vectorFieldName,
        Grpc.PlaceholderValue placeholderValue,
        SimilarityMetricType metricType,
        int limit,
        SearchParameters? parameters,
        CancellationToken cancellationToken)
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

            if (parameters.GroupByField is not null)
            {
                request.SearchParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.GroupByField,
                    Value = parameters.GroupByField
                });
            }
        }

        // Note that we send both the consistency level and the guarantee timestamp, although the latter is derived
        // from the former and should be sufficient.
        if (parameters?.ConsistencyLevel is null)
        {
            request.UseDefaultConsistency = true;
            request.GuaranteeTimestamp = CalculateGuaranteeTimestamp(Name, ConsistencyLevel.Session, userProvidedGuaranteeTimestamp: null);
        }
        else
        {
            request.ConsistencyLevel = (Grpc.ConsistencyLevel)parameters.ConsistencyLevel.Value;
            request.GuaranteeTimestamp = CalculateGuaranteeTimestamp(
                Name, parameters.ConsistencyLevel.Value, parameters.GuaranteeTimestamp);
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
                    Value = parameters is null ? "{}" : Combine(parameters.ExtraParameters)
                }
            });

        Grpc.SearchResults response =
            await _client.InvokeAsync(_client.GrpcClient.SearchAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        List<FieldData> fieldData = ProcessReturnedFieldData(response.Results.FieldsData);

        return new SearchResults
        {
            CollectionName = response.CollectionName,
            FieldsData = fieldData,
            Ids = response.Results.Ids is null ? default : MilvusIds.FromGrpc(response.Results.Ids),
            NumQueries = response.Results.NumQueries,
            Scores = response.Results.Scores,
            Limit = response.Results.TopK,
            Limits = response.Results.Topks,
        };
    }

    private static void PopulateFloatVectorData(
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

    private static void PopulateBinaryVectorData(
        IReadOnlyList<ReadOnlyMemory<byte>> vectors, Grpc.PlaceholderValue placeholderValue)
    {
        placeholderValue.Type = Grpc.PlaceholderType.BinaryVector;

        foreach (ReadOnlyMemory<byte> milvusVector in vectors)
        {
            placeholderValue.Values.Add(ByteString.CopyFrom(milvusVector.Span));
        }
    }

#if NET8_0_OR_GREATER
    private static void PopulateFloat16VectorData(
        IReadOnlyList<ReadOnlyMemory<Half>> vectors, Grpc.PlaceholderValue placeholderValue)
    {
        placeholderValue.Type = Grpc.PlaceholderType.Float16Vector;

        foreach (ReadOnlyMemory<Half> milvusVector in vectors)
        {
            int length = milvusVector.Length * sizeof(ushort);
            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);

            for (int i = 0; i < milvusVector.Length; i++)
            {
                ushort halfBits = BitConverter.HalfToUInt16Bits(milvusVector.Span[i]);
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * sizeof(ushort)), halfBits);
            }

            placeholderValue.Values.Add(ByteString.CopyFrom(bytes.AsSpan(0, length)));
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }
#endif

    /// <summary>
    /// Flushes collection data to disk, required only in order to get up-to-date statistics.
    /// </summary>
    /// <remarks>
    /// This method will be removed in a future version.
    /// </remarks>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>A <see cref="FlushResult" /> containing information about the flush operation.</returns>
    public Task<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        => _client.FlushAsync(new[] { Name }, cancellationToken);

    /// <summary>
    /// Returns sealed segments information of a collection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<IReadOnlyList<PersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        CancellationToken cancellationToken = default)
    {
        GetPersistentSegmentInfoRequest request = new GetPersistentSegmentInfoRequest { CollectionName = Name };

        GetPersistentSegmentInfoResponse response = await _client.InvokeAsync(
            _client.GrpcClient.GetPersistentSegmentInfoAsync,
            request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Infos.Select(i => new PersistentSegmentInfo(
                i.CollectionID,
                i.PartitionID,
                i.SegmentID,
                i.NumRows,
                i.State))
            .ToList();
    }

    /// <summary>
    /// Retrieves rows from a collection via scalar filtering based on a boolean expression.
    /// </summary>
    /// <param name="expression">A boolean expression determining which rows are to be returned.</param>
    /// <param name="parameters">Various additional optional parameters to configure the query.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>A list of <see cref="FieldData{TData}" /> instances with the query results.</returns>
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

        PopulateQueryRequestFromParameters(request, parameters);

        QueryResults? response = await _client.InvokeAsync(
                _client.GrpcClient.QueryAsync,
                request,
                static r => r.Status,
                cancellationToken)
                .ConfigureAwait(false);

        return ProcessReturnedFieldData(response.FieldsData);
    }

    /// <summary>
    /// Retrieves rows from a collection via scalar filtering based on a boolean expression using iterator.
    /// </summary>
    /// <param name="expression">A boolean expression determining which rows are to be returned.</param>
    /// <param name="batchSize">Batch size that will be used for every iteration request. Must be between 1 and 16384.</param>
    /// <param name="parameters">Various additional optional parameters to configure the query.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>A list of <see cref="FieldData{TData}" /> instances with the query results.</returns>
    public async IAsyncEnumerable<IReadOnlyList<FieldData>> QueryWithIteratorAsync(
        string? expression = null,
        int batchSize = 1000,
        QueryParameters? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if ((parameters?.Offset ?? 0) != 0)
        {
            throw new MilvusException("Not support offset when searching iteration");
        }

        DescribeCollectionResponse? describeResponse = await _client.InvokeAsync(
                _client.GrpcClient.DescribeCollectionAsync,
                new DescribeCollectionRequest { CollectionName = Name },
                r => r.Status,
                cancellationToken)
                .ConfigureAwait(false);

        Grpc.FieldSchema? pkField = describeResponse.Schema.Fields.FirstOrDefault(x => x.IsPrimaryKey);
        if (pkField == null)
        {
            throw new MilvusException("Schema must contain pk field");
        }

        bool isUserRequestPkField = parameters?.OutputFieldsInternal?.Contains(pkField.Name) ?? false;
        string? userExpression = expression;
        int userLimit = parameters?.Limit ?? int.MaxValue;

        QueryRequest request = new()
        {
            CollectionName = Name,
            Expr = (userExpression, pkField) switch
            {
                // If user expression is not null, we should use it
                {userExpression: not null} => userExpression,
                // If user expression is null and pk field is string
                {pkField.DataType: DataType.VarChar} => $"{pkField.Name} != ''",
                // If user expression is null and pk field is int
                {pkField.DataType: DataType.Int8 or DataType.Int16 or DataType.Int32 or DataType.Int64} => $"{pkField.Name} < {long.MaxValue}",
                // If user expression is null and pk field is not string and not int
                _ => throw new MilvusException("Unsupported data type for primary key field")
            }
        };

        PopulateQueryRequestFromParameters(request, parameters);

        // Request id field in any case to proceed with an iterations
        if (!isUserRequestPkField) request.OutputFields.Add(pkField.Name);

        // Replace parameters required for iterator
        string iterationBatchSize = Math.Min(batchSize, userLimit).ToString(CultureInfo.InvariantCulture);
        ReplaceKeyValueItems(request.QueryParams,
            new Grpc.KeyValuePair {Key = Constants.Iterator, Value = "True"},
            new Grpc.KeyValuePair {Key = Constants.ReduceStopForBest, Value = "True"},
            new Grpc.KeyValuePair {Key = Constants.BatchSize, Value = iterationBatchSize},
            new Grpc.KeyValuePair {Key = Constants.Offset, Value = "0"},
            new Grpc.KeyValuePair {Key = Constants.Limit, Value = iterationBatchSize});

        int processedItemsCount = 0;
        while (true)
        {
            QueryResults? response = await _client.InvokeAsync(
                    _client.GrpcClient.QueryAsync,
                    request,
                    static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

            object? pkLastValue;
            int processedDuringIterationCount;
            Grpc.FieldData? pkFieldsData = response.FieldsData.Single(x => x.FieldId == pkField.FieldID);
            switch (pkField.DataType)
            {
                case DataType.VarChar:
                    pkLastValue = pkFieldsData.Scalars.StringData.Data.LastOrDefault();
                    processedDuringIterationCount = pkFieldsData.Scalars.StringData.Data.Count;
                    break;
                case DataType.Int8:
                case DataType.Int16:
                case DataType.Int32:
                    pkLastValue = pkFieldsData.Scalars.IntData.Data.LastOrDefault();
                    processedDuringIterationCount = pkFieldsData.Scalars.IntData.Data.Count;
                    break;
                case DataType.Int64:
                    pkLastValue = pkFieldsData.Scalars.LongData.Data.LastOrDefault();
                    processedDuringIterationCount = pkFieldsData.Scalars.LongData.Data.Count;
                    break;
                default:
                    throw new MilvusException("Unsupported data type for primary key field");
            }

            // If there are no more items to process, we should break the loop
            if(processedDuringIterationCount == 0) yield break;

            // Respond with processed data
            if (!isUserRequestPkField)
            {
                // Filter out extra field if user didn't request it
                response.FieldsData.Remove(pkFieldsData);
            }
            yield return ProcessReturnedFieldData(response.FieldsData);

            processedItemsCount += processedDuringIterationCount;
            int leftItemsCount = userLimit - processedItemsCount;

            // If user limit is reached, we should break the loop
            if(leftItemsCount <= 0) yield break;

            // Setup next iteration limit and expression
            ReplaceKeyValueItems(
                request.QueryParams,
                new Grpc.KeyValuePair
                {
                    Key = Constants.Limit,
                    Value = Math.Min(batchSize, leftItemsCount).ToString(CultureInfo.InvariantCulture)
                });

            string nextExpression = pkField.DataType switch
            {
                DataType.VarChar => $"{pkField.Name} > '{pkLastValue}'",
                DataType.Int8 or DataType.Int16 or DataType.Int32 or DataType.Int64 => $"{pkField.Name} > {pkLastValue}",
                _ => throw new MilvusException("Unsupported data type for primary key field")
            };

            if (!string.IsNullOrWhiteSpace(userExpression))
            {
                nextExpression += $" and ({userExpression})";
            }

            request.Expr = nextExpression;
        }
    }

    /// <summary>
    /// Get query segment information.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns><see cref="QuerySegmentInfoResult"/></returns>
    public async Task<IReadOnlyList<QuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        CancellationToken cancellationToken = default)
    {
        GetQuerySegmentInfoRequest request = new GetQuerySegmentInfoRequest { CollectionName = Name };

        GetQuerySegmentInfoResponse response =
            await _client.InvokeAsync(_client.GrpcClient.GetQuerySegmentInfoAsync, request, static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        return response.Infos.Select(i => new QuerySegmentInfoResult(
                i.CollectionID, i.IndexName, i.IndexID, i.MemSize, i.NodeIds, i.NumRows, i.PartitionID, i.SegmentID,
                (SegmentState)i.State))
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
        FlushResult response = await FlushAsync(cancellationToken).ConfigureAwait(false);

        foreach (IReadOnlyList<long> ids in response.CollSegIDs.Values.Where(p => p.Count > 0))
        {
            await _client.WaitForFlushAsync(ids, waitingInterval, timeout, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static List<FieldData> ProcessReturnedFieldData(RepeatedField<Grpc.FieldData> grpcFields)
    {
        int fieldCount = grpcFields.Count;
        List<FieldData> results = new List<FieldData>(fieldCount);

        Grpc.FieldData? metaFieldData = null;

        foreach (Grpc.FieldData grpcField in grpcFields)
        {
            if (grpcField.IsDynamic)
            {
                if (metaFieldData is not null)
                {
                    throw new NotSupportedException("Multiple dynamic fields in results");
                }

                metaFieldData = grpcField;
            }
            else
            {
                results.Add(FieldData.FromGrpcFieldData(grpcField));
            }
        }

        if (metaFieldData is null)
        {
            return results;
        }

        // Dynamic data is present, parse out the fields from the JSON

        if (metaFieldData.FieldCase != Grpc.FieldData.FieldOneofCase.Scalars
            || metaFieldData.Scalars.DataCase != ScalarField.DataOneofCase.JsonData)
        {
            throw new NotSupportedException("Non-JSON dynamic field in results");
        }

        RepeatedField<ByteString> rawJsonValues = metaFieldData.Scalars.JsonData.Data;

        Dictionary<string, Array> dynamicFields = new();

        int rowCount = rawJsonValues.Count;
        for (int rowNum = 0; rowNum < rowCount; rowNum++)
        {
            Utf8JsonReader jsonReader = new(metaFieldData.Scalars.JsonData.Data[rowNum].Span);

            if (!jsonReader.Read() || jsonReader.TokenType != JsonTokenType.StartObject)
            {
                throw ParseError();
            }

            while (true)
            {
                if (!jsonReader.Read())
                {
                    throw ParseError();
                }

                if (jsonReader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (jsonReader.TokenType != JsonTokenType.PropertyName
                    || jsonReader.GetString() is not string fieldName
                    || !jsonReader.Read())

                {
                    throw ParseError();
                }

                switch (jsonReader.TokenType)
                {
                    case JsonTokenType.String:
                        {
                            if (!dynamicFields.TryGetValue(fieldName, out Array? array))
                            {
                                array = dynamicFields[fieldName] = new string[rowCount];
                            }

                            ((string[])array)[rowNum] = jsonReader.GetString()!;
                            break;
                        }

                    case JsonTokenType.Number:
                        {
                            if (!dynamicFields.TryGetValue(fieldName, out Array? array))
                            {
                                array = dynamicFields[fieldName] = new long[rowCount];
                            }

                            ((long[])array)[rowNum] = jsonReader.GetInt64();
                            break;
                        }

                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        {
                            if (!dynamicFields.TryGetValue(fieldName, out Array? array))
                            {
                                array = dynamicFields[fieldName] = new bool[rowCount];
                            }

                            ((bool[])array)[rowNum] = jsonReader.TokenType == JsonTokenType.True;
                            break;
                        }

                    default:
                        throw ParseError();
                }
            }

            MilvusException ParseError()
                => new MilvusException("Couldn't parse dynamic JSON data: " +
                                       metaFieldData.Scalars.JsonData.Data[rowNum].ToStringUtf8());
        }

        foreach (KeyValuePair<string, Array> kvp in dynamicFields)
        {
            results.Add(kvp.Value switch
            {
                string[] stringArray => FieldData.CreateVarChar(kvp.Key, stringArray, isDynamic: true),
                long[] longArray => FieldData.Create(kvp.Key, longArray, isDynamic: true),
                bool[] boolArray => FieldData.Create(kvp.Key, boolArray, isDynamic: true),
                _ => throw new MilvusException("Unexpected array type when deserializing dynamic data: " +
                                               kvp.Value.GetType())
            });
        }

        return results;
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
            ConsistencyLevel.Strong => GuaranteeTimestampStrong,

            ConsistencyLevel.Session
                => _client.CollectionLastMutationTimestamps.TryGetValue(collectionName, out ulong lastMutationTimestamp)
                    ? lastMutationTimestamp
                    : GuaranteeTimestampEventually,

            ConsistencyLevel.BoundedStaleness => GuaranteeTimestampBounded,

            ConsistencyLevel.Eventually => GuaranteeTimestampEventually,

            ConsistencyLevel.Customized => userProvidedGuaranteeTimestamp
                ?? throw new ArgumentException(
                    $"A guarantee timestamp is required with consistency level {ConsistencyLevel.Customized}"),

            _ => throw new ArgumentOutOfRangeException(
                nameof(consistencyLevel), consistencyLevel, "Invalid consistency level: " + consistencyLevel)
        };

        return guaranteeTimestamp;
    }

    private static void ReplaceKeyValueItems(RepeatedField<Grpc.KeyValuePair> collection, params Grpc.KeyValuePair[] pairs)
    {
        string[] obsoleteParameterKeys = pairs.Select(x => x.Key).Distinct().ToArray();
        KeyValuePair[] obsoleteParameters = collection.Where(x => obsoleteParameterKeys.Contains(x.Key)).ToArray();
        foreach (KeyValuePair field in obsoleteParameters)
        {
            collection.Remove(field);
        }

        foreach (KeyValuePair pair in pairs)
        {
            collection.Add(pair);
        }
    }

    private void PopulateQueryRequestFromParameters(QueryRequest request, QueryParameters? parameters)
    {
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
                    Key = Constants.Limit, Value = parameters.Limit.Value.ToString(CultureInfo.InvariantCulture)
                });
            }
        }

        // Note that we send both the consistency level and the guarantee timestamp, although the latter is derived
        // from the former and should be sufficient.
        if (parameters?.ConsistencyLevel is null)
        {
            request.UseDefaultConsistency = true;
            request.GuaranteeTimestamp = CalculateGuaranteeTimestamp(Name, ConsistencyLevel.Session, userProvidedGuaranteeTimestamp: null);
        }
        else
        {
            request.ConsistencyLevel = (Grpc.ConsistencyLevel)parameters.ConsistencyLevel.Value;
            request.GuaranteeTimestamp =
                CalculateGuaranteeTimestamp(Name, parameters.ConsistencyLevel.Value,
                    parameters.GuaranteeTimestamp);
        }
    }
}
