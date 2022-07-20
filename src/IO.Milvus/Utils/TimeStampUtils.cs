using System;

namespace IO.Milvus.Utils
{
    public static class TimeStampUtils
    {
        public static long GetTimeStamp(bool AccurateToMilliseconds = false)
        {
            if (AccurateToMilliseconds)
            {
                return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            }
            else
            {
                return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            }
        }
    }
}
