using IO.Milvus.Exception;
using IO.Milvus.Utils;
using System.Collections.Generic;

namespace IO.Milvus.Param.Control
{
    public class GetFlushStateParam
    {
        public static GetFlushStateParam Create(
            IEnumerable<long> ids
            )
        {
            var param = new GetFlushStateParam();
            param.SegmentIDs.AddRange(ids);
            return param;
        }

        public List<long> SegmentIDs { get; } = new List<long>();

        internal void Check()
        {
            if (SegmentIDs.IsEmpty())
            {
                throw new ParamException("Segment id array cannot be empty");
            }
        }
    }
}
