using IO.Milvus.Param;

namespace IO.Milvus.Exception
{
    /// <summary>
    /// Interfaces including <code>search</code>/<code>search</code>/<code>loadCollection</code> might 
    /// throw this exception when server return illegal response. It may indicate a bug in server.
    /// </summary>
    public class IllegalResponseException : MilvusException
    {
        public IllegalResponseException(string message) : base(message, Status.IllegalResponse)
        {
        }
    }
}
