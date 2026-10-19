using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to register a named file resource for external-table workflows.
/// </summary>
public sealed class AddFileResourceReq
{
    /// <summary>
    /// The name of the file resource to register.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The path of the file resource.
    /// </summary>
    public string Path { get; set; } = "";

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(Name);
        Verify.NotNullOrWhiteSpace(Path);
    }

    internal Grpc.AddFileResourceRequest ToGrpcAddFileResourceRequest()
        => new() { Name = Name, Path = Path };
}
