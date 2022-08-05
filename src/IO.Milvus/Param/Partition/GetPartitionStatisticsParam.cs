using System;

namespace IO.Milvus.Param.Partition
{
    public class GetPartitionStatisticsParam
    {
        public static GetPartitionStatisticsParam Create(string collectionName,string partitionName)
        {
            var param = new GetPartitionStatisticsParam()
            {
                CollectionName = collectionName,
                PartitionName = partitionName
            };

            param.Check();
            return param;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, nameof(CollectionName));
            ParamUtils.CheckNullEmptyString(PartitionName, nameof(PartitionName));
        }

        public string PartitionName { get; set; }

        public string CollectionName { get; set; }

        public bool IsFulshCollection { get; set; } = true;

        public override string ToString()
        {
            return $"{nameof(GetPartitionStatisticsParam)}{{{nameof(CollectionName)}=/'{CollectionName}/', {nameof(PartitionName)}=/'{PartitionName}/', {nameof(IsFulshCollection)}=/'{IsFulshCollection}/'}}";

        }
    }
}
