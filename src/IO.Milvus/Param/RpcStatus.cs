using System;

namespace IO.Milvus.Param
{
    /// <summary>
    /// Utility class to wrap a message.
    /// </summary>
    public class RpcStatus
    {
        public static string SUCCESS_MSG = "Success";

        public string Msg { get; }

        public String getMsg()
        {
            return Msg;
        }

        public RpcStatus(String msg)
        {
            this.Msg = msg;
        }

        /// <summary>
        /// Constructs a <code>String</code> by <see cref="RpcStatus"/> instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            $"RpcStatus{{msg=\'{Msg}\'}}";
    }
}
