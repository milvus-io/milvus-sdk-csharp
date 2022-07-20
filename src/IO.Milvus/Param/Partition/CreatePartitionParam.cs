namespace IO.Milvus.Param.Partition
{
    public class CreatePartitionParam
    {
        public static CreatePartitionParam Create(string collectionName,
            string partitionName)
        {
            var param = new CreatePartitionParam()
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
            return $"{nameof(CreatePartitionParam)}{{{nameof(CollectionName)}=/'{CollectionName}/', {nameof(PartitionName)}=/'{PartitionName}/'}}";

        }
    }
}
