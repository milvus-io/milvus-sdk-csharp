using IO.Milvus.Param;

namespace IO.Milvus.Exception
{
    /// <summary>
    /// Exception for illegal parameters input.
    /// </summary>
    public class ParamException : MilvusException
    {
        public ParamException(string message) : base(message, Status.ParamError)
        {
        }
    }
}
