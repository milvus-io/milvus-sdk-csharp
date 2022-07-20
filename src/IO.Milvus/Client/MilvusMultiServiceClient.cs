using IO.Milvus.Grpc;
using IO.Milvus.Param;
using IO.Milvus.Param.Alias;
using IO.Milvus.Param.Collection;
using IO.Milvus.Param.Control;
using IO.Milvus.Param.Credential;
using IO.Milvus.Param.Dml;
using IO.Milvus.Param.Index;
using IO.Milvus.Param.Partition;
using System;
using System.Threading.Tasks;

namespace IO.Milvus.Client
{
    public class MilvusMultiServiceClient 
    {
        public R<RpcStatus> AlterAlias(AlterAliasParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<CalcDistanceResults> CalcDistance(CalcDistanceParam requestParam)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> CreateAlias(CreateAliasParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> CreateCollection(CreateCollectionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> CreateCredential(CreateCredentialParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> CreateIndex(CreateIndexParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> CreatePartition(CreatePartitionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<MutationResult> Delete(DeleteParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> DeleteCredential(DeleteCredentialParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<DescribeCollectionResponse> DescribeCollection(DescribeCollectionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<DescribeIndexResponse> DescribeIndex(DescribeIndexParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> DropAlias(DropAliasParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> DropCollection(DropCollectionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> DropCollection(string collectionName)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> DropIndex(DropIndexParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> DropPartition(DropPartitionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<FlushResponse> Flush(FlushParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetCollectionStatisticsResponse> GetCollectionStatistics(GetCollectionStatisticsParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetCompactionStateResponse> GetCompactionState(GetCompactionStateParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetCompactionPlansResponse> GetCompactionStateWithPlans(GetCompactionPlansParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetFlushStateResponse> GetFlushState(GetFlushStateParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetIndexBuildProgressResponse> GetIndexBuildProgress(GetIndexBuildProgressParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetMetricsResponse> GetMetrics(GetMetricsParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetPartitionStatisticsResponse> GetPartitionStatistics(GetPartitionStatisticsParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetPersistentSegmentInfoResponse> GetPersistentSegmentInfo(GetPersistentSegmentInfoParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<GetQuerySegmentInfoResponse> GetQuerySegmentInfo(GetQuerySegmentInfoParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<bool> HasCollection(HasCollectionParam hasCollectionParam)
        {
            throw new NotImplementedException();
        }

        public R<bool> HasCollection(string collectionName)
        {
            throw new NotImplementedException();
        }

        public R<bool> HasPartition(HasPartitionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<MutationResult> Insert(InsertParam requestParam)
        {
            throw new NotImplementedException();
        }

        public Task<R<MutationResult>> InsertAsync(InsertParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> LoadBalance(LoadBalanceParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> LoadCollection(LoadCollectionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> LoadPartitions(LoadPartitionsParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<ManualCompactionResponse> ManualCompaction(ManualCompactionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<QueryResults> Query(QueryParam requestParam)
        {
            throw new NotImplementedException();
        }

        public Task<R<QueryResults>> QueryAsync(QueryParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> ReleaseCollection(ReleaseCollectionParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> ReleasePartitions(ReleasePartitionsParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<SearchResults> Search<TVector>(SearchParam<TVector> requestParam)
        {
            throw new NotImplementedException();
        }

        public Task<R<SearchResults>> SearchAsync<TVector>(SearchParam<TVector> requestParam)
        {
            throw new NotImplementedException();
        }

        public R<ShowCollectionsResponse> ShowCollections(ShowCollectionsParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<ShowPartitionsResponse> ShowPartitions(ShowPartitionsParam requestParam)
        {
            throw new NotImplementedException();
        }

        public R<RpcStatus> UpdateCredential(UpdateCredentialParam requestParam)
        {
            throw new NotImplementedException();
        }

        public IMilvusClient WithTimeout(TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }
    }
}
