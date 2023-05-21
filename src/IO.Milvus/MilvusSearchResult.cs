namespace IO.Milvus;

/// <summary>
/// Milvus Search Result.
/// </summary>
public class MilvusSearchResult
{
    internal static MilvusSearchResult From(Grpc.SearchResults searchResults)
    {
        return new MilvusSearchResult();
    }
}
