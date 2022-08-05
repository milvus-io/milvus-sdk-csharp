using IO.Milvus.Client;
using IO.Milvus.Exception;
using IO.Milvus.Param;
using System;
using System.Diagnostics.CodeAnalysis;

namespace IO.Milvus.Connection
{
    /// <summary>
    /// Defined address and Milvus clients for each server.
    /// </summary>
    public class ServerSetting
    {
        public ServerAddress ServerAddress { get; }

        public IMilvusClient Client { get; }

        public ServerSetting(Builder builder)
        {
            ServerAddress = builder.serverAddress;
            Client = builder.milvusClient;
        }

        public class Builder
        {
            internal ServerAddress serverAddress;
            internal IMilvusClient milvusClient;

            private Builder() { }

            /// <summary>
            /// Sets the server address
            /// </summary>
            /// <param name="serverAddress">ServerAddress host,port/server</param>
            /// <returns><code>Builder</code></returns>
            /// <exception cref="ArgumentNullException"></exception>
            public Builder WithHost(ServerAddress serverAddress)
            {
                this.serverAddress = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
                return this;
            }

            /// <summary>
            /// Sets the server client for a cluster
            /// </summary>
            /// <param name="milvusClient">MilvusClient</param>
            /// <returns><code>Builder</code></returns>
            public Builder WithMilvusClient(IMilvusClient milvusClient)
            {
                this.milvusClient = milvusClient;
                return this;
            }

            /// <summary>
            /// Verifies parameters and creates a new {@link ConnectParam} instance.
            /// </summary>
            /// <returns><see cref="ServerSetting"/></returns>
            /// <exception cref="ParamException"></exception>
            public ServerSetting Build()
            {
                ParamUtils.CheckNullEmptyString(serverAddress.Host,"Host Name");

                if (serverAddress.Port < 0 || serverAddress.Port > 0xFFFF)
                {
                    throw new ParamException("Port is out of range!");
                }

                if (milvusClient == null)
                {
                    throw new ParamException("Milvus client can not be empty");
                }

                return new ServerSetting(this);
            }
        }
    }
}
