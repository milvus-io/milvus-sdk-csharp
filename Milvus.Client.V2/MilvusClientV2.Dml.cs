using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Inserts rows into a collection, recording the mutation timestamp for Session consistency
    /// (see design doc §5.1.2).
    /// </summary>
    /// <example>
    /// Row-based insert:
    /// <code>
    /// MutationResp result = await client.InsertAsync(new InsertReq
    /// {
    ///     CollectionName = "book",
    ///     RowsData =
    ///     [
    ///         new Dictionary&lt;string, object?&gt; { ["id"] = 1L, ["title"] = "A", ["vector"] = new[] { 0.1f, 0.2f } },
    ///         new Dictionary&lt;string, object?&gt; { ["id"] = 2L, ["title"] = "B", ["vector"] = new[] { 0.3f, 0.4f } }
    ///     ]
    /// });
    /// </code>
    /// Column-based insert:
    /// <code>
    /// await client.InsertAsync(new InsertReq
    /// {
    ///     CollectionName = "book",
    ///     ColumnsData =
    ///     [
    ///         FieldData.Create("id", new long[] { 1, 2 }),
    ///         FieldData.CreateVarChar("title", new[] { "A", "B" })
    ///     ]
    /// });
    /// </code>
    /// <see cref="InsertReq.ColumnsData" /> and <see cref="InsertReq.RowsData" /> are mutually exclusive.
    /// </example>
    /// <param name="request">The request containing the collection name and field data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task<MutationResp> InsertAsync(
        InsertReq request,
        CancellationToken cancellationToken = default)
        => InsertAsync(request, allowRetry: true, cancellationToken);

    private async Task<MutationResp> InsertAsync(
        InsertReq request,
        bool allowRetry,
        CancellationToken cancellationToken)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        string databaseName = ResolveDatabaseName(request.DatabaseName);

        // Describe the collection (cache-served) to stamp the request with the schema timestamp, so the
        // server can detect a stale schema and return SchemaMismatch instead of mis-encoding the data.
        DescribeCollectionResp description = await DescribeCollectionAsync(
            new Requests.Collection.DescribeCollectionReq
            {
                DatabaseName = request.DatabaseName,
                CollectionName = request.CollectionName
            },
            cancellationToken).ConfigureAwait(false);

        request.SchemaTimestamp = description.UpdateTimestamp;
        ApplyRowData(request, description.Schema, isUpsert: false);
        ValidateDataAgainstSchema(request.ColumnsData, description.Schema, isUpsert: false);
        Grpc.InsertRequest grpcRequest = request.ToGrpcInsertRequest();
        try
        {
            Grpc.MutationResult response = await InvokeAsync(
                    GrpcClient.InsertAsync, grpcRequest, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

            CollectionTsCache.Instance.Set(_endpoint, databaseName, request.CollectionName, unchecked((long)response.Timestamp));

            return MutationResp.FromGrpc(response);
        }
        catch (MilvusException ex) when (ex.ErrorCode == MilvusErrorCode.SchemaMismatch && allowRetry)
        {
            // The collection was recreated or its schema changed by another client between our last describe
            // and this insert. Drop the stale cached schema and retry once, mirroring the Java/C++ SDKs.
            SchemaCache.Instance.Invalidate(_endpoint, databaseName, request.CollectionName);
            return await InsertAsync(request, allowRetry: false, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Upserts (inserts or updates) rows into a collection, recording the mutation timestamp for Session
    /// consistency.
    /// </summary>
    /// <param name="request">The request containing the collection name and field data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task<MutationResp> UpsertAsync(
        UpsertReq request,
        CancellationToken cancellationToken = default)
        => UpsertAsync(request, allowRetry: true, cancellationToken);

    private async Task<MutationResp> UpsertAsync(
        UpsertReq request,
        bool allowRetry,
        CancellationToken cancellationToken)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        string databaseName = ResolveDatabaseName(request.DatabaseName);

        // Same schema-timestamp stamping as InsertAsync (see above).
        DescribeCollectionResp description = await DescribeCollectionAsync(
            new Requests.Collection.DescribeCollectionReq
            {
                DatabaseName = request.DatabaseName,
                CollectionName = request.CollectionName
            },
            cancellationToken).ConfigureAwait(false);

        request.SchemaTimestamp = description.UpdateTimestamp;
        ApplyRowData(request, description.Schema, isUpsert: true);
        ValidateDataAgainstSchema(request.ColumnsData, description.Schema, isUpsert: true);
        Grpc.UpsertRequest grpcRequest = request.ToGrpcUpsertRequest();
        try
        {
            Grpc.MutationResult response = await InvokeAsync(
                    GrpcClient.UpsertAsync, grpcRequest, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

            CollectionTsCache.Instance.Set(_endpoint, databaseName, request.CollectionName, unchecked((long)response.Timestamp));

            return MutationResp.FromGrpc(response);
        }
        catch (MilvusException ex) when (ex.ErrorCode == MilvusErrorCode.SchemaMismatch && allowRetry)
        {
            // Same recreate/schema-change recovery as InsertAsync.
            SchemaCache.Instance.Invalidate(_endpoint, databaseName, request.CollectionName);
            return await UpsertAsync(request, allowRetry: false, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes rows from a collection by expression, recording the mutation timestamp for Session consistency.
    /// </summary>
    /// <param name="request">The request containing the collection name and delete expression.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<MutationResp> DeleteAsync(
        DeleteReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        string databaseName = ResolveDatabaseName(request.DatabaseName);

        string? primaryKeyField = null;
        if (request.Ids is { Count: > 0 })
        {
            DescribeCollectionResp description = await DescribeCollectionAsync(
                new Requests.Collection.DescribeCollectionReq
                {
                    DatabaseName = request.DatabaseName,
                    CollectionName = request.CollectionName
                },
                cancellationToken).ConfigureAwait(false);

            primaryKeyField = description.Schema.Fields.SingleOrDefault(f => f.IsPrimaryKey)?.Name
                ?? throw new MilvusException(MilvusErrorCode.UnexpectedError,
                    $"Collection '{request.CollectionName}' has no primary key field.");
        }

        Grpc.DeleteRequest grpcRequest = request.ToGrpcDeleteRequest(primaryKeyField);
        Grpc.MutationResult response = await InvokeAsync(
                GrpcClient.DeleteAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        CollectionTsCache.Instance.Set(_endpoint, databaseName, request.CollectionName, unchecked((long)response.Timestamp));

        return MutationResp.FromGrpc(response);
    }

    // Converts row-based data (if any) to columnar FieldData using the collection schema, replacing the
    // request's Data. Both input shapes cannot be set at once. RowsData is consumed (cleared) after the
    // conversion so a SchemaMismatch retry re-entering this method does not hit the mutual-exclusion guard.
    private static void ApplyRowData(InsertReq request, CollectionSchema schema, bool isUpsert)
    {
        if (request.RowsData.Count > 0)
        {
            if (request.ColumnsData.Count > 0)
            {
                throw new ArgumentException("Data and Rows cannot both be set.", nameof(request));
            }

            request.ColumnsData = RowDataConverter.ConvertRows(request.RowsData, schema, isUpsert);
            request.RowsData = Array.Empty<IDictionary<string, object?>>();
        }
    }

    private static void ApplyRowData(UpsertReq request, CollectionSchema schema, bool isUpsert)
    {
        if (request.RowsData.Count > 0)
        {
            if (request.ColumnsData.Count > 0)
            {
                throw new ArgumentException("Data and Rows cannot both be set.", nameof(request));
            }

            request.ColumnsData = RowDataConverter.ConvertRows(
                request.RowsData, schema, isUpsert, request.PartialUpdate);
            request.RowsData = Array.Empty<IDictionary<string, object?>>();
        }
    }

    // Validates the provided columnar data against the collection schema, mirroring the C++ SDK's
    // CheckInsertInput: per-row vector dimension, text/array length caps, sparse index/value range, and the
    // auto-ID primary-key presence rule. This catches data the server would reject with a clearer error.
    private static void ValidateDataAgainstSchema(
        IReadOnlyList<FieldData> data, CollectionSchema schema, bool isUpsert)
    {
        var schemaFields = new Dictionary<string, FieldSchema>(StringComparer.Ordinal);
        foreach (FieldSchema field in schema.Fields)
        {
            schemaFields[field.Name] = field;
        }

        var providedFields = new HashSet<string>(StringComparer.Ordinal);

        foreach (FieldData field in data)
        {
            if (field.IsDynamic || field.FieldName.Length == 0)
            {
                continue;
            }

            if (!schemaFields.TryGetValue(field.FieldName, out FieldSchema? schemaField))
            {
                // Unknown fields are either dynamic (handled above) or rejected server-side; skip.
                continue;
            }

            providedFields.Add(field.FieldName);
            ValidateFieldData(field, schemaField);
        }

        // Auto-ID primary key: upsert requires it; on insert the server decides whether explicitly provided
        // auto-id values are accepted (allow_insert_auto_id), matching RowDataConverter which forwards them.
        FieldSchema? primaryKey = schema.Fields.FirstOrDefault(f => f.IsPrimaryKey);
        if (primaryKey is { AutoId: true })
        {
            bool present = providedFields.Contains(primaryKey.Name);
            if (isUpsert && !present)
            {
                throw new ArgumentException(
                    $"Upsert requires the auto-id primary key field '{primaryKey.Name}' to be present in the data.",
                    nameof(data));
            }
        }
    }

    private static void ValidateFieldData(FieldData field, FieldSchema schemaField)
    {
        switch (field.DataType)
        {
            case DataType.FloatVector:
                ValidateVectorDimension<ReadOnlyMemory<float>>(field, schemaField, f => f.Length);
                break;
            case DataType.Float16Vector:
            case DataType.BFloat16Vector:
                ValidateVectorDimension<ReadOnlyMemory<ushort>>(field, schemaField, f => f.Length);
                break;
            case DataType.Int8Vector:
                ValidateVectorDimension<ReadOnlyMemory<sbyte>>(field, schemaField, f => f.Length);
                break;
            case DataType.BinaryVector:
                ValidateVectorDimension<ReadOnlyMemory<byte>>(field, schemaField, f => f.Length * 8);
                break;
            case DataType.SparseFloatVector:
                ValidateSparse(field);
                break;
            case DataType.VarChar:
            case DataType.String:
            case DataType.Geometry:
            case DataType.Timestamptz:
                if (schemaField.MaxLength is { } maxLength)
                {
                    ValidateTextLength(field, maxLength);
                }

                break;
            case DataType.Array:
                if (schemaField.MaxCapacity is { } maxCapacity)
                {
                    ValidateArrayCapacity(field, maxCapacity);
                }

                break;
        }
    }

    private static void ValidateVectorDimension<T>(FieldData field, FieldSchema schemaField, Func<T, int> lengthOf)
    {
        if (schemaField.Dimension is not { } expectedDim)
        {
            return;
        }

        if (field is not FieldData<T> typed)
        {
            throw new ArgumentException(
                $"Field '{field.FieldName}' has type {field.DataType} but its data does not match.");
        }

        for (int i = 0; i < typed.RowCount; i++)
        {
            // A nullable row marked invalid (valid_data=false) carries no vector payload; skip its dimension.
            if (!typed.IsRowValid(i))
            {
                continue;
            }

            if (lengthOf(typed.Data[i]) != expectedDim)
            {
                throw new ArgumentException(
                    $"Row {i} of field '{field.FieldName}' has dimension {lengthOf(typed.Data[i])}, but the schema " +
                    $"declares {expectedDim}.");
            }
        }
    }

    private static void ValidateSparse(FieldData field)
    {
        if (field is not SparseFloatVectorFieldData typed)
        {
            return;
        }

        for (int i = 0; i < typed.RowCount; i++)
        {
            MilvusSparseVector<float> vector = typed.Data[i];
            foreach (int index in vector.Indices.Span)
            {
                if (index < 0)
                {
                    throw new ArgumentException(
                        $"Row {i} of sparse field '{field.FieldName}' has a negative index {index}.");
                }
            }

            foreach (float value in vector.Values.Span)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    throw new ArgumentException(
                        $"Row {i} of sparse field '{field.FieldName}' has a NaN or infinite value.");
                }
            }
        }
    }

    private static void ValidateTextLength(FieldData field, int maxLength)
    {
        if (field is not FieldData<string> typed)
        {
            return;
        }

        for (int i = 0; i < typed.RowCount; i++)
        {
            string? value = typed.Data[i];
            if (value is not null && value.Length > maxLength)
            {
                throw new ArgumentException(
                    $"Row {i} of field '{field.FieldName}' has length {value.Length}, exceeding the schema max {maxLength}.");
            }
        }
    }

    private static void ValidateArrayCapacity(FieldData field, int maxCapacity)
    {
        for (int i = 0; i < field.RowCount; i++)
        {
            object? row = field.GetValueAsObject(i);
            if (row is System.Collections.ICollection collection && collection.Count > maxCapacity)
            {
                throw new ArgumentException(
                    $"Row {i} of array field '{field.FieldName}' has {collection.Count} elements, exceeding the " +
                    $"schema max capacity {maxCapacity}.");
            }
        }
    }
}
