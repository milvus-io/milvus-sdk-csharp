namespace IO.Milvus.Param.Control
{
    public class GetCompactionStateParam
    {
        public static GetCompactionStateParam Create(long compactionID)
        {
            return new GetCompactionStateParam()
            {
                CompactionID = compactionID
            };
        }

        public long CompactionID { get; set; }
    }
}
