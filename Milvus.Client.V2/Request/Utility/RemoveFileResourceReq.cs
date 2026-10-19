using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to remove a registered file resource by name.
/// </summary>
public sealed class RemoveFileResourceReq
{
    /// <summary>
    /// The name of the file resource to remove.
    /// </summary>
    public string Name { get; set; } = "";

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(Name);
    }

    internal Grpc.RemoveFileResourceRequest ToGrpcRemoveFileResourceRequest()
        => new() { Name = Name };
}
