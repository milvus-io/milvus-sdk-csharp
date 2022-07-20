namespace IO.Milvus.Param.Partition
{
    public class DropPartitionParam {
        public static DropPartitionParam Create(string collectionName,
                string partitionName)
        {
            var param = new DropPartitionParam()
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
            return $"{nameof(DropPartitionParam)}{{{nameof(CollectionName)}=/'{CollectionName}/', {nameof(PartitionName)}=/'{PartitionName}/'}}";

        }

    }
}
