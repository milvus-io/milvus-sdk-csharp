namespace IO.Milvus.Param.Collection
{
    public class DescribeCollectionParam
    {
        public static DescribeCollectionParam Create(string collectionName)
        {
            var param = new DescribeCollectionParam()
            {
                CollectionName = collectionName
            };
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, $"{nameof(DescribeCollectionParam)}.{nameof(CollectionName)}");
        }

        public override string ToString()
        {
            return $"{nameof(DescribeCollectionParam)}{{{nameof(CollectionName)}=/'{CollectionName}/'}}";
        }
    }
}
