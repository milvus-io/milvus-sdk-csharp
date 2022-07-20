using IO.Milvus.Common.ClientEnum;
using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using System.Collections.Generic;

namespace IO.Milvus.Param.Dml
{
    public class QueryParam
    {
        public static QueryParam Create(
            string collectionName,
            List<string> partitionNames,
            List<string> outFields,
            ConsistencyLevelEnum consistencyLevel = ConsistencyLevelEnum.STRONG,
            string expr = "",
            ulong travelTimestamp = 0L,
            ulong gracefulTime = 5000L,
            ulong guaranteeTimestamp = Constant.GUARANTEE_EVENTUALLY_TS
            )
        {
            var param = new QueryParam()
            {
                CollectionName = collectionName,
                PartitionNames = partitionNames,
                OutFields = outFields,
                ConsistencyLevel = consistencyLevel,
                Expr = expr,
                TravelTimestamp = travelTimestamp,
                GracefulTime = gracefulTime,
                GuaranteeTimestamp = guaranteeTimestamp
            };
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        public string Expr { get; set; }

        public List<string> PartitionNames { get; set; } = new List<string>();

        public List<string> OutFields { get; set; } = new List<string>();

        public ConsistencyLevelEnum ConsistencyLevel { get; set; }

        public ulong TravelTimestamp { get; set; }

        public ulong GuaranteeTimestamp { get; set; }

        public ulong GracefulTime { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, "Collection name");
            ParamUtils.CheckNullEmptyString(Expr, "Expression");

            if (TravelTimestamp < 0)
            {
                throw new ParamException("The travel timestamp must be greater than 0");
            }

            if (GuaranteeTimestamp < 0)
            {
                throw new ParamException("The guarantee timestamp must be greater than 0");
            }
        }
    }
}
