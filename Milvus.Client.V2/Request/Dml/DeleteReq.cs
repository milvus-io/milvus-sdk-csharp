using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dml;

/// <summary>
/// Represents a request to delete rows from a collection by expression.
/// </summary>
public sealed class DeleteReq
{
    /// <summary>
    /// The name of the collection to delete from.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The boolean expression identifying the rows to delete (e.g. <c>"id in [1, 2, 3]"</c>).
    /// </summary>
    public string Expression { get; set; } = "";

    /// <summary>
    /// An optional partition to delete from.
    /// </summary>
    public string? PartitionName { get; set; }

    internal Grpc.DeleteRequest ToGrpcDeleteRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(Expression);

        return new Grpc.DeleteRequest
        {
            CollectionName = CollectionName,
            PartitionName = PartitionName ?? "",
            Expr = Expression
        };
    }
}
