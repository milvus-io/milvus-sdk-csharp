using System.Diagnostics.CodeAnalysis;

namespace IO.Milvus.Param.Collection
{
    public class GetCollectionStatisticsParam
    {
        /// <summary>
        /// Collection name cannot be empty or null.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Requires a flush action before retrieving collection statistics.
        /// </summary>
        public bool IsFlushCollection { get; set; } = true;

        public static GetCollectionStatisticsParam Create(string collectionName,
            bool isFlushCollection = true)
        {
            var param = new GetCollectionStatisticsParam()
            {
                CollectionName = collectionName,
                IsFlushCollection = isFlushCollection
            };
            param.Check();

            return param;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName,
                $"{nameof(GetCollectionStatisticsParam)}.{nameof(CollectionName)}");
        }
    }
}
