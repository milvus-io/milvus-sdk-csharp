using System;

namespace IO.Milvus.Utils;

internal static class TimestampUtils
{
    public static long GetNowUTCTimestamp(bool accurateToMilliseconds = false)
    {
        return ToUTCTimestamp(DateTime.Now, accurateToMilliseconds);
    }

    public static long ToUTCTimestamp(this DateTime dt, bool accurateToMilliseconds = false)
    {
        return ToTimestamp(dt.ToUniversalTime(), accurateToMilliseconds);
    }

    public static long ToTimestamp(this DateTime dt, bool accurateToMilliseconds = false)
    {
        if (accurateToMilliseconds)
        {
            return (dt.Ticks - 621355968000000000) / 10000;
        }
        else
        {
            return (dt.Ticks - 621355968000000000) / 10000000;
        }
    }

    public static DateTime GetTimeFromTimstamp(long Timestamp, bool accurateToMilliseconds = false)
    {
        var startTime = new DateTime(1970, 1, 1);
        if (accurateToMilliseconds)
        {
            return startTime.AddTicks(Timestamp * 10000);
        }
        else
        {
            return startTime.AddTicks(Timestamp * 10000000);
        }
    }
}
