using System;
using System.Net;
using System.Net.Http;

namespace IO.Milvus.Connection
{
    public class ClusterListener : IListener
    {
        #region Private Static Fields
        //TODO Logger
        private static string HEALTH_PATH = "http://{0}:{1}/healthz";
        private static string RESPONSE_OK = "OK";
        private static HttpClient OK_HTTP_CLIENT = new HttpClient()
        {
            Timeout = System.TimeSpan.FromSeconds(5),
        };
        #endregion

        #region Public Method
        public bool HeartBeat(ServerSetting serverSetting)
        {
            string url = string.Format(HEALTH_PATH, serverSetting.ServerAddress.Host,
                serverSetting.ServerAddress.Port);

            bool isRunning = false;
            try
            {
                var response = Get(url);
                isRunning = CheckResponse(response);
                if (isRunning)
                {
                    //logger.debug("Host [{}] heartbeat Success of Milvus Cluster Listener.", serverSetting.ServerAddress.Host);
                }
            }
            catch 
            {
                //logger.error("Host [{}] heartbeat Error of Milvus Cluster Listener.",serverSetting.getServerAddress().getHost());
            }
            return isRunning;
        }
        #endregion

        #region Private Methods
        private bool CheckResponse(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return string.Equals(responseString, RESPONSE_OK, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private HttpResponseMessage Get(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("OkHttp GET error: url cannot be null.");
            }

            var httpMsg = new HttpRequestMessage(HttpMethod.Get,url);

            return OK_HTTP_CLIENT.SendAsync(httpMsg).GetAwaiter().GetResult();
        }
        #endregion
    }
}
