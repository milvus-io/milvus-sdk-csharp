namespace IO.Milvus.Param.Control
{
    public class ManualCompactionParam
    {
        public static ManualCompactionParam Create(string collectionName)
        {
            var param = new ManualCompactionParam()
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
