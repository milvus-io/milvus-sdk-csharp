namespace IO.Milvus.Param.Dml
{
    public class DeleteParam
    {
        public static DeleteParam Create(
            string collectionName,
            string expr,
            string partitionName = "")
        {
            var param = new DeleteParam()
            {
                CollectionName = collectionName,
                Expr = expr,
                PartitionName = partitionName
            };
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        public string PartitionName { get; set; }

        public string Expr { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, "Collection name");
            ParamUtils.CheckNullEmptyString(Expr, "Expression");
        }
    }
}
