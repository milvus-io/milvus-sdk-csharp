using System.Globalization;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to create one or more indexes, mirroring the Java SDK's <c>CreateIndexReq</c> and the
/// C++ <c>CreateIndexRequest</c>. Each index is described by an <see cref="IndexParam" /> (the input analogue of
/// the Java <c>IndexParam</c>); <c>CreateIndexAsync</c> issues one gRPC create-index call per entry and,
/// when <see cref="Sync" /> is set, waits for each to finish building.
/// </summary>
public sealed class CreateIndexReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The indexes to create, one entry per field/index. Must not be empty.
    /// </summary>
    public IReadOnlyList<IndexParam> Indexes { get; set; } = Array.Empty<IndexParam>();

    /// <summary>
    /// Whether to wait until the index is fully built. Defaults to <c>true</c>, matching the C++ and Java SDKs.
    /// </summary>
    public bool Sync { get; set; } = true;

    /// <summary>
    /// The maximum time in milliseconds to wait for each index to build when <see cref="Sync" /> is enabled.
    /// Defaults to 60000. A value of 0 means wait forever.
    /// </summary>
    public long TimeoutMs { get; set; } = 60000;

    internal Grpc.CreateIndexRequest ToGrpcCreateIndexRequest(IndexParam index)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(index);
        Verify.NotNullOrWhiteSpace(index.FieldName);

        var request = new Grpc.CreateIndexRequest
        {
            CollectionName = CollectionName,
            FieldName = index.FieldName,
            IndexName = string.IsNullOrEmpty(index.IndexName) ? Constants.DefaultIndexName : index.IndexName
        };

        if (index.IndexType is not null)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.IndexType,
                Value = index.IndexType.Value.ToWireString()
            });
        }

        if (index.MetricType is not null)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.MetricType,
                Value = index.MetricType.Value.ToWireString()
            });
        }

        foreach (KeyValuePair<string, object> parameter in index.ExtraParams)
        {
            // The server requires a positive integer dimension, matching the Java SDK's isLegalDimensionValue
            // check (accepts only the integral types; floats, booleans, null and numeric strings are rejected).
            // Range-check against long.MaxValue so an oversized ulong/BigInteger dimension fails here with
            // ArgumentException instead of a raw OverflowException from Convert.ToInt64.
            if (parameter.Key == Constants.VectorDim
                && (!IsIntegral(parameter.Value)
                    || parameter.Value is ulong ulongValue && ulongValue > long.MaxValue
                    || parameter.Value is System.Numerics.BigInteger bigIntegerValue && bigIntegerValue > long.MaxValue
                    || Convert.ToInt64(parameter.Value, System.Globalization.CultureInfo.InvariantCulture) <= 0))
            {
                throw new ArgumentException(
                    $"Dimension must be a positive integer, got: '{parameter.Value}'.", nameof(index));
            }

            request.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = parameter.Key,
                // Mirror Java's String.valueOf for the arbitrary object values a caller may supply.
                Value = Convert.ToString(parameter.Value, System.Globalization.CultureInfo.InvariantCulture) ?? ""
            });
        }

        request.DbName = DatabaseName ?? "";
        return request;
    }

    private static bool IsIntegral(object value)
        => value is sbyte or byte or short or ushort or int or uint or long or ulong or System.Numerics.BigInteger;
}
