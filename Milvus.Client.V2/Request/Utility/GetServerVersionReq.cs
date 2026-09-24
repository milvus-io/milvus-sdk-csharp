namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get the Milvus server version.
/// </summary>
public sealed class GetServerVersionReq
{
    /// <summary>
    /// Whether to return detailed version information (build time, git commit, Go version, deploy mode)
    /// in addition to the version string. Defaults to <c>false</c>.
    /// </summary>
    public bool Detail { get; set; }
}
