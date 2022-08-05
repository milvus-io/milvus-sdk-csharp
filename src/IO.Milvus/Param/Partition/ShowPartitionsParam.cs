using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System.Collections.Generic;

namespace IO.Milvus.Param.Partition
{
    public class ShowPartitionsParam
    {
        public string CollectionName { get; set; }

        public List<string> PartitionNames { get; } = new List<string>();

        public ShowType ShowType { get; set; } = ShowType.All;

        public static ShowPartitionsParam Create(string collectionName,List<string> partitionNames, ShowType showType = ShowType.All)
        {
            var param = new ShowPartitionsParam()
            {
                CollectionName = collectionName,
                ShowType = showType
            };

            if (partitionNames != null)
            {
                foreach (var partitionName in partitionNames)
                {
                    if (!param.PartitionNames.Contains(partitionName))
                    {
                        param.PartitionNames.Add(partitionName);
                    }
                }
            }
            param.Check();

            return param;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, nameof(CollectionName));

            if (!PartitionNames.IsEmpty())
            {
                foreach (var partitionName in PartitionNames)
                {
                    ParamUtils.CheckNullEmptyString(partitionName, $"{nameof(PartitionNames)}'s {nameof(partitionName)} in {nameof(partitionName)}");
                }
                ShowType = ShowType.InMemory;
            }
        }
    }
}
