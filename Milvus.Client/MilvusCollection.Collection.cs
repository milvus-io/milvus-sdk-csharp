using System.Globalization;
using System.Text.Json;

namespace Milvus.Client;

#pragma warning disable CA1711 // Rename type name MilvusCollection so that it does not end in 'Collection'
public partial class MilvusCollection
#pragma warning restore CA1711
{
    /// <summary>
    /// Describes a collection, returning information about its configuration and schema.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>A description of the collection.</returns>
    public async Task<MilvusCollectionDescription> DescribeAsync(CancellationToken cancellationToken = default)
    {
        var request = new DescribeCollectionRequest { CollectionName = Name };

        DescribeCollectionResponse response =
            await _client.InvokeAsync(_client.GrpcClient.DescribeCollectionAsync, request, r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        // In Milvus 2.5, describing a non-existent collection returns success (code 0) with null schema.
        if (response.Schema is null)
        {
            throw new MilvusException(MilvusErrorCode.CollectionNotFound, response.Status.Reason);
        }

        List<FieldSchema> fields = new();
        foreach (Grpc.FieldSchema grpcField in response.Schema.Fields)
        {
            FieldSchema milvusField = new(
                grpcField.FieldID,
                grpcField.Name,
                (MilvusDataType)grpcField.DataType,
                (MilvusDataType)grpcField.ElementType,
                (FieldState)grpcField.State,
                grpcField.IsPrimaryKey,
                grpcField.AutoID,
                grpcField.IsPartitionKey,
                grpcField.IsDynamic,
                grpcField.Description,
                grpcField.Nullable,
                grpcField.DefaultValue is not null ? ConvertFromValueField(grpcField.DefaultValue) : null);

            foreach (Grpc.KeyValuePair parameter in grpcField.TypeParams)
            {
                switch (parameter.Key)
                {
                    case Constants.VarcharMaxLength:
                        milvusField.MaxLength = int.Parse(parameter.Value, CultureInfo.InvariantCulture);
                        break;

                    case Constants.MaxCapacity:
                        milvusField.MaxCapacity = int.Parse(parameter.Value, CultureInfo.InvariantCulture);
                        break;

                    case Constants.VectorDim:
                        milvusField.Dimension = int.Parse(parameter.Value, CultureInfo.InvariantCulture);
                        break;

                    case Constants.EnableAnalyzer:
                        milvusField.EnableAnalyzer = parameter.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        break;

                    case Constants.AnalyzerParams:
                        milvusField.AnalyzerParams = JsonSerializer.Deserialize<Dictionary<string, object>>(parameter.Value);
                        break;
                }
            }

            milvusField.IsFunctionOutput = grpcField.IsFunctionOutput;

            fields.Add(milvusField);
        }

        List<FunctionSchema> functions = new();
        foreach (Grpc.FunctionSchema grpcFunction in response.Schema.Functions)
        {
            var parameters = grpcFunction.Params.Select(p => new KeyValuePair<string, string>(p.Key, p.Value));
            functions.Add(new FunctionSchema(
                grpcFunction.Id,
                grpcFunction.Name,
                (FunctionType)(int)grpcFunction.Type,
                grpcFunction.InputFieldNames,
                grpcFunction.OutputFieldNames,
                grpcFunction.Description,
                parameters));
        }

        CollectionSchema milvusCollectionSchema = new(fields, functions)
        {
            Name = response.Schema.Name,
            Description = response.Schema.Description,
            EnableDynamicFields = response.Schema.EnableDynamicField

            // Note that an AutoId previously existed at the schema level, but is not deprecated.
            // AutoId is now only defined at the field level.
        };

        Dictionary<string, IList<int>> startPositions = response.StartPositions.ToDictionary(
            kdp => kdp.Key,
            kdp => (IList<int>)kdp.Data.Select(static p => (int)p).ToList());

        return new MilvusCollectionDescription(
            response.Aliases,
            response.CollectionName,
            response.CollectionID,
            (ConsistencyLevel)response.ConsistencyLevel,
            response.CreatedUtcTimestamp,
            milvusCollectionSchema,
            response.ShardsNum,
            startPositions);
    }

    /// <summary>
    /// Adds a new field to an existing collection. Available since Milvus v2.6.
    /// </summary>
    /// <param name="field">
    /// The field to add. Must have <see cref="FieldSchema.Nullable" /> set — Milvus rejects any added
    /// field that isn't, even an empty collection with no existing rows to reconcile, and even one
    /// that also sets <see cref="FieldSchema.DefaultValue" />: a default does not substitute for
    /// nullable, the two are independent requirements. Vector fields cannot be added this way.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <exception cref="MilvusException">
    /// <paramref name="field" /> is not nullable, is a vector type, or duplicates an existing field
    /// name; or the collection does not exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Rows that existed before the field was added read back as <see langword="null" /> for it,
    /// unless <see cref="FieldSchema.DefaultValue" /> is also set, in which case they read back as
    /// that default — Milvus backfills it rather than leaving existing rows null. The same default
    /// applies to new rows that omit the field going forward, same as at collection creation.
    /// </para>
    /// <para>
    /// The collection does not need to be released first, and — verified against Milvus 2.6.4 — a newly
    /// added field is immediately queryable on an already-loaded collection, with no release/reload
    /// required.
    /// </para>
    /// </remarks>
    public async Task AddCollectionFieldAsync(FieldSchema field, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(field);

        var request = new AddCollectionFieldRequest
        {
            CollectionName = Name,
            Schema = field.ToGrpc().ToByteString()
        };

        await _client.InvokeAsync(_client.GrpcClient.AddCollectionFieldAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sets or removes properties on an existing field. Available since Milvus v2.6.
    /// </summary>
    /// <param name="fieldName">The name of the field to alter.</param>
    /// <param name="properties">
    /// Properties to set, e.g. <c>max_length</c> on a <c>VarChar</c> field, or <c>mmap.enabled</c> on
    /// any field. At least one of <paramref name="properties" /> or <paramref name="deleteKeys" /> must
    /// be non-empty.
    /// </param>
    /// <param name="deleteKeys">
    /// Property keys to remove. Milvus only accepts a fixed set of recognized property names here --
    /// unrecognized keys are rejected even if never set on this field, so this is not a general-purpose
    /// key removal.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Both <paramref name="properties" /> and <paramref name="deleteKeys" /> are empty.
    /// </exception>
    /// <exception cref="MilvusException">
    /// <paramref name="fieldName" /> does not exist, or a key in <paramref name="deleteKeys" /> is not
    /// one Milvus recognizes as a field property.
    /// </exception>
    /// <remarks>
    /// Unlike a typical database, <c>max_length</c> can be both increased and decreased freely --
    /// confirmed empirically against 2.6.4, on both an empty field and one already holding data longer
    /// than the new limit.
    /// </remarks>
    public async Task AlterCollectionFieldAsync(
        string fieldName,
        IReadOnlyDictionary<string, string>? properties = null,
        IReadOnlyList<string>? deleteKeys = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(fieldName);

        if ((properties is null || properties.Count == 0) && (deleteKeys is null || deleteKeys.Count == 0))
        {
            throw new ArgumentException(
                $"At least one of {nameof(properties)} or {nameof(deleteKeys)} must be non-empty.");
        }

        var request = new AlterCollectionFieldRequest { CollectionName = Name, FieldName = fieldName };

        if (properties is not null)
        {
            foreach (KeyValuePair<string, string> property in properties)
            {
                request.Properties.Add(new Grpc.KeyValuePair { Key = property.Key, Value = property.Value });
            }
        }

        if (deleteKeys is not null)
        {
            request.DeleteKeys.AddRange(deleteKeys);
        }

        await _client.InvokeAsync(_client.GrpcClient.AlterCollectionFieldAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a function to an existing collection. Available since Milvus v2.6.
    /// </summary>
    /// <param name="function">
    /// The function to add. Its input and output fields must already exist in the collection -- this
    /// does not create them.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <exception cref="MilvusException">
    /// Always, for a <see cref="FunctionType.Bm25" /> function as of Milvus 2.6.20 -- see the remarks.
    /// Also thrown for an input or output field that does not exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>Not currently usable for BM25, the only function type this SDK can build.</b> Verified against
    /// two server versions: Milvus 2.6.4 does not implement this RPC at all (the call fails at the gRPC
    /// transport level with an <c>Unimplemented</c> status, surfacing as a raw
    /// <see cref="global::Grpc.Core.RpcException" /> rather than a <see cref="MilvusException" />).
    /// Milvus 2.6.20 does implement the RPC, but rejects a BM25 function outright with <c>"currently
    /// does not support adding BM25 function"</c>. <see cref="FunctionType.Rerank" /> and
    /// <see cref="FunctionType.TextEmbedding" /> are unverified -- this SDK has no builder for either,
    /// see <see cref="FunctionSchema" />'s constructor -- so whether they fare any better is unknown.
    /// </para>
    /// <para>
    /// Because <see cref="AddCollectionFieldAsync" /> refuses vector fields, a function's output field
    /// -- typically a sparse vector for BM25 -- has to already be part of the collection's original
    /// schema for this to have anything to attach to, once it does become usable. Until then, that
    /// field behaves as an ordinary column: it must be supplied on every insert, exactly like any other
    /// non-nullable field.
    /// </para>
    /// </remarks>
    public async Task AddCollectionFunctionAsync(FunctionSchema function, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(function);

        var request = new AddCollectionFunctionRequest { CollectionName = Name, FunctionSchema = function.ToGrpc() };

        await _client.InvokeAsync(_client.GrpcClient.AddCollectionFunctionAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces the definition of an existing function. Available since Milvus v2.6.
    /// </summary>
    /// <param name="functionName">The name of the function to alter.</param>
    /// <param name="newFunction">The function's new definition, which replaces the old one entirely.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <exception cref="MilvusException">
    /// Always, for a <see cref="FunctionType.Bm25" /> function as of Milvus 2.6.20: rejected with
    /// <c>"currently does not support alter BM25 function"</c>, the same restriction documented on
    /// <see cref="AddCollectionFunctionAsync" />. Also thrown when <paramref name="functionName" />
    /// does not exist.
    /// </exception>
    public async Task AlterCollectionFunctionAsync(
        string functionName, FunctionSchema newFunction, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(functionName);
        Verify.NotNull(newFunction);

        var request = new AlterCollectionFunctionRequest
        {
            CollectionName = Name,
            FunctionName = functionName,
            FunctionSchema = newFunction.ToGrpc()
        };

        await _client.InvokeAsync(_client.GrpcClient.AlterCollectionFunctionAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a function from a collection. Available since Milvus v2.6.
    /// </summary>
    /// <param name="functionName">The name of the function to drop.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// Dropping a function that does not exist was observed to succeed silently on Milvus 2.6.20
    /// rather than throw -- confirmed both for a function name that was never valid, and for one whose
    /// add had itself failed (see <see cref="AddCollectionFunctionAsync" />). Whether this holds for a
    /// function that genuinely existed and was actively producing output could not be verified: adding
    /// a function currently fails for every function type this SDK can construct, so there was nothing
    /// real to drop. On Milvus 2.6.4 this RPC is not implemented at all, the same as
    /// <see cref="AddCollectionFunctionAsync" /> and <see cref="AlterCollectionFunctionAsync" />.
    /// </para>
    /// </remarks>
    public async Task DropCollectionFunctionAsync(string functionName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(functionName);

        var request = new DropCollectionFunctionRequest { CollectionName = Name, FunctionName = functionName };

        await _client.InvokeAsync(_client.GrpcClient.DropCollectionFunctionAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Renames a collection.
    /// </summary>
    /// <param name="newName">The new collection name.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task RenameAsync(string newName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(newName);

        var request = new RenameCollectionRequest { OldName = Name, NewName = newName };

        await _client.InvokeAsync(_client.GrpcClient.RenameCollectionAsync, request, cancellationToken)
            .ConfigureAwait(false);

        Name = newName;
    }

    /// <summary>
    /// Drops a collection.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task DropAsync(CancellationToken cancellationToken = default)
    {
        var request = new DropCollectionRequest { CollectionName = Name };

        await _client.InvokeAsync(_client.GrpcClient.DropCollectionAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the current number of entities in the collection. Call
    /// <see cref="FlushAsync(System.Threading.CancellationToken)" /> before invoking this method to ensure up-to-date
    /// results.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>The number of entities currently in the collection.</returns>
    public async Task<int> GetEntityCountAsync(CancellationToken cancellationToken = default)
    {
        var request = new GetCollectionStatisticsRequest { CollectionName = Name };

        GetCollectionStatisticsResponse response = await _client.InvokeAsync(
            _client.GrpcClient.GetCollectionStatisticsAsync,
            request,
            static r => r.Status, cancellationToken).ConfigureAwait(false);


        return response.Stats.FirstOrDefault(kvp => kvp.Key == "row_count") is Grpc.KeyValuePair kvp &&
               int.TryParse(kvp.Value, out int numRows)
            ? numRows
            : throw new InvalidOperationException("Invalid or missing 'row_count'");
    }

    /// <summary>
    /// Loads a collection into memory so that it can be searched or queried.
    /// </summary>
    /// <param name="replicaNumber">An optional replica number to load.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task LoadAsync(int? replicaNumber = null, CancellationToken cancellationToken = default)
    {
        var request = new LoadCollectionRequest { CollectionName = Name };

        if (replicaNumber is not null)
        {
            Verify.GreaterThanOrEqualTo(replicaNumber.Value, 1);

            request.ReplicaNumber = replicaNumber.Value;
        }

        await _client.InvokeAsync(_client.GrpcClient.LoadCollectionAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Releases a collection that has been previously loaded.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ReleaseAsync(CancellationToken cancellationToken = default)
    {
        var request = new ReleaseCollectionRequest { CollectionName = Name };

        await _client.InvokeAsync(_client.GrpcClient.ReleaseCollectionAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the loading progress for a collection, and optionally one or more of its partitions.
    /// </summary>
    /// <param name="partitionNames">
    /// An optional list of partition names for which to check the loading progress.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>The loading progress of the collection.</returns>
    public async Task<long> GetLoadingProgressAsync(
        IReadOnlyList<string>? partitionNames = null,
        CancellationToken cancellationToken = default)
    {
        GetLoadingProgressRequest request = new() { CollectionName = Name };

        if (partitionNames is not null)
        {
            request.PartitionNames.AddRange(partitionNames);
        }

        GetLoadingProgressResponse response =
            await _client.InvokeAsync(_client.GrpcClient.GetLoadingProgressAsync, request, static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        return response.Progress;
    }

    /// <summary>
    /// Polls Milvus for loading progress of a collection until it is fully loaded.
    /// To perform a single progress check, use <see cref="GetLoadingProgressAsync" />.
    /// </summary>
    /// <param name="partitionNames">
    /// An optional list of partition names for which to check the loading progress.
    /// </param>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">How long to poll for before throwing a <see cref="TimeoutException" />.</param>
    /// <param name="progress">Provides information about the progress of the loading operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task WaitForCollectionLoadAsync(
        IReadOnlyList<string>? partitionNames = null,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        partitionNames ??= Array.Empty<string>();

        await Utils.Poll(
            async () =>
            {
                long progress = await GetLoadingProgressAsync(partitionNames, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return (progress == 100, progress);
            },
            $"Timeout when waiting for collection '{Name}' to load",
            waitingInterval, timeout, progress, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Compacts the collection.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>The compaction ID.</returns>
    public async Task<long> CompactAsync(CancellationToken cancellationToken = default)
    {
        MilvusCollectionDescription description = await DescribeAsync(cancellationToken).ConfigureAwait(false);

        ManualCompactionResponse response = await _client.InvokeAsync(_client.GrpcClient.ManualCompactionAsync,
            new ManualCompactionRequest { CollectionID = description.CollectionId, Timetravel = 0 },
            static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.CompactionID;
    }

    private static object? ConvertFromValueField(Grpc.ValueField valueField) =>
        valueField.DataCase switch
        {
            Grpc.ValueField.DataOneofCase.BoolData => valueField.BoolData,
            Grpc.ValueField.DataOneofCase.IntData => valueField.IntData,
            Grpc.ValueField.DataOneofCase.LongData => valueField.LongData,
            Grpc.ValueField.DataOneofCase.FloatData => valueField.FloatData,
            Grpc.ValueField.DataOneofCase.DoubleData => valueField.DoubleData,
            Grpc.ValueField.DataOneofCase.StringData => valueField.StringData,
            Grpc.ValueField.DataOneofCase.BytesData => valueField.BytesData.ToByteArray(),
            _ => null
        };
}
