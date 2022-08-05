using IO.Milvus.Exception;
using IO.Milvus.Param;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace IO.Milvus.Connection
{
    /// <summary>
    /// Factory with managing multi cluster.
    /// </summary>
    public class ClusterFactory<TVector>
    {
        private ServerMonitor<TVector> monitor;

        private ClusterFactory(Builder builder)
        {
            this.ServerSettings = builder.serverSettings;
            this.Master = this.DefaultServer;
            this.AvailableServerSettings = builder.serverSettings;
            if (builder.keepMonitor)
            {
                monitor = new ServerMonitor<TVector>(this, builder.queryNodeSingleSearch);
                monitor.Start();
            }
        }

        #region Properties
        public ServerSetting DefaultServer => ServerSettings[0];

        public List<ServerSetting> AvailableServerSettings { get; private set; }

        public ServerSetting Master { get; private set; }

        public List<ServerSetting> ServerSettings { get; }

        public bool MasterIsRunning()
        {
            List<ServerAddress> serverAddresses = AvailableServerSettings
                    .Select(setiings => setiings.ServerAddress)
                    .ToList();

            return serverAddresses.Contains(Master.ServerAddress);
        }

        public void MasterChange(ServerSetting serverSetting)
        {
            this.Master = serverSetting;
        }

        public void AvailableServerChange(List<ServerSetting> serverSettings)
        {
            this.AvailableServerSettings = serverSettings;
        }

        public ServerSetting electMaster()
        {
            return (AvailableServerSettings != null && AvailableServerSettings.Count > 0) ? AvailableServerSettings.First() : DefaultServer;
        }
        #endregion

        #region Methods
        public void Close()
        {
            if (monitor != null)
            {
                monitor.Close();
            }
        }

        public static Builder NewBuilder()
        {
            return new Builder();
        }
        #endregion

        #region Builder
        public class Builder
        {
            internal List<ServerSetting> serverSettings;
            internal bool keepMonitor = false;
            internal QueryNodeSingleSearch<TVector> queryNodeSingleSearch;

            internal Builder()
            {
            }

            public Builder WithServerSetting(List<ServerSetting> serverSettings)
            {
                this.serverSettings = serverSettings;
                return this;
            }

            public Builder KeepMonitor(bool enable)
            {
                this.keepMonitor = enable;
                return this;
            }

            public Builder WithQueryNodeSingleSearch(QueryNodeSingleSearch<TVector> queryNodeSingleSearch)
            {
                this.queryNodeSingleSearch = queryNodeSingleSearch;
                return this;
            }

            public ClusterFactory<TVector> Build()
            {
                if (serverSettings == null || serverSettings.Count == 0)
                {
                    throw new ParamException("Server settings is empty!");
                }

                return new ClusterFactory<TVector>(this);
            }
        }
        #endregion
    }
}
