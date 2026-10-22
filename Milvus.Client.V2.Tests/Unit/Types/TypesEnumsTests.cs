using Xunit;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class TypesEnumsTests
{
    [Fact]
    public void CompactionState_values_match_source()
    {
        Assert.Equal(0, (int)CompactionState.UndefinedState);
        Assert.Equal(1, (int)CompactionState.Executing);
        Assert.Equal(2, (int)CompactionState.Completed);
    }

    [Fact]
    public void CompactionState_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.CompactionState.UndefiedState, (Grpc.CompactionState)(int)CompactionState.UndefinedState);
        Assert.Equal(Grpc.CompactionState.Executing, (Grpc.CompactionState)(int)CompactionState.Executing);
        Assert.Equal(Grpc.CompactionState.Completed, (Grpc.CompactionState)(int)CompactionState.Completed);
    }

    [Fact]
    public void CompactionTaskState_values_match_source()
    {
        Assert.Equal(0, (int)CompactionTaskState.Unknown);
        Assert.Equal(1, (int)CompactionTaskState.Executing);
        Assert.Equal(2, (int)CompactionTaskState.Pipelining);
        Assert.Equal(3, (int)CompactionTaskState.Completed);
        Assert.Equal(4, (int)CompactionTaskState.Failed);
        Assert.Equal(5, (int)CompactionTaskState.Timeout);
        Assert.Equal(6, (int)CompactionTaskState.Analyzing);
        Assert.Equal(7, (int)CompactionTaskState.Indexing);
        Assert.Equal(8, (int)CompactionTaskState.Cleaned);
        Assert.Equal(9, (int)CompactionTaskState.MetaSaved);
        Assert.Equal(10, (int)CompactionTaskState.Statistic);
    }

    [Fact]
    public void CompactionTaskState_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.CompactionTaskState.Unknown, (Grpc.CompactionTaskState)(int)CompactionTaskState.Unknown);
        Assert.Equal(Grpc.CompactionTaskState.Executing, (Grpc.CompactionTaskState)(int)CompactionTaskState.Executing);
        Assert.Equal(Grpc.CompactionTaskState.Pipelining, (Grpc.CompactionTaskState)(int)CompactionTaskState.Pipelining);
        Assert.Equal(Grpc.CompactionTaskState.Completed, (Grpc.CompactionTaskState)(int)CompactionTaskState.Completed);
        Assert.Equal(Grpc.CompactionTaskState.Failed, (Grpc.CompactionTaskState)(int)CompactionTaskState.Failed);
        Assert.Equal(Grpc.CompactionTaskState.Timeout, (Grpc.CompactionTaskState)(int)CompactionTaskState.Timeout);
        Assert.Equal(Grpc.CompactionTaskState.Analyzing, (Grpc.CompactionTaskState)(int)CompactionTaskState.Analyzing);
        Assert.Equal(Grpc.CompactionTaskState.Indexing, (Grpc.CompactionTaskState)(int)CompactionTaskState.Indexing);
        Assert.Equal(Grpc.CompactionTaskState.Cleaned, (Grpc.CompactionTaskState)(int)CompactionTaskState.Cleaned);
        Assert.Equal(Grpc.CompactionTaskState.MetaSaved, (Grpc.CompactionTaskState)(int)CompactionTaskState.MetaSaved);
        Assert.Equal(Grpc.CompactionTaskState.Statistic, (Grpc.CompactionTaskState)(int)CompactionTaskState.Statistic);
    }

    [Fact]
    public void CompactionType_values_match_source()
    {
        Assert.Equal(0, (int)CompactionType.Undefined);
        Assert.Equal(2, (int)CompactionType.Merge);
        Assert.Equal(3, (int)CompactionType.Mix);
        Assert.Equal(4, (int)CompactionType.Single);
        Assert.Equal(5, (int)CompactionType.Minor);
        Assert.Equal(6, (int)CompactionType.Major);
        Assert.Equal(7, (int)CompactionType.Level0Delete);
        Assert.Equal(8, (int)CompactionType.Clustering);
        Assert.Equal(9, (int)CompactionType.Sort);
        Assert.Equal(10, (int)CompactionType.PartitionKeySort);
        Assert.Equal(11, (int)CompactionType.ClusteringPartitionKeySort);
        Assert.Equal(12, (int)CompactionType.BumpSchemaVersion);
    }

    [Fact]
    public void CompactionType_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.CompactionType.Undefined, (Grpc.CompactionType)(int)CompactionType.Undefined);
        Assert.Equal(Grpc.CompactionType.Merge, (Grpc.CompactionType)(int)CompactionType.Merge);
        Assert.Equal(Grpc.CompactionType.Mix, (Grpc.CompactionType)(int)CompactionType.Mix);
        Assert.Equal(Grpc.CompactionType.Single, (Grpc.CompactionType)(int)CompactionType.Single);
        Assert.Equal(Grpc.CompactionType.Minor, (Grpc.CompactionType)(int)CompactionType.Minor);
        Assert.Equal(Grpc.CompactionType.Major, (Grpc.CompactionType)(int)CompactionType.Major);
        Assert.Equal(Grpc.CompactionType.Level0Delete, (Grpc.CompactionType)(int)CompactionType.Level0Delete);
        Assert.Equal(Grpc.CompactionType.Clustering, (Grpc.CompactionType)(int)CompactionType.Clustering);
        Assert.Equal(Grpc.CompactionType.Sort, (Grpc.CompactionType)(int)CompactionType.Sort);
        Assert.Equal(Grpc.CompactionType.PartitionKeySort, (Grpc.CompactionType)(int)CompactionType.PartitionKeySort);
        Assert.Equal(Grpc.CompactionType.ClusteringPartitionKeySort, (Grpc.CompactionType)(int)CompactionType.ClusteringPartitionKeySort);
        Assert.Equal(Grpc.CompactionType.BumpSchemaVersion, (Grpc.CompactionType)(int)CompactionType.BumpSchemaVersion);
    }

    [Fact]
    public void FunctionType_values_match_source()
    {
        Assert.Equal(0, (int)FunctionType.Unknown);
        Assert.Equal(1, (int)FunctionType.Bm25);
        Assert.Equal(2, (int)FunctionType.TextEmbedding);
        Assert.Equal(3, (int)FunctionType.Rerank);
    }

    [Fact]
    public void FunctionType_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.FunctionType.Unknown, (Grpc.FunctionType)(int)FunctionType.Unknown);
        Assert.Equal(Grpc.FunctionType.Bm25, (Grpc.FunctionType)(int)FunctionType.Bm25);
        Assert.Equal(Grpc.FunctionType.TextEmbedding, (Grpc.FunctionType)(int)FunctionType.TextEmbedding);
        Assert.Equal(Grpc.FunctionType.Rerank, (Grpc.FunctionType)(int)FunctionType.Rerank);
    }

    [Fact]
    public void ImportState_values_match_source()
    {
        Assert.Equal(0, (int)ImportState.Pending);
        Assert.Equal(1, (int)ImportState.Failed);
        Assert.Equal(2, (int)ImportState.Started);
        Assert.Equal(5, (int)ImportState.Persisted);
        Assert.Equal(6, (int)ImportState.Completed);
        Assert.Equal(7, (int)ImportState.FailedAndCleaned);
        Assert.Equal(8, (int)ImportState.Flushed);
    }

    [Fact]
    public void ImportState_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.ImportState.ImportPending, (Grpc.ImportState)(int)ImportState.Pending);
        Assert.Equal(Grpc.ImportState.ImportFailed, (Grpc.ImportState)(int)ImportState.Failed);
        Assert.Equal(Grpc.ImportState.ImportStarted, (Grpc.ImportState)(int)ImportState.Started);
        Assert.Equal(Grpc.ImportState.ImportPersisted, (Grpc.ImportState)(int)ImportState.Persisted);
        Assert.Equal(Grpc.ImportState.ImportCompleted, (Grpc.ImportState)(int)ImportState.Completed);
        Assert.Equal(Grpc.ImportState.ImportFailedAndCleaned, (Grpc.ImportState)(int)ImportState.FailedAndCleaned);
        Assert.Equal(Grpc.ImportState.ImportFlushed, (Grpc.ImportState)(int)ImportState.Flushed);
    }

    [Fact]
    public void RefreshExternalCollectionState_values_match_source()
    {
        Assert.Equal(0, (int)RefreshExternalCollectionState.Pending);
        Assert.Equal(1, (int)RefreshExternalCollectionState.InProgress);
        Assert.Equal(2, (int)RefreshExternalCollectionState.Completed);
        Assert.Equal(3, (int)RefreshExternalCollectionState.Failed);
    }

    [Fact]
    public void RefreshExternalCollectionState_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.RefreshExternalCollectionState.RefreshPending, (Grpc.RefreshExternalCollectionState)(int)RefreshExternalCollectionState.Pending);
        Assert.Equal(Grpc.RefreshExternalCollectionState.RefreshInProgress, (Grpc.RefreshExternalCollectionState)(int)RefreshExternalCollectionState.InProgress);
        Assert.Equal(Grpc.RefreshExternalCollectionState.RefreshCompleted, (Grpc.RefreshExternalCollectionState)(int)RefreshExternalCollectionState.Completed);
        Assert.Equal(Grpc.RefreshExternalCollectionState.RefreshFailed, (Grpc.RefreshExternalCollectionState)(int)RefreshExternalCollectionState.Failed);
    }

    [Fact]
    public void IndexState_values_match_source()
    {
        Assert.Equal(0, (int)IndexState.None);
        Assert.Equal(1, (int)IndexState.Unissued);
        Assert.Equal(2, (int)IndexState.InProgress);
        Assert.Equal(3, (int)IndexState.Finished);
        Assert.Equal(4, (int)IndexState.Failed);
        Assert.Equal(5, (int)IndexState.Retry);
    }

    [Fact]
    public void IndexState_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.IndexState.None, (Grpc.IndexState)(int)IndexState.None);
        Assert.Equal(Grpc.IndexState.Unissued, (Grpc.IndexState)(int)IndexState.Unissued);
        Assert.Equal(Grpc.IndexState.InProgress, (Grpc.IndexState)(int)IndexState.InProgress);
        Assert.Equal(Grpc.IndexState.Finished, (Grpc.IndexState)(int)IndexState.Finished);
        Assert.Equal(Grpc.IndexState.Failed, (Grpc.IndexState)(int)IndexState.Failed);
        Assert.Equal(Grpc.IndexState.Retry, (Grpc.IndexState)(int)IndexState.Retry);
    }

    [Fact]
    public void LoadState_values_match_source()
    {
        Assert.Equal(0, (int)LoadState.NotExist);
        Assert.Equal(1, (int)LoadState.NotLoad);
        Assert.Equal(2, (int)LoadState.Loading);
        Assert.Equal(3, (int)LoadState.Loaded);
    }

    [Fact]
    public void LoadState_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.LoadState.NotExist, (Grpc.LoadState)(int)LoadState.NotExist);
        Assert.Equal(Grpc.LoadState.NotLoad, (Grpc.LoadState)(int)LoadState.NotLoad);
        Assert.Equal(Grpc.LoadState.Loading, (Grpc.LoadState)(int)LoadState.Loading);
        Assert.Equal(Grpc.LoadState.Loaded, (Grpc.LoadState)(int)LoadState.Loaded);
    }

    [Theory]
    [InlineData(IndexType.Invalid, 0)]
    [InlineData(IndexType.Flat, 1)]
    [InlineData(IndexType.IvfFlat, 2)]
    [InlineData(IndexType.IvfSq8, 3)]
    [InlineData(IndexType.IvfPq, 4)]
    [InlineData(IndexType.Hnsw, 5)]
    [InlineData(IndexType.HnswSq, 6)]
    [InlineData(IndexType.HnswPq, 7)]
    [InlineData(IndexType.HnswPrq, 8)]
    [InlineData(IndexType.DiskAnn, 9)]
    [InlineData(IndexType.AutoIndex, 10)]
    [InlineData(IndexType.Scann, 11)]
    [InlineData(IndexType.GpuIvfFlat, 12)]
    [InlineData(IndexType.GpuIvfPq, 13)]
    [InlineData(IndexType.GpuBruteForce, 14)]
    [InlineData(IndexType.GpuCagra, 15)]
    [InlineData(IndexType.BinFlat, 16)]
    [InlineData(IndexType.BinIvfFlat, 17)]
    [InlineData(IndexType.Trie, 18)]
    [InlineData(IndexType.StlSort, 19)]
    [InlineData(IndexType.Inverted, 20)]
    [InlineData(IndexType.Bitmap, 21)]
    [InlineData(IndexType.SparseInvertedIndex, 22)]
    [InlineData(IndexType.SparseWand, 23)]
    [InlineData(IndexType.Ngram, 24)]
    [InlineData(IndexType.Rtree, 25)]
    [InlineData(IndexType.FmIndex, 26)]
    [InlineData(IndexType.Hybrid, 27)]
    [InlineData(IndexType.VecIndex, 28)]
    [InlineData(IndexType.IvfRabitq, 29)]
    [InlineData(IndexType.MinHashLsh, 30)]
    public void IndexType_values_match_source(IndexType indexType, int expected)
    {
        Assert.Equal(expected, (int)indexType);
    }

    [Theory]
    [InlineData(IndexType.Invalid, "INVALID")]
    [InlineData(IndexType.Flat, "FLAT")]
    [InlineData(IndexType.IvfFlat, "IVF_FLAT")]
    [InlineData(IndexType.IvfSq8, "IVF_SQ8")]
    [InlineData(IndexType.IvfPq, "IVF_PQ")]
    [InlineData(IndexType.Hnsw, "HNSW")]
    [InlineData(IndexType.HnswSq, "HNSW_SQ")]
    [InlineData(IndexType.HnswPq, "HNSW_PQ")]
    [InlineData(IndexType.HnswPrq, "HNSW_PRQ")]
    [InlineData(IndexType.DiskAnn, "DISKANN")]
    [InlineData(IndexType.AutoIndex, "AUTOINDEX")]
    [InlineData(IndexType.Scann, "SCANN")]
    [InlineData(IndexType.GpuIvfFlat, "GPU_IVF_FLAT")]
    [InlineData(IndexType.GpuIvfPq, "GPU_IVF_PQ")]
    [InlineData(IndexType.GpuBruteForce, "GPU_BRUTE_FORCE")]
    [InlineData(IndexType.GpuCagra, "GPU_CAGRA")]
    [InlineData(IndexType.BinFlat, "BIN_FLAT")]
    [InlineData(IndexType.BinIvfFlat, "BIN_IVF_FLAT")]
    [InlineData(IndexType.Trie, "TRIE")]
    [InlineData(IndexType.StlSort, "STL_SORT")]
    [InlineData(IndexType.Inverted, "INVERTED")]
    [InlineData(IndexType.Bitmap, "BITMAP")]
    [InlineData(IndexType.SparseInvertedIndex, "SPARSE_INVERTED_INDEX")]
    [InlineData(IndexType.SparseWand, "SPARSE_WAND")]
    [InlineData(IndexType.Ngram, "NGRAM")]
    [InlineData(IndexType.Rtree, "RTREE")]
    [InlineData(IndexType.FmIndex, "FMINDEX")]
    [InlineData(IndexType.Hybrid, "HYBRID")]
    [InlineData(IndexType.VecIndex, "VECINDEX")]
    [InlineData(IndexType.IvfRabitq, "IVF_RABITQ")]
    [InlineData(IndexType.MinHashLsh, "MINHASH_LSH")]
    public void IndexType_ToWireString_matches_type_mappings(IndexType indexType, string expected)
    {
        Assert.Equal(expected, indexType.ToWireString());
    }

    [Fact]
    public void SegmentState_and_level_expose_all_values()
    {
        Assert.Equal(8, Enum.GetValues(typeof(SegmentState)).Length);
        Assert.True(Enum.IsDefined(typeof(SegmentState), SegmentState.None));
        Assert.True(Enum.IsDefined(typeof(SegmentState), SegmentState.NotExist));
        Assert.True(Enum.IsDefined(typeof(SegmentState), SegmentState.Growing));
        Assert.True(Enum.IsDefined(typeof(SegmentState), SegmentState.Sealed));
        Assert.True(Enum.IsDefined(typeof(SegmentState), SegmentState.Flushed));
        Assert.True(Enum.IsDefined(typeof(SegmentState), SegmentState.Flushing));
        Assert.True(Enum.IsDefined(typeof(SegmentState), SegmentState.Dropped));
        Assert.True(Enum.IsDefined(typeof(SegmentState), SegmentState.Importing));

        Assert.Equal(4, Enum.GetValues(typeof(SegmentLevel)).Length);
        Assert.True(Enum.IsDefined(typeof(SegmentLevel), SegmentLevel.Legacy));
        Assert.True(Enum.IsDefined(typeof(SegmentLevel), SegmentLevel.L0));
        Assert.True(Enum.IsDefined(typeof(SegmentLevel), SegmentLevel.L1));
        Assert.True(Enum.IsDefined(typeof(SegmentLevel), SegmentLevel.L2));
    }

    [Theory]
    [InlineData(MetricType.Invalid, 0)]
    [InlineData(MetricType.L2, 1)]
    [InlineData(MetricType.Ip, 2)]
    [InlineData(MetricType.Cosine, 3)]
    [InlineData(MetricType.Hamming, 4)]
    [InlineData(MetricType.Jaccard, 5)]
    [InlineData(MetricType.MhJaccard, 6)]
    [InlineData(MetricType.Bm25, 7)]
    [InlineData(MetricType.MaxSim, 8)]
    [InlineData(MetricType.MaxSimCosine, 9)]
    [InlineData(MetricType.MaxSimIp, 10)]
    [InlineData(MetricType.MaxSimL2, 11)]
    [InlineData(MetricType.MaxSimJaccard, 12)]
    [InlineData(MetricType.MaxSimHamming, 13)]
    public void MetricType_values_match_source(MetricType metricType, int expected)
    {
        Assert.Equal(expected, (int)metricType);
    }

    [Theory]
    [InlineData(MetricType.Invalid, "INVALID")]
    [InlineData(MetricType.L2, "L2")]
    [InlineData(MetricType.Ip, "IP")]
    [InlineData(MetricType.Cosine, "COSINE")]
    [InlineData(MetricType.Hamming, "HAMMING")]
    [InlineData(MetricType.Jaccard, "JACCARD")]
    [InlineData(MetricType.MhJaccard, "MHJACCARD")]
    [InlineData(MetricType.Bm25, "BM25")]
    [InlineData(MetricType.MaxSim, "MAX_SIM")]
    [InlineData(MetricType.MaxSimCosine, "MAX_SIM_COSINE")]
    [InlineData(MetricType.MaxSimIp, "MAX_SIM_IP")]
    [InlineData(MetricType.MaxSimL2, "MAX_SIM_L2")]
    [InlineData(MetricType.MaxSimJaccard, "MAX_SIM_JACCARD")]
    [InlineData(MetricType.MaxSimHamming, "MAX_SIM_HAMMING")]
    public void MetricType_ToWireString_matches_type_mappings(MetricType metricType, string expected)
    {
        Assert.Equal(expected, metricType.ToWireString());
    }

    [Fact]
    public void RowPolicyAction_values_match_source()
    {
        Assert.Equal(-1, (int)RowPolicyAction.Unknown);
        Assert.Equal(0, (int)RowPolicyAction.Query);
        Assert.Equal(1, (int)RowPolicyAction.Search);
        Assert.Equal(2, (int)RowPolicyAction.Insert);
        Assert.Equal(3, (int)RowPolicyAction.Delete);
        Assert.Equal(4, (int)RowPolicyAction.Upsert);
        Assert.Equal(5, (int)RowPolicyAction.QueryIterator);
        Assert.Equal(6, (int)RowPolicyAction.SearchIterator);
        Assert.Equal(7, (int)RowPolicyAction.HybridSearch);
    }

    [Fact]
    public void RowPolicyAction_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.RowPolicyAction.Query, (Grpc.RowPolicyAction)(int)RowPolicyAction.Query);
        Assert.Equal(Grpc.RowPolicyAction.Search, (Grpc.RowPolicyAction)(int)RowPolicyAction.Search);
        Assert.Equal(Grpc.RowPolicyAction.Insert, (Grpc.RowPolicyAction)(int)RowPolicyAction.Insert);
        Assert.Equal(Grpc.RowPolicyAction.Delete, (Grpc.RowPolicyAction)(int)RowPolicyAction.Delete);
        Assert.Equal(Grpc.RowPolicyAction.Upsert, (Grpc.RowPolicyAction)(int)RowPolicyAction.Upsert);
        Assert.Equal(Grpc.RowPolicyAction.QueryIterator, (Grpc.RowPolicyAction)(int)RowPolicyAction.QueryIterator);
        Assert.Equal(Grpc.RowPolicyAction.SearchIterator, (Grpc.RowPolicyAction)(int)RowPolicyAction.SearchIterator);
        Assert.Equal(Grpc.RowPolicyAction.HybridSearch, (Grpc.RowPolicyAction)(int)RowPolicyAction.HybridSearch);
    }

    [Fact]
    public void RowPolicyType_values_match_source()
    {
        Assert.Equal(0, (int)RowPolicyType.Unknown);
        Assert.Equal(1, (int)RowPolicyType.Permissive);
        Assert.Equal(2, (int)RowPolicyType.Restrictive);
    }

    [Fact]
    public void RowPolicyType_maps_to_proto_one_to_one()
    {
        Assert.Equal(Grpc.RowPolicyType.Unknown, (Grpc.RowPolicyType)(int)RowPolicyType.Unknown);
        Assert.Equal(Grpc.RowPolicyType.Permissive, (Grpc.RowPolicyType)(int)RowPolicyType.Permissive);
        Assert.Equal(Grpc.RowPolicyType.Restrictive, (Grpc.RowPolicyType)(int)RowPolicyType.Restrictive);
    }
}
