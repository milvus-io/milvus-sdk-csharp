using System.Globalization;
using System.Runtime.CompilerServices;

using Google.Protobuf.Collections;

using Milvus.Client.V2.Responses.Collection;

using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Performs a vector similarity search.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<SearchResp> SearchAsync(
        SearchReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.SearchRequest grpcRequest = BuildSearchRequest(request);
        Grpc.SearchResults response = await InvokeAsync(
                GrpcClient.SearchAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return SearchResp.FromGrpc(response);
    }

    /// <summary>
    /// Performs a hybrid search, combining the results of multiple ANN searches with a reranker.
    /// </summary>
    /// <param name="request">The hybrid search request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<SearchResp> HybridSearchAsync(
        HybridSearchReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        request.Validate();

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var grpcRequest = new Grpc.HybridSearchRequest { CollectionName = request.CollectionName };

        foreach (SearchReq subRequest in request.SearchRequests)
        {
            grpcRequest.Requests.Add(BuildHybridSearchSubRequest(subRequest));
        }

        grpcRequest.RankParams.AddRange(ToGrpcKeyValuePairs(request.Reranker.ToRankParams()));
        grpcRequest.RankParams.Add(new Grpc.KeyValuePair
        {
            Key = "limit",
            Value = request.Limit.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        SearchParameters? parameters = request.Parameters;
        if (parameters is not null)
        {
            if (parameters.PartitionNamesInternal?.Count > 0)
            {
                grpcRequest.PartitionNames.AddRange(parameters.PartitionNamesInternal);
            }
            if (parameters.OutputFieldsInternal?.Count > 0)
            {
                grpcRequest.OutputFields.AddRange(parameters.OutputFieldsInternal);
            }
            if (parameters.TimeTravelTimestamp is not null)
            {
                grpcRequest.TravelTimestamp = parameters.TimeTravelTimestamp.Value;
            }
            if (parameters.RoundDecimal is not null)
            {
                grpcRequest.RankParams.Add(new Grpc.KeyValuePair { Key = "round_decimal", Value = parameters.RoundDecimal.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (parameters.GroupByField is not null)
            {
                grpcRequest.RankParams.Add(new Grpc.KeyValuePair { Key = "group_by_field", Value = parameters.GroupByField });
            }
            if (parameters.GroupSize is not null)
            {
                grpcRequest.RankParams.Add(new Grpc.KeyValuePair { Key = "group_size", Value = parameters.GroupSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (parameters.StrictGroupSize is not null)
            {
                grpcRequest.RankParams.Add(new Grpc.KeyValuePair { Key = "strict_group_size", Value = parameters.StrictGroupSize.Value.ToString() });
            }

            grpcRequest.ConsistencyLevel = parameters.ConsistencyLevel is { } cl
                ? (Grpc.ConsistencyLevel)(int)cl
                : Grpc.ConsistencyLevel.Session;
            grpcRequest.GuaranteeTimestamp = CalculateGuaranteeTimestamp(
                _endpoint, _database, request.CollectionName,
                parameters.ConsistencyLevel ?? ConsistencyLevel.Session, parameters.GuaranteeTimestamp);
        }
        else
        {
            grpcRequest.UseDefaultConsistency = true;
            grpcRequest.GuaranteeTimestamp = CalculateGuaranteeTimestamp(_endpoint, _database, request.CollectionName, ConsistencyLevel.Session, null);
        }

        Grpc.SearchResults response = await InvokeAsync(
                GrpcClient.HybridSearchAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return SearchResp.FromGrpc(response);
    }

    /// <summary>
    /// Queries rows from a collection by expression.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<QueryResp> QueryAsync(
        QueryReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.QueryRequest grpcRequest = request.ToGrpcQueryRequest();
        grpcRequest.GuaranteeTimestamp = CalculateGuaranteeTimestamp(
            _endpoint, _database, request.CollectionName,
            request.Parameters?.ConsistencyLevel ?? ConsistencyLevel.Session,
            request.Parameters?.GuaranteeTimestamp);
        Grpc.QueryResults response = await InvokeAsync(
                GrpcClient.QueryAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return QueryResp.FromGrpc(response);
    }

    /// <summary>
    /// Fetches rows by primary key.
    /// </summary>
    /// <param name="request">The get request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<GetResp> GetAsync(
        GetReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        DescribeCollectionResp description = await DescribeCollectionAsync(
            new Requests.Collection.DescribeCollectionReq { CollectionName = request.CollectionName },
            cancellationToken).ConfigureAwait(false);

        FieldSchema? primaryKey = description.Schema.Fields.SingleOrDefault(f => f.IsPrimaryKey)
            ?? throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"Collection '{request.CollectionName}' has no primary key field.");

        Grpc.QueryRequest grpcRequest = request.ToGrpcQueryRequest(primaryKey.Name);
        Grpc.QueryResults response = await InvokeAsync(
                GrpcClient.QueryAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return GetResp.FromGrpc(response);
    }

    /// <summary>
    /// Creates a server-side iterator that pages over query results in batches.
    /// </summary>
    /// <remarks>
    /// The iterator lazily pages over the server in <see cref="QueryIteratorReq.BatchSize" />-sized batches
    /// (default 1000, range 1–16384). <see cref="QueryParameters.Offset" /> is not supported and throws.
    /// Consume with <c>await foreach</c>, optionally with <c>.WithCancellation(token)</c>.
    /// </remarks>
    /// <param name="request">The query iterator request.</param>
    public QueryIterator QueryIteratorAsync(QueryIteratorReq request)
    {
        Verify.NotNull(request);
        request.Validate();
        return new QueryIterator(this, request);
    }

    /// <summary>
    /// Creates a server-side iterator that pages over search results in batches.
    /// </summary>
    /// <remarks>
    /// The iterator uses the <c>search_iter_v2</c> token protocol and requires a Milvus server of version 2.5.2
    /// or later. <see cref="SearchParameters.Offset" /> is not supported and throws. Consume with
    /// <c>await foreach</c>, optionally with <c>.WithCancellation(token)</c>.
    /// </remarks>
    /// <param name="request">The search iterator request.</param>
    public SearchIterator SearchIteratorAsync(SearchIteratorReq request)
    {
        Verify.NotNull(request);
        request.Validate();
        return new SearchIterator(this, request);
    }

    internal async IAsyncEnumerable<IReadOnlyList<FieldData>> QueryIteratorCoreAsync(
        QueryIteratorReq request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        DescribeCollectionResp description = await DescribeCollectionAsync(
                new Requests.Collection.DescribeCollectionReq { CollectionName = request.CollectionName },
                cancellationToken)
            .ConfigureAwait(false);

        FieldSchema? pkField = description.Schema.Fields.FirstOrDefault(f => f.IsPrimaryKey);
        if (pkField is null)
        {
            throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"Collection '{request.CollectionName}' has no primary key field.");
        }

        bool isUserRequestPkField = request.Parameters?.OutputFieldsInternal?.Contains(pkField.Name) ?? false;
        string? userExpression = request.Expression;
        int userLimit = request.Parameters?.Limit ?? int.MaxValue;

        string expr = userExpression ?? pkField.DataType switch
        {
            DataType.VarChar => $"{pkField.Name} != ''",
            DataType.Int8 or DataType.Int16 or DataType.Int32 or DataType.Int64 => $"{pkField.Name} < {long.MaxValue}",
            _ => throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"Unsupported data type '{pkField.DataType}' for primary key field '{pkField.Name}'.")
        };

        var grpcRequest = new Grpc.QueryRequest
        {
            CollectionName = request.CollectionName,
            Expr = expr
        };

        QueryParameters? parameters = request.Parameters;
        if (parameters is not null)
        {
            if (parameters.PartitionNamesInternal?.Count > 0)
            {
                grpcRequest.PartitionNames.AddRange(parameters.PartitionNamesInternal);
            }
            if (parameters.OutputFieldsInternal?.Count > 0)
            {
                grpcRequest.OutputFields.AddRange(parameters.OutputFieldsInternal);
            }
            if (parameters.TimeTravelTimestamp is not null)
            {
                grpcRequest.TravelTimestamp = parameters.TimeTravelTimestamp.Value;
            }

            grpcRequest.ConsistencyLevel = parameters.ConsistencyLevel is { } cl
                ? (Grpc.ConsistencyLevel)(int)cl
                : Grpc.ConsistencyLevel.Session;
            grpcRequest.GuaranteeTimestamp = CalculateGuaranteeTimestamp(
                _endpoint, _database, request.CollectionName,
                parameters.ConsistencyLevel ?? ConsistencyLevel.Session, parameters.GuaranteeTimestamp);
        }
        else
        {
            grpcRequest.UseDefaultConsistency = true;
            grpcRequest.GuaranteeTimestamp = CalculateGuaranteeTimestamp(
                _endpoint, _database, request.CollectionName, ConsistencyLevel.Session, null);
        }

        // Request the primary key field in any case to drive the iteration.
        if (!isUserRequestPkField)
        {
            grpcRequest.OutputFields.Add(pkField.Name);
        }

        // Replace parameters required for the iterator.
        string iterationBatchSize = Math.Min(request.BatchSize, userLimit).ToString(CultureInfo.InvariantCulture);
        ReplaceKeyValueItems(grpcRequest.QueryParams,
            new Grpc.KeyValuePair { Key = "iterator", Value = "True" },
            new Grpc.KeyValuePair { Key = "reduce_stop_for_best", Value = "True" },
            new Grpc.KeyValuePair { Key = "batch_size", Value = iterationBatchSize },
            new Grpc.KeyValuePair { Key = "offset", Value = "0" },
            new Grpc.KeyValuePair { Key = "limit", Value = iterationBatchSize });

        int processedItemsCount = 0;
        while (true)
        {
            Grpc.QueryResults response = await InvokeAsync(
                    GrpcClient.QueryAsync, grpcRequest, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

            Grpc.FieldData? pkFieldData = response.FieldsData.FirstOrDefault(f => f.FieldName == pkField.Name);
            if (pkFieldData is null)
            {
                throw new MilvusException(MilvusErrorCode.UnexpectedError,
                    $"Query iterator response did not contain primary key field '{pkField.Name}'.");
            }

            object? pkLastValue;
            int processedDuringIterationCount;
            switch (pkField.DataType)
            {
                case DataType.VarChar:
                    pkLastValue = pkFieldData.Scalars.StringData.Data.LastOrDefault();
                    processedDuringIterationCount = pkFieldData.Scalars.StringData.Data.Count;
                    break;
                case DataType.Int8:
                case DataType.Int16:
                case DataType.Int32:
                    pkLastValue = pkFieldData.Scalars.IntData.Data.LastOrDefault();
                    processedDuringIterationCount = pkFieldData.Scalars.IntData.Data.Count;
                    break;
                case DataType.Int64:
                    pkLastValue = pkFieldData.Scalars.LongData.Data.LastOrDefault();
                    processedDuringIterationCount = pkFieldData.Scalars.LongData.Data.Count;
                    break;
                default:
                    throw new MilvusException(MilvusErrorCode.UnexpectedError,
                        $"Unsupported data type '{pkField.DataType}' for primary key field '{pkField.Name}'.");
            }

            if (processedDuringIterationCount == 0)
            {
                yield break;
            }

            // Remove the extra primary key field if the user did not request it.
            if (!isUserRequestPkField)
            {
                response.FieldsData.Remove(pkFieldData);
            }

            yield return DqlConversions.ProcessReturnedFieldData(response.FieldsData);

            processedItemsCount += processedDuringIterationCount;
            int leftItemsCount = userLimit - processedItemsCount;
            if (leftItemsCount <= 0)
            {
                yield break;
            }

            ReplaceKeyValueItems(grpcRequest.QueryParams,
                new Grpc.KeyValuePair
                {
                    Key = "limit",
                    Value = Math.Min(request.BatchSize, leftItemsCount).ToString(CultureInfo.InvariantCulture)
                });

            string nextExpression = pkField.DataType switch
            {
                DataType.VarChar => $"{pkField.Name} > '{EscapeStringLiteral(pkLastValue as string)}'",
                DataType.Int8 or DataType.Int16 or DataType.Int32 or DataType.Int64 => $"{pkField.Name} > {pkLastValue}",
                _ => throw new MilvusException(MilvusErrorCode.UnexpectedError,
                    $"Unsupported data type '{pkField.DataType}' for primary key field '{pkField.Name}'.")
            };

            if (!string.IsNullOrWhiteSpace(userExpression))
            {
                nextExpression += $" and ({userExpression})";
            }

            grpcRequest.Expr = nextExpression;
        }
    }

    internal async IAsyncEnumerable<IReadOnlyList<FieldData>> SearchIteratorCoreAsync(
        SearchIteratorReq request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        DescribeCollectionResp description = await DescribeCollectionAsync(
                new Requests.Collection.DescribeCollectionReq { CollectionName = request.CollectionName },
                cancellationToken)
            .ConfigureAwait(false);

        long collectionId = description.CollectionId;

        int remaining = request.Limit is > 0 and not int.MaxValue ? request.Limit : int.MaxValue;

        var subRequest = new SearchReq
        {
            CollectionName = request.CollectionName,
            VectorFieldName = request.VectorFieldName,
            Vectors = request.Vectors,
            SparseVectors = request.SparseVectors,
            HalfVectors = request.HalfVectors,
            MetricType = request.MetricType,
            Limit = request.BatchSize,
            Parameters = request.Parameters
        };

        Grpc.SearchRequest grpcRequest = BuildSearchRequest(subRequest);
        grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = "collection_id", Value = collectionId.ToString(CultureInfo.InvariantCulture) });
        grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = "iterator", Value = "True" });
        grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = "search_iter_v2", Value = "True" });
        grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = "guarantee_timestamp", Value = "0" });

        string? token = null;
        float? lastBound = null;

        while (remaining > 0)
        {
            int batchSize = Math.Min(request.BatchSize, remaining);
            ReplaceKeyValueItems(grpcRequest.SearchParams,
                new Grpc.KeyValuePair { Key = "topk", Value = batchSize.ToString(CultureInfo.InvariantCulture) },
                new Grpc.KeyValuePair { Key = "search_iter_batch_size", Value = batchSize.ToString(CultureInfo.InvariantCulture) });

            if (token is not null)
            {
                ReplaceKeyValueItems(grpcRequest.SearchParams,
                    new Grpc.KeyValuePair { Key = "search_iter_id", Value = token });
            }

            if (lastBound is not null)
            {
                ReplaceKeyValueItems(grpcRequest.SearchParams,
                    new Grpc.KeyValuePair { Key = "search_iter_last_bound", Value = FormatIteratorBound(lastBound.Value) });
            }

            Grpc.SearchResults response = await InvokeAsync(
                    GrpcClient.SearchAsync, grpcRequest, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

            if (response.SessionTs > 0)
            {
                ReplaceKeyValueItems(grpcRequest.SearchParams,
                    new Grpc.KeyValuePair { Key = "guarantee_timestamp", Value = response.SessionTs.ToString(CultureInfo.InvariantCulture) });
            }

            Grpc.SearchIteratorV2Results? iteratorInfo = response.Results?.SearchIteratorV2Results;
            if (iteratorInfo is null || string.IsNullOrEmpty(iteratorInfo.Token))
            {
                throw new MilvusException(MilvusErrorCode.UnexpectedError,
                    "The server does not support the Search Iterator V2 protocol; a Milvus server of version 2.5.2 or later is required.");
            }

            token ??= iteratorInfo.Token;
            lastBound = iteratorInfo.LastBound;

            int count = response.Results?.Topks.Count > 0 ? (int)response.Results.Topks[0] : 0;
            if (count == 0)
            {
                yield break;
            }

            yield return DqlConversions.ProcessReturnedFieldData(response.Results!.FieldsData);

            remaining -= count;
        }
    }

    private static string FormatIteratorBound(float bound)
        => ((double)bound).ToString("0.000000000000000", CultureInfo.InvariantCulture);

    private static void ReplaceKeyValueItems(
        RepeatedField<Grpc.KeyValuePair> collection, params Grpc.KeyValuePair[] pairs)
    {
        string[] obsoleteParameterKeys = pairs.Select(x => x.Key).Distinct().ToArray();
        Grpc.KeyValuePair[] obsoleteParameters = collection.Where(x => obsoleteParameterKeys.Contains(x.Key)).ToArray();
        foreach (Grpc.KeyValuePair field in obsoleteParameters)
        {
            collection.Remove(field);
        }

        foreach (Grpc.KeyValuePair pair in pairs)
        {
            collection.Add(pair);
        }
    }

    private Grpc.SearchRequest BuildSearchRequest(SearchReq request)
    {
        Verify.NotNullOrWhiteSpace(request.VectorFieldName);
        Verify.NotNullOrWhiteSpace(request.VectorFieldName);

        // Exactly one of dense / sparse / half vectors must be provided.
        int vectorInputs = (request.Vectors.Count > 0 ? 1 : 0)
                           + (request.SparseVectors is { Count: > 0 } ? 1 : 0)
                           + (request.HalfVectors is { Count: > 0 } ? 1 : 0);
        if (vectorInputs != 1)
        {
            throw new ArgumentException(
                "Exactly one of Vectors, SparseVectors or HalfVectors must be provided.");
        }

        Verify.GreaterThan(request.Limit, 0);

        var grpcRequest = new Grpc.SearchRequest
        {
            CollectionName = request.CollectionName,
            DslType = Grpc.DslType.BoolExprV1
        };

        SearchParameters? parameters = request.Parameters;
        if (parameters is not null)
        {
            if (parameters.PartitionNamesInternal?.Count > 0)
            {
                grpcRequest.PartitionNames.AddRange(parameters.PartitionNamesInternal);
            }
            if (parameters.OutputFieldsInternal?.Count > 0)
            {
                grpcRequest.OutputFields.AddRange(parameters.OutputFieldsInternal);
            }
            if (parameters.Expression is not null)
            {
                grpcRequest.Dsl = parameters.Expression;
            }
            if (parameters.TimeTravelTimestamp is not null)
            {
                grpcRequest.TravelTimestamp = parameters.TimeTravelTimestamp.Value;
            }
            if (parameters.Offset is not null)
            {
                grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = "offset", Value = parameters.Offset.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (parameters.RoundDecimal is not null)
            {
                grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = "round_decimal", Value = parameters.RoundDecimal.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (parameters.GroupByField is not null)
            {
                grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = Constants.GroupByField, Value = parameters.GroupByField });
            }
            if (parameters.GroupSize is not null)
            {
                grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = Constants.GroupSize, Value = parameters.GroupSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (parameters.StrictGroupSize is not null)
            {
                grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = Constants.StrictGroupSize, Value = parameters.StrictGroupSize.Value.ToString() });
            }
            if (parameters.IgnoreGrowing is not null)
            {
                grpcRequest.SearchParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.IgnoreGrowing,
                    Value = parameters.IgnoreGrowing.Value ? "true" : "false"
                });
            }
            if (parameters.GracefulTime is not null)
            {
                grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = Constants.GracefulTime, Value = parameters.GracefulTime.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            foreach (KeyValuePair<string, string> parameter in parameters.ExtraParameters)
            {
                grpcRequest.SearchParams.Add(new Grpc.KeyValuePair { Key = parameter.Key, Value = parameter.Value });
            }

            grpcRequest.ConsistencyLevel = parameters.ConsistencyLevel is { } cl
                ? (Grpc.ConsistencyLevel)(int)cl
                : Grpc.ConsistencyLevel.Session;
            grpcRequest.GuaranteeTimestamp = CalculateGuaranteeTimestamp(
                _endpoint, _database, request.CollectionName, parameters.ConsistencyLevel ?? ConsistencyLevel.Session, parameters.GuaranteeTimestamp);
        }
        else
        {
            grpcRequest.UseDefaultConsistency = true;
            grpcRequest.GuaranteeTimestamp = CalculateGuaranteeTimestamp(_endpoint, _database, request.CollectionName, ConsistencyLevel.Session, null);
        }

        grpcRequest.PlaceholderGroup = new Grpc.PlaceholderGroup { Placeholders = { request.ToPlaceholderValue() } }.ToByteString();
        grpcRequest.SearchParams.AddRange(
            new[]
            {
                new Grpc.KeyValuePair { Key = "anns_field", Value = request.VectorFieldName },
                new Grpc.KeyValuePair { Key = "topk", Value = request.Limit.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                new Grpc.KeyValuePair { Key = "metric_type", Value = request.MetricType.ToWireString() },
                new Grpc.KeyValuePair
                {
                    Key = "params",
                    Value = Combine(parameters?.ExtraParameters)
                }
            });

        return grpcRequest;
    }

    private static string Combine(IDictionary<string, string>? parameters)
    {
        if (parameters is null)
        {
            return "{}";
        }

        // Serialize the extra parameters as a proper JSON object so string values are quoted and
        // special characters are escaped, matching how the Java SDK sends the same bag.
        return System.Text.Json.JsonSerializer.Serialize(parameters);
    }

    // Escapes backslashes and single quotes inside a string literal embedded in a boolean expression, so
    // VarChar primary-key values used in the iterator cursor (e.g. {pk} > 'value') cannot break or inject
    // into the expression.
    private static string EscapeStringLiteral(string? value)
        => (value ?? "").Replace("\\", "\\\\").Replace("'", "\\'");

    private static Grpc.SearchRequest BuildHybridSearchSubRequest(SearchReq subRequest)
    {
        Verify.NotNullOrWhiteSpace(subRequest.VectorFieldName);

        int vectorInputs = (subRequest.Vectors.Count > 0 ? 1 : 0)
                           + (subRequest.SparseVectors is { Count: > 0 } ? 1 : 0)
                           + (subRequest.HalfVectors is { Count: > 0 } ? 1 : 0);
        if (vectorInputs != 1)
        {
            throw new ArgumentException(
                "Exactly one of Vectors, SparseVectors or HalfVectors must be provided for each sub-request.");
        }

        Verify.GreaterThan(subRequest.Limit, 0);

        var request = new Grpc.SearchRequest
        {
            CollectionName = subRequest.CollectionName,
            DslType = Grpc.DslType.BoolExprV1
        };

        SearchParameters? parameters = subRequest.Parameters;
        if (parameters is not null)
        {
            if (parameters.Expression is not null)
            {
                request.Dsl = parameters.Expression;
            }
            if (parameters.Offset is not null)
            {
                request.SearchParams.Add(new Grpc.KeyValuePair { Key = "offset", Value = parameters.Offset.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (parameters.RoundDecimal is not null)
            {
                request.SearchParams.Add(new Grpc.KeyValuePair { Key = "round_decimal", Value = parameters.RoundDecimal.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            foreach (KeyValuePair<string, string> parameter in parameters.ExtraParameters)
            {
                request.SearchParams.Add(new Grpc.KeyValuePair { Key = parameter.Key, Value = parameter.Value });
            }
        }

        request.PlaceholderGroup = new Grpc.PlaceholderGroup { Placeholders = { subRequest.ToPlaceholderValue() } }.ToByteString();
        request.SearchParams.AddRange(
            new[]
            {
                new Grpc.KeyValuePair { Key = "anns_field", Value = subRequest.VectorFieldName },
                new Grpc.KeyValuePair { Key = "topk", Value = subRequest.Limit.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                new Grpc.KeyValuePair { Key = "metric_type", Value = subRequest.MetricType.ToWireString() },
                new Grpc.KeyValuePair { Key = "params", Value = Combine(parameters?.ExtraParameters) }
            });

        return request;
    }

    private static IEnumerable<Grpc.KeyValuePair> ToGrpcKeyValuePairs(
        IReadOnlyList<KeyValuePair<string, string>> pairs)
    {
        foreach (KeyValuePair<string, string> pair in pairs)
        {
            yield return new Grpc.KeyValuePair { Key = pair.Key, Value = pair.Value };
        }
    }

    internal static ulong CalculateGuaranteeTimestamp(
        string endpoint, string database, string collectionName, ConsistencyLevel consistencyLevel, ulong? userProvidedGuaranteeTimestamp)
    {
        if (userProvidedGuaranteeTimestamp is not null && consistencyLevel != ConsistencyLevel.Customized)
        {
            throw new ArgumentException(
                $"A guarantee timestamp can only be specified with consistency level {ConsistencyLevel.Customized}");
        }

        return consistencyLevel switch
        {
            ConsistencyLevel.Strong => (ulong)Constants.GuaranteeStrongTs,
            ConsistencyLevel.Session
                => (ulong)CollectionTsCache.Instance.Get(endpoint, database, collectionName) is { } ts && ts != 0
                    ? (ulong)ts
                    : (ulong)Constants.GuaranteeEventuallyTs,
            ConsistencyLevel.BoundedStaleness => (ulong)2,
            ConsistencyLevel.Eventually => (ulong)Constants.GuaranteeEventuallyTs,
            ConsistencyLevel.Customized => userProvidedGuaranteeTimestamp
                ?? throw new ArgumentException(
                    $"A guarantee timestamp is required with consistency level {ConsistencyLevel.Customized}"),
            _ => throw new ArgumentOutOfRangeException(nameof(consistencyLevel), consistencyLevel, null)
        };
    }
}
