using IO.Milvus.Param;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace IO.Milvus.Connection
{
    public class ServerMonitor<TVector> : Runnable
    {
        //TODO add logger

        private static long heartbeatInterval = 10 * 1000;
        private long? lastHeartbeat;
        private List<IListener> listeners;
        private ClusterFactory<TVector> clusterFactory;
        private Thread monitorThread;
        private volatile bool isRunning;

        public ServerMonitor(ClusterFactory<TVector> clusterFactory, QueryNodeSingleSearch<TVector> queryNodeSingleSearch)
        {
            if (queryNodeSingleSearch == null)
            {
                this.listeners = new List<IListener>() { new ClusterListener(), new QueryNodeListener<TVector>(queryNodeSingleSearch) };
            }
            else
            {
                this.listeners = new List<IListener>() { new ClusterListener() };
            }
            this.clusterFactory = clusterFactory;

            monitorThread = new Thread(new ThreadStart(Run))
            {
                Name = "Milvus-server-monitor",
                IsBackground = true,
            };
            isRunning = true;
        }

        public void Start()
        {
            //logger.info("Milvus Server Monitor start.");
            monitorThread.Start();
        }

        public void Close()
        {
            isRunning = false;
            //logger.info("Milvus Server Monitor close.");
            monitorThread.Interrupt();
        }

        #region ServerMonitorRunnable
        public override void Run()
        {
            while (isRunning)
            {
                long startTime = DateTime.Now.Millisecond;

                if (null == lastHeartbeat || startTime - lastHeartbeat > heartbeatInterval)
                {

                    lastHeartbeat = startTime;

                    try
                    {
                        List<ServerSetting> availableServer = GetAvailableServer();
                        clusterFactory.AvailableServerChange(availableServer);
                    }
                    catch (System.Exception)
                    {
                        //logger.error("Milvus Server Heartbeat error, monitor will stop.", e);
                    }

                    if (!clusterFactory.MasterIsRunning())
                    {
                        ServerSetting master = clusterFactory.electMaster();

                        //logger.warn("Milvus Server Heartbeat. Master is Not Running, Re-Elect [{}] to master.",master.ServerAddress.Host;

                        clusterFactory.MasterChange(master);
                    }
                    else
                    {
                        //TODO Logger
                        //logger.debug("Milvus Server Heartbeat. Master is Running.");
                    }
                }
            }
        }

        private List<ServerSetting> GetAvailableServer()
        {
            return clusterFactory.ServerSettings
                .Where(p => CheckServerState(p))
                .ToList();
        }

        private bool CheckServerState(ServerSetting serverSetting)
        {
            foreach (var listener in listeners)
            {
                bool isRunning = listener.HeartBeat(serverSetting);
                if (!isRunning)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

    }
}
