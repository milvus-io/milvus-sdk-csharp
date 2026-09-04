using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to compact a collection, merging small segments into larger ones.
/// </summary>
public sealed class CompactReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to compact.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// Whether to perform a major compaction, merging all segments regardless of their size.
    /// </summary>
    public bool IsMajorCompaction { get; set; }

    /// <summary>
    /// When true, compacts only the L0 delta segments (unindexed, unsorted recent writes) into sealed segments.
    /// </summary>
    public bool IsL0Compaction { get; set; }

    /// <summary>
    /// The target segment size for the compaction. When set, the server aims to produce segments of at most this
    /// size. The value is interpreted in <see cref="TargetSizeUnit" /> units (default <c>"mb"</c>) and converted
    /// to MB before being sent to the server.
    /// </summary>
    public long? TargetSize { get; set; }

    /// <summary>
    /// The unit of <see cref="TargetSize" />: <c>"b"</c>, <c>"kb"</c>, <c>"mb"</c> (default), <c>"gb"</c>,
    /// <c>"tb"</c> or <c>"pb"</c>. Mirrors the Java SDK's <c>targetSizeUnit</c>.
    /// </summary>
    public string? TargetSizeUnit { get; set; }

    internal Grpc.ManualCompactionRequest ToGrpcManualCompactionRequest(long collectionId)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        var request = new Grpc.ManualCompactionRequest
        {
            CollectionID = collectionId,
            CollectionName = CollectionName,
            MajorCompaction = IsMajorCompaction,
            L0Compaction = IsL0Compaction
        };

        if (TargetSize is { } targetSize)
        {
            request.TargetSize = ConvertTargetSizeToMB(targetSize);
        }

        request.DbName = DatabaseName ?? "";
        return request;
    }

    /// <summary>
    /// Converts the requested <see cref="TargetSize" /> (with an optional unit, default MB) into MB, rejecting
    /// non-positive values and values too small to reach 1 MB. Mirrors the Java SDK's <c>convertTargetSizeToMB</c>
    /// (and PyMilvus's <c>parse_target_size</c>).
    /// </summary>
    private long ConvertTargetSizeToMB(long targetSize)
    {
        if (targetSize <= 0)
        {
            throw new ArgumentException($"targetSize must be a positive integer, got {targetSize}");
        }

        string? targetSizeUnit = TargetSizeUnit;
        string unit;
        if (string.IsNullOrWhiteSpace(targetSizeUnit))
        {
            unit = "mb";
        }
        else
        {
#pragma warning disable CA1308, CA1847 // The unit contract is lowercase; the string overload is required for netstandard2.0/net462.
            unit = targetSizeUnit!.Trim().ToLowerInvariant();
#pragma warning restore CA1308, CA1847
        }

        decimal unitBytes = unit switch
        {
            "b" => 1,
            "kb" => 1024,
            "mb" => 1024M * 1024M,
            "gb" => 1024M * 1024M * 1024M,
            "tb" => 1024M * 1024M * 1024M * 1024M,
            "pb" => 1024M * 1024M * 1024M * 1024M * 1024M,
            _ => throw new ArgumentException(
                $"Invalid targetSizeUnit: '{TargetSizeUnit}'. Supported units: b, kb, mb, gb, tb, pb")
        };

        decimal sizeMb;
        try
        {
            sizeMb = decimal.Floor(targetSize * unitBytes / (1024M * 1024M));
        }
        catch (OverflowException)
        {
            // For the tb/pb units a large TargetSize overflows decimal; fail with the clear message below
            // instead of leaking OverflowException.
            sizeMb = long.MaxValue + 1M;
        }

        if (sizeMb > long.MaxValue)
        {
            throw new ArgumentException(
                $"target size too large: {targetSize}{TargetSizeUnit}, must be at most {long.MaxValue}MB");
        }

        if (sizeMb <= 0)
        {
            throw new ArgumentException(
                $"target size too small: {targetSize}{TargetSizeUnit}, must be at least 1MB");
        }

        return (long)sizeMb;
    }
}
