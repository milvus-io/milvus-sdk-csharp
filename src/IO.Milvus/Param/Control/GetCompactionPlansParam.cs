namespace IO.Milvus.Param.Control
{
    public class GetCompactionPlansParam
    {
        public static GetCompactionPlansParam Create(long compactionID)
        {
            return new GetCompactionPlansParam()
            {
                CompactionID = compactionID
            };
        }

        public long CompactionID { get; set; }
    }
}
