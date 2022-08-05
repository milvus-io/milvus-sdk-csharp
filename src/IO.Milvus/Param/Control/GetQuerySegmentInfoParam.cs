namespace IO.Milvus.Param.Control
{
    public class GetQuerySegmentInfoParam
    {
        public static GetQuerySegmentInfoParam Create(string collectionName)
        {
            var param = new GetQuerySegmentInfoParam()
            {
                CollectionName = collectionName
            };
            param.Check();

            return param;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, nameof(CollectionName));
        }

        public string CollectionName { get; set; }
    }
}
