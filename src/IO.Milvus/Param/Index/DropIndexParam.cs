namespace IO.Milvus.Param.Index
{
    public class DropIndexParam
    {
        public static DropIndexParam Create(
            string collectionName,
            string indexName)
        {
            var param = new DropIndexParam()
            {
                CollectionName = collectionName,
                IndexName = indexName
            };
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        public string IndexName { get; set; } = Constant.DEFAULT_INDEX_NAME;

        internal void Check()
        {
            if (string.IsNullOrEmpty(IndexName))
            {
                IndexName = Constant.DEFAULT_INDEX_NAME;
            }
        }
    }
}
