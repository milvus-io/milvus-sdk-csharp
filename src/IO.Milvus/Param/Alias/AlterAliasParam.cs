namespace IO.Milvus.Param.Alias
{
    public class AlterAliasParam
    {
        public static AlterAliasParam Create(
            string collectionName,
            string alias)
        {
            var param = new AlterAliasParam()
            {
                CollectionName = collectionName,
                Alias = alias
            };
            param.Check();

            return param;
        }

        #region Properties
        public string Alias { get; set; }

        public string CollectionName { get; set; }
        #endregion

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, "Collection name");
            ParamUtils.CheckNullEmptyString(Alias, "Alias");
        }
    }

}
