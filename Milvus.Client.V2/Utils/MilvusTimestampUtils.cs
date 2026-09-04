namespace Milvus.Client.V2.Utils;

/// <summary>
/// Utilities for converting <see cref="DateTime" /> to Milvus timestamps and back.
/// </summary>
/// <remarks>
/// For more information about Milvus timestamps, see <see href="https://milvus.io/docs/timestamp.md" />.
/// </remarks>
public static class MilvusTimestampUtils
{
    /// <summary>
    /// Converts a <see cref="DateTime" /> to a Milvus timestamp, suitable for use as a guarantee timestamp or a time
    /// travel timestamp in search/query requests.
    /// </summary>
    /// <param name="dateTime">A UTC <see cref="DateTime" />.</param>
    /// <returns>
    /// A Milvus timestamp. Note that Milvus timestamps contain an opaque internal component which isn't converted, so
    /// timestamps cannot be fully round-tripped to <see cref="DateTime" />.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public static ulong FromDateTime(DateTime dateTime)
        => dateTime.Kind == DateTimeKind.Utc
            ? ((ulong)dateTime.Ticks - UnixEpochTicks) / 10000 << LogicalBits
            : throw new ArgumentException("Only UTC DateTimes are supported", nameof(dateTime));

    /// <summary>
    /// Converts a Milvus timestamp to a <see cref="DateTime" />.
    /// </summary>
    /// <param name="timestamp">A Milvus timestamp.</param>
    /// <returns>
    /// A UTC <see cref="DateTime" />. Note that Milvus timestamps contain an opaque internal component which isn't
    /// converted, so timestamps cannot be fully round-tripped to <see cref="DateTime" />.
    /// </returns>
    public static DateTime ToDateTime(ulong timestamp)
    {
        // Zero out the 18-bit logical component of the Milvus timestamp, leaving only milliseconds since Unix epoch
        timestamp = (timestamp & LogicalBitmask) >> LogicalBits;
        DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp);
        return DateTime.SpecifyKind(dto.DateTime, DateTimeKind.Utc);
    }

    private const int LogicalBits = 18;
    private const ulong LogicalBitmask = ~(((ulong)1 << LogicalBits) - 1);

    private const ulong UnixEpochTicks = 621355968000000000;
}
