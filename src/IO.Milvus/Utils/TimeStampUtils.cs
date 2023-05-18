using System;

namespace IO.Milvus.Utils;

internal static class TimeStampUtils
{
    public static long GetTimeStamp(bool accurateToMilliseconds = false)
    {
        if (accurateToMilliseconds)
        {
            return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }
        else
        {
            return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }

    public static long ToTimestamp(this DateTime dt, bool accurateToMilliseconds = false)
    {
        if (accurateToMilliseconds)
        {
            return (dt.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }
        else
        {
            return (dt.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }
}
