using System;

namespace IO.Milvus.Utils;

/// <summary>
/// Timestamps methods
/// </summary>
/// <remarks>
/// <see href="https://www.epochconverter.com/"/>
/// </remarks>
internal static class TimestampUtils
{
    public static long GetNowUTCTimestamp()
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public static long ToUtcTimestamp(this DateTime dt)
    {
        return ToTimestamp(dt.ToUniversalTime());
    }

    public static long ToTimestamp(this DateTime dt)
    {
        return (dt.Ticks - 621355968000000000) / 10000;
    }

    public static DateTime GetTimeFromTimstamp(long timestamp)
    {
        if (timestamp > 253402300799999)
        {
            return DateTime.Now;
        }
        else
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        }
    }
}