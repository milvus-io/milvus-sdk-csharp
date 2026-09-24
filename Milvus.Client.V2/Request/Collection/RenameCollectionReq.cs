using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to rename a collection.
/// </summary>
public sealed class RenameCollectionReq
{
    /// <summary>
    /// The current name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional database name containing the collection to rename. When empty, the client's currently
    /// selected database is used, matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The new name of the collection.
    /// </summary>
    public string NewCollectionName { get; set; } = "";

    /// <summary>
    /// An optional target database to move the collection into. When empty, the collection is renamed in its
    /// current database.
    /// </summary>
    public string? TargetDatabaseName { get; set; }

    internal Grpc.RenameCollectionRequest ToGrpcRenameCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(NewCollectionName);
        return new Grpc.RenameCollectionRequest
        {
            DbName = DatabaseName ?? "",
            OldName = CollectionName,
            NewName = NewCollectionName,
            NewDBName = TargetDatabaseName ?? ""
        };
    }
}
