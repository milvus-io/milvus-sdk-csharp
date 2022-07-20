using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
