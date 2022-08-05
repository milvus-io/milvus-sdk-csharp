namespace IO.Milvus.Param.Collection
{
    public class ReleaseCollectionParam
    {
        public static ReleaseCollectionParam Create(string collectionName)
        {
            var param = new ReleaseCollectionParam()
            {
                CollectionName = collectionName
            };
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, $"{nameof(ReleaseCollectionParam)}.{nameof(CollectionName)}");
        }

        public override string ToString()
        {
            return $"{nameof(ReleaseCollectionParam)}{{{nameof(CollectionName)}=/'{CollectionName}/'}}";
        }
    }
}
