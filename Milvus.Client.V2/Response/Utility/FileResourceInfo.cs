namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// A registered file resource, as returned by <c>ListFileResources</c>.
/// </summary>
public sealed class FileResourceInfo
{
    private FileResourceInfo(long id, string name, string path)
    {
        Id = id;
        Name = name;
        Path = path;
    }

    internal static FileResourceInfo FromGrpc(Grpc.FileResourceInfo info)
        => new(info.Id, info.Name, info.Path);

    /// <summary>
    /// The ID of the file resource.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// The name of the file resource.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The path of the file resource.
    /// </summary>
    public string Path { get; }
}
