using IO.Milvus.Param;
using System;
using System.Collections.Generic;
using IO.Milvus.Param.Dml;
using IO.Milvus.Grpc;
using IO.Milvus.Response;
using IO.Milvus.Utils;

namespace IO.Milvus.Connection
{
    public class QueryNodeListener<TVector> : IListener
    {
        private const int HEARTBEAT_TIMEOUT_MILLS = 4000;
        private SearchParam<TVector> searchParam;

        public QueryNodeListener(QueryNodeSingleSearch<TVector> singleSearch)
        {
            searchParam = SearchParam<TVector>.Create(
                collectionName: singleSearch.CollectionName,
                vectorFieldName: singleSearch.VectorFieldName,
                metricType: singleSearch.MetricType,
                vectors: singleSearch.Vectors,
                topk: 5,
                roundDecimal: -1,
                gracefulTime: 1L
                );

            //searchParam = SearchParam<TVector>.NewBuilder()
            //    .WithCollectionName(singleSearch.CollectionName)
            //    .WithVectors(singleSearch.Vectors)
            //    .WithVectorFieldName(singleSearch.VectorFieldName)
            //    .WithParams(singleSearch.Params)
            //    .WithMetricType(singleSearch.MetricType)
            //    .WithTopK(5)
            //    .WithRoundDecimal(-1)
            //    .WithGuaranteeTimestamp(1L)
            //    .Build();
        }

        public bool HeartBeat(ServerSetting serverSetting)
        {
            bool isRunning = false;

            try
            {
                R<SearchResults> response = serverSetting.Client
                    .WithTimeout(TimeSpan.FromMilliseconds(HEARTBEAT_TIMEOUT_MILLS))
                    .Search(searchParam);

                if (response.Status == Param.Status.Success)
                {
                    var wrapperSearch = new SearchResultsWrapper<TVector>(response.Data.Results);
                    List<SearchResultsWrapper<TVector>.IDScore> idScores = wrapperSearch.getIDScore(0);
                    if (idScores.IsNotEmpty())
                    {
                        //logger.debug("Host [{}] heartbeat Success of Milvus QueryNode Listener.",serverSetting.getServerAddress().getHost());
                        isRunning = true;
                    }
                }
            }
            catch (System.Exception)
            {
                //logger.error("Host [{}] heartbeat Error of Milvus QueryNode Listener.",serverSetting.getServerAddress().getHost(), e);
            }
            return isRunning;
        }
    }
}
