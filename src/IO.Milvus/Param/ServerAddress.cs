using IO.Milvus.Exception;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace IO.Milvus.Param
{
    /// <summary>
    /// ServerAddress for Milvus
    /// </summary>
    /// <remarks>
    /// Default Host = localhost
    /// Default Port = 19530
    /// </remarks>
    public class ServerAddress:IEqualityComparer<ServerAddress>,IEquatable<ServerAddress>
    {
        #region Properties
        public string Host { get; }

        public int Port { get; }

        public int HealthPort { get; }
        #endregion

        #region Ctor
        private ServerAddress(Builder builder)
        {
            Host = builder.host;
            Port = builder.port;
            HealthPort = builder.healthPort;
        }
        #endregion

        public static Builder NewBuilder()
        {
            return new Builder();
        }

        public class Builder
        {
            internal string host = "localhost";
            internal int port = 19530;
            internal int healthPort = 9091;

            internal Builder() { }

            public Builder WithHost(string host)
            {
                this.host = host;
                return this;
            }

            public Builder WithPort(int port)
            {
                this.port = port;
                return this;
            }

            public Builder WithHealthPort(int port)
            {
                this.healthPort = port;
                return this;
            }

            public ServerAddress Build()
            {
                ParamUtils.CheckNullEmptyString(host, "Host name");

                if (port < 0 || port > 0xFFFF)
                {
                    throw new ParamException("Port is out of range!");
                }

                if (healthPort < 0 || healthPort > 0xFFFF)
                {
                    throw new ParamException("Health Port is out of range!");
                }

                return new ServerAddress(this);
            }
        }

        public override string ToString()
        {
            return $"{nameof(ServerAddress)}{{host=\'{Host}\', port=\'{Port}\'}}";
        }

        public bool Equals(ServerAddress x, ServerAddress y)
        {
            return x.Port == y.Port && x.Host == y.Host;
        }

        public int GetHashCode(ServerAddress obj)
        {
            return new HashCodeBuilder()
                .Add(Host)
                .Add(Port)
                .GetHashCode();
        }

        public bool Equals(ServerAddress other)
        {
            return Equals(this, other); 
        }
    }
}
