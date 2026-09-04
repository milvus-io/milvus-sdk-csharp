namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to list all collections in the database.
/// </summary>
public sealed class ListCollectionsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// When true, only loaded collections are returned. When false (default), all collections are returned.
    /// </summary>
    public bool OnlyShowLoaded { get; set; }

#pragma warning disable CS0612 // The server marks ShowType as obsolete but still uses it.
    internal Grpc.ShowCollectionsRequest ToGrpcShowCollectionsRequest()
        => new()
        {
            DbName = DatabaseName ?? "",
            Type = OnlyShowLoaded ? Grpc.ShowType.InMemory : Grpc.ShowType.All
        };
#pragma warning restore CS0612
}
