using System;

namespace IO.Milvus.Param.Index
{
    public class DescribeIndexParam
    {
        public static DescribeIndexParam Create(string collectionName,
            string indexName)
        {
            DescribeIndexParam param = new DescribeIndexParam()
            {
                CollectionName = collectionName,
                IndexName = indexName
            };
            param.Check();

            return param;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, "Collection name");

            if (string.IsNullOrEmpty(IndexName))
            {
                IndexName = Constant.DEFAULT_INDEX_NAME;
            }

        }

        public string CollectionName { get; set; }

        public string IndexName { get; set; }
    }
}
