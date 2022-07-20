using IO.Milvus.Exception;
using IO.Milvus.Utils;
using System.Collections.Generic;

namespace IO.Milvus.Param.Partition
{
    public class ReleasePartitionsParam
    {
        public static ReleasePartitionsParam Create(
         string collectionName,
         IEnumerable<string> partitionNames)
        {
            var param = new ReleasePartitionsParam()
            {
                CollectionName = collectionName,
            };

            foreach (var partitionName in partitionNames)
            {
                if (!param.PartitionNames.Contains(partitionName))
                {
                    param.PartitionNames.Add(partitionName);
                }
            }
            param.Check();

            return param;
        }

        public static ReleasePartitionsParam Create(
                string collectionName,
                string partitionName)
        {
            var param = new ReleasePartitionsParam()
            {
                CollectionName = collectionName,
            };
            if (!param.PartitionNames.Contains(partitionName))
            {
                param.PartitionNames.Add(partitionName);
            }
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        public List<string> PartitionNames { get;  } = new List<string>();

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, nameof(CollectionName));
            if (PartitionNames.IsEmpty())
            {
                throw new ParamException("Partition names cannot be empty");
            }

            PartitionNames.ForEach(p => ParamUtils.CheckNullEmptyString(p, nameof(PartitionNames)));
        }
    }
}
