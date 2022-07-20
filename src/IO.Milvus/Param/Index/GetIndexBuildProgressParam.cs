namespace IO.Milvus.Param.Index
{
    public class GetIndexBuildProgressParam
    {
        public static GetIndexBuildProgressParam Create(string collectionName,
           string indexName)
        {
            GetIndexBuildProgressParam param = new GetIndexBuildProgressParam()
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
