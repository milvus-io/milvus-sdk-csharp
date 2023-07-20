namespace IO.Milvus.Utils;

internal static class TimestampUtils
{
    internal static long GetNowUtcTimestamp()
        => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    internal static long ToUtcTimestamp(this DateTime dt)
        => ToTimestamp(dt.ToUniversalTime());

    internal static long ToTimestamp(this DateTime dt)
        => (dt.Ticks - 621355968000000000) / 10000;

    internal static DateTime GetTimeFromTimestamp(long timestamp)
        => timestamp > 253402300799999
            ? DateTime.Now
            : DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
}
