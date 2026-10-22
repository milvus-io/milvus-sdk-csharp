using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to add a struct field to an existing collection.
/// </summary>
public sealed class AddCollectionStructFieldReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to add the struct field to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The schema of the struct field to add. The struct field must be nullable: Milvus only accepts a nullable
    /// struct field when adding it to an existing collection, mirroring the Java/C++/Rust SDKs.
    /// </summary>
    public StructFieldSchema StructField { get; set; } = null!;
    internal Grpc.AddCollectionStructFieldRequest ToGrpcAddCollectionStructFieldRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(StructField);

        // Milvus only accepts a nullable struct field when adding it to an existing collection; reject a
        // non-nullable schema up front (matching Java's toStructFieldSchema / Rust's AddCollectionStructField).
        if (!StructField.Nullable)
        {
            throw new ArgumentException(
                $"Struct field '{StructField.Name}' must be nullable to be added to an existing collection.",
                nameof(StructField));
        }

        var request = new Grpc.AddCollectionStructFieldRequest
        {
            CollectionName = CollectionName,
            StructArrayFieldSchema = CreateCollectionReq.ConvertToGrpcStructFieldSchema(StructField, StructField.Nullable)
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
