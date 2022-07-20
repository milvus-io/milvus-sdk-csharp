using IO.Milvus.Exception;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;

namespace IO.Milvus.Param.Partition
{
    public class LoadPartitionsParam
    {
        public static LoadPartitionsParam Create(
            string collectionName,
            IEnumerable<string> partitionNames,
            int replicalNumber = 0)
        {
            var param = new LoadPartitionsParam()
            {
                CollectionName = collectionName,
                ReplicalNumber = replicalNumber,
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

        public static LoadPartitionsParam Create(
            string collectionName,
            string partitionName,
            int replicalNumber = 0)
        {
            var param = new LoadPartitionsParam()
            {
                CollectionName = collectionName,
                ReplicalNumber = replicalNumber,
            };
            if (!param.PartitionNames.Contains(partitionName))
            {
                param.PartitionNames.Add(partitionName);
            }
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        public int ReplicalNumber { get; set; } = 1;

        public List<string> PartitionNames { get; } = new List<string>();

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName,nameof(CollectionName));
            if (PartitionNames.IsEmpty())
            {
                throw new ParamException("Partition names cannot be empty");
            }

            PartitionNames.ForEach(p => ParamUtils.CheckNullEmptyString(p, nameof(PartitionNames)));            
        }
    }
}
