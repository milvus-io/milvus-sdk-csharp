using IO.Milvus.Exception;
using IO.Milvus.Utils;
using System;
using System.Diagnostics.CodeAnalysis;

namespace IO.Milvus.Param
{
    public class ConnectParam
    {
        private int _port = 19530;
        #region Ctor
        public ConnectParam()
        {
        }

        public static ConnectParam Create(
            string host,
            int port = 19530,
            string name = "root",
            string password = "milvus")
        {
            var param = new ConnectParam()
            {
                Host = host,
                Port = port,
                Authorization = $"{name}:{password}",
            };

            param.Check();
            return param;
        }
        #endregion

        #region Properties
        public string Host { get; set; } = "localhost";

        public int Port
        {
            get => _port; set
            {
                _port = value;
                CheckPort();
            }
        }

        [Obsolete("Useless")]
        public TimeSpan ConnectTimeout { get; } = TimeSpan.FromMilliseconds(10000);

        [Obsolete("Useless")]
        public TimeSpan KeepAliveTime { get; } = TimeSpan.MaxValue;

        [Obsolete("Useless")]
        public TimeSpan KeepAliveTimeout { get; } = TimeSpan.FromMilliseconds(20000);

        [Obsolete("Useless")]
        public bool IsKeepAliveWithoutCalls { get; } = false;

        [Obsolete("Useless")]
        public bool IsSecure { get; } = false;

        [Obsolete("Useless")]
        public TimeSpan IdleTimeout { get; } = TimeSpan.FromHours(24);

        public string Authorization { get; set; } = "root:milvus";

        public bool UseHttps { get; set; } = false;
        #endregion

        #region Methods
        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(Host, $"{nameof(ConnectParam)}.{nameof(ConnectParam.Host)}");

            CheckPort();
        }

        public string GetAddress()
        {
            string httpType = UseHttps ? "https" : "http";
            return $"{httpType}://{Host}:{Port}";
        }

        public override string ToString()
        {
            return $"ConnectParam{{host=/'{Host}/', port=/'{Port}/'}}";
        }

        private void CheckPort()
        {
            if (Port < 0 || Port > 0xFFFF)
            {
                throw new ParamException("Port is out of range!");
            }
        }
        #endregion
    }
}
