using IO.Milvus.Exception;

namespace IO.Milvus.Param
{
    /// <summary>
    /// Utility functions for param classes
    /// </summary>
    public class ParamUtils
    {
        /// <summary>
        ///  Checks if a string is empty or null.
        ///  Throws <see cref="ParamException"/> if the string is empty of null.
        /// </summary>
        /// <param name="target">target string</param>
        /// <param name="name">a name to describe this string</param>
        /// <exception cref="ParamException"></exception>
        public static void CheckNullEmptyString(string target, string name)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ParamException(name + " cannot be null or empty");
            }
        }

        /// <summary>
        /// Checks if a metric is for float vector.
        /// </summary>
        /// <param name="metric">metirc type</param>
        /// <returns></returns>
        public static bool IsFloatMetric(MetricType metric)
        {
            return metric == MetricType.L2 || metric == MetricType.IP;
        }

        /// <summary>
        /// Checks if a metric is for binary vector.
        /// </summary>
        /// <param name="metric"></param>
        /// <returns></returns>
        public static bool IsBinaryMetric(MetricType metric)
        {
            return !IsFloatMetric(metric);
        }
    }
}
