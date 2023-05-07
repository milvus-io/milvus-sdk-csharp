using System.Collections;

namespace IO.Milvus.Utils
{
    public static class CollectionUtils
    {
        public static bool IsNotEmpty(this ICollection list)
        {
            return list != null && list.Count > 0;
        }

        public static bool IsEmpty(this ICollection list)
        {
            return !list.IsNotEmpty();
        }
    }
}
