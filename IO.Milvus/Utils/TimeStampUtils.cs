namespace IO.Milvus.Utils;

/// <summary>
/// Timestamps methods
/// </summary>
/// <remarks>
/// <see href="https://www.epochconverter.com/"/>
/// </remarks>
internal static class TimestampUtils
{
    public static long GetNowUtcTimestamp()
        => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static long ToUtcTimestamp(this DateTime dt)
        => ToTimestamp(dt.ToUniversalTime());

    public static long ToTimestamp(this DateTime dt)
        => (dt.Ticks - 621355968000000000) / 10000;

    public static DateTime GetTimeFromTimestamp(long timestamp)
        => timestamp > 253402300799999
            ? DateTime.Now
            : DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
}
