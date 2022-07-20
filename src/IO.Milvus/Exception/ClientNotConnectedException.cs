using IO.Milvus.Param;

namespace IO.Milvus.Exception
{
    /// <summary>
    ///  Milvus client API throws this exception when not connected to the Milvus server.
    /// </summary>
    public class ClientNotConnectedException : MilvusException
    {
        public ClientNotConnectedException(string message) : base(message,Status.ClientNotConnected)
        {
        }
    }
}
