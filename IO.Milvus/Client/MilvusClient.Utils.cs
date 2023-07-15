namespace IO.Milvus.Client;

// ReSharper disable StringLiteralTypo

public partial class MilvusClient
{
    private static string GetGrpcIndexType(MilvusIndexType indexType)
        => indexType switch
        {
            MilvusIndexType.Invalid => "INVALID",
            MilvusIndexType.Flat => "FLAT",
            MilvusIndexType.IvfFlat => "IVF_FLAT",
            MilvusIndexType.IvfPq => "IVF_PQ",
            MilvusIndexType.IvfSq8 => "IVF_SQ8",
            MilvusIndexType.IvfHnsw => "IVF_HNSW",
            MilvusIndexType.Hnsw => "HNSW",
            MilvusIndexType.RhnswFlat => "RHNSW_FLAT",
            MilvusIndexType.RhnswPq => "RHNSW_PQ",
            MilvusIndexType.RhnswSq => "RHNSW_SQ",
            MilvusIndexType.Annoy => "ANNOY",
            MilvusIndexType.BinFlat => "BIN_FLAT",
            MilvusIndexType.BinIvfFlat => "BIN_IVF_FLAT",
            MilvusIndexType.Trie => "TRIE",
            MilvusIndexType.AutoIndex => "AUTOINDEX",

            _ => throw new ArgumentOutOfRangeException(nameof(indexType), indexType, null)
        };

    private static string GetGrpcMetricType(MilvusMetricType metricType)
        => metricType.ToString();

}
