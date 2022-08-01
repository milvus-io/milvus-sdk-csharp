using System.Text.RegularExpressions;

namespace IO.Milvus.Workbench.Utils
{
    public static class IPValidationUtils
    {
        public static bool IsHost(string ip)
        {
            //判断是否为IP
            return string.Equals(ip, "localhost", System.StringComparison.OrdinalIgnoreCase) ||
                   IsIP(ip);
        }

        public static bool IsIP(string ip)
        {
            //判断是否为IP
            return Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
        }
    }
}