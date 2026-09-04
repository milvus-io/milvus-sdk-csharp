using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// Maps the public <see cref="IndexType" /> / <see cref="SimilarityMetricType" /> enums to the string values
/// sent over the wire (index create / search requests).
/// </summary>
internal static class TypeMappings
{
    internal static string ToWireString(this IndexType indexType) => indexType switch
    {
        IndexType.Invalid => "INVALID",
        IndexType.Flat => "FLAT",
        IndexType.IvfFlat => "IVF_FLAT",
        IndexType.IvfSq8 => "IVF_SQ8",
        IndexType.IvfPq => "IVF_PQ",
        IndexType.Hnsw => "HNSW",
        IndexType.HnswSq => "HNSW_SQ",
        IndexType.HnswPq => "HNSW_PQ",
        IndexType.HnswPrq => "HNSW_PRQ",
        IndexType.DiskAnn => "DISKANN",
        IndexType.AutoIndex => "AUTOINDEX",
        IndexType.Scann => "SCANN",
        IndexType.GpuIvfFlat => "GPU_IVF_FLAT",
        IndexType.GpuIvfPq => "GPU_IVF_PQ",
        IndexType.GpuBruteForce => "GPU_BRUTE_FORCE",
        IndexType.GpuCagra => "GPU_CAGRA",
        IndexType.BinFlat => "BIN_FLAT",
        IndexType.BinIvfFlat => "BIN_IVF_FLAT",
        IndexType.Trie => "TRIE",
        IndexType.StlSort => "STL_SORT",
        IndexType.Inverted => "INVERTED",
        IndexType.Bitmap => "BITMAP",
        IndexType.SparseInvertedIndex => "SPARSE_INVERTED_INDEX",
        IndexType.SparseWand => "SPARSE_WAND",
        _ => throw new ArgumentOutOfRangeException(nameof(indexType), indexType, null)
    };

    internal static string ToWireString(this SimilarityMetricType metricType) => metricType switch
    {
        SimilarityMetricType.Invalid => "INVALID",
        SimilarityMetricType.L2 => "L2",
        SimilarityMetricType.Ip => "IP",
        SimilarityMetricType.Cosine => "COSINE",
        SimilarityMetricType.Hamming => "HAMMING",
        SimilarityMetricType.Jaccard => "JACCARD",
        SimilarityMetricType.MhJaccard => "MHJACCARD",
        SimilarityMetricType.Bm25 => "BM25",
        SimilarityMetricType.MaxSim => "MAX_SIM",
        SimilarityMetricType.MaxSimCosine => "MAX_SIM_COSINE",
        SimilarityMetricType.MaxSimIp => "MAX_SIM_IP",
        SimilarityMetricType.MaxSimL2 => "MAX_SIM_L2",
        SimilarityMetricType.MaxSimJaccard => "MAX_SIM_JACCARD",
        SimilarityMetricType.MaxSimHamming => "MAX_SIM_HAMMING",
        _ => throw new ArgumentOutOfRangeException(nameof(metricType), metricType, null)
    };
}
