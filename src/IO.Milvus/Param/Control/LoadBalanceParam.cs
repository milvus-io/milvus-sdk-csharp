using IO.Milvus.Exception;
using IO.Milvus.Utils;
using System.Collections.Generic;

namespace IO.Milvus.Param.Control
{
    public class LoadBalanceParam
    {
        public static LoadBalanceParam Create(
            long srcNodeID, 
            List<long> destNodeIDs, 
            List<long> segmentIDs)
        {
            var param = new LoadBalanceParam()
            {
                SrcNodeID = srcNodeID,
                DestNodeIDs = destNodeIDs,
                SegmentIDs = segmentIDs,
            };
            param.Check();

            return param;
        }

        public long SrcNodeID { get; set; }

        public List<long> DestNodeIDs { get; set; } = new List<long>();

        public List<long> SegmentIDs { get; set; } = new List<long>();

        internal void Check()
        {
            if (SegmentIDs.IsEmpty())
            {
                throw new ParamException("Sealed segment id array cannot be empty");
            }

            if (DestNodeIDs.IsEmpty())
            {
                throw new ParamException("Destination query node id array cannot be empty");
            }
        }
    }
}
