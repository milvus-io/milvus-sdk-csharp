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
    /// <exception cref="ArgumentException">
    /// <paramref name="dateTime" /> is not <see cref="DateTimeKind.Utc" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="dateTime" /> is before the Unix epoch (1970-01-01T00:00:00Z), which cannot be represented.
    /// </exception>
    public static ulong FromDateTime(DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Only UTC DateTimes are supported", nameof(dateTime));
        }

        if (dateTime.Ticks < (long)UnixEpochTicks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateTime), "DateTimes before the Unix epoch cannot be converted to a Milvus timestamp.");
        }

        ulong millis = ((ulong)dateTime.Ticks - UnixEpochTicks) / 10000;

        // The 64-bit timestamp is 46-bit physical (ms) + 18-bit logical. An ms value at or above 2^46 would
        // overflow the ulong shift and silently wrap to a wrong timestamp, so guard it consistent with the
        // pre-epoch check. 2^46 ms is ~year 4197, which is still far before DateTime.MaxValue (year 9999,
        // ~2.53e14 ms); the guard is needed precisely because DateTime.MaxValue's ms exceeds 2^46 (~7.04e13),
        // so it would otherwise overflow and wrap.
        const ulong PhysicalBitsMask = 1UL << (64 - LogicalBits);
        if (millis >= PhysicalBitsMask)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                $"The given DateTime is too far in the future to be represented as a Milvus timestamp.");
        }

        return millis << LogicalBits;
    }

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
