namespace IO.Milvus.Param.Partition
{
    public class HasPartitionParam
    {
        public static HasPartitionParam Create(string collectionName,
            string partitionName)
        {
            var param = new HasPartitionParam()
            {
                CollectionName = collectionName,
                PartitionName = partitionName
            };

            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        public string PartitionName { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, "Collection name");
            ParamUtils.CheckNullEmptyString(PartitionName, "Partition name");
        }

        public override string ToString()
        {
            return $"{nameof(HasPartitionParam)}{{{nameof(CollectionName)}=/'{CollectionName}/', {nameof(PartitionName)}=/'{PartitionName}/'}}";

        }
    }
}
