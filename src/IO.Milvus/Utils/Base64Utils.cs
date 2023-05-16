using System;
using System.Text;

namespace IO.Milvus.Utils
{
    public static class Base64Utils
    {
        public static string ToBase64(this string value)
        {
            return Convert.ToBase64String(Encoding.GetEncoding("utf-8").GetBytes(value));
        }
    }
}
