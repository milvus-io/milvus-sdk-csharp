using Milvus.Client.V2.Requests.Utility;
namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// Represents the result of a <c>GetServerVersion</c> operation.
/// </summary>
public sealed class GetServerVersionResp
{
    private GetServerVersionResp(
        string version, string? buildTime, string? gitCommit, string? goVersion, string? deployMode)
    {
        Version = version;
        BuildTime = buildTime;
        GitCommit = gitCommit;
        GoVersion = goVersion;
        DeployMode = deployMode;
    }

    internal static GetServerVersionResp FromGrpc(Grpc.GetVersionResponse response)
        => new(response.Version, buildTime: null, gitCommit: null, goVersion: null, deployMode: null);

    internal static GetServerVersionResp FromGrpc(Grpc.ConnectResponse response)
    {
        // ServerInfo is a singular proto message; it may be unset (null) on servers that don't populate it.
        if (response.ServerInfo is null)
        {
            return new("", buildTime: null, gitCommit: null, goVersion: null, deployMode: null);
        }

        Grpc.ServerInfo info = response.ServerInfo;
        return new(info.BuildTags, info.BuildTime, info.GitCommit, info.GoVersion, info.DeployMode);
    }

    /// <summary>
    /// The Milvus server version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// The build time of the server. Only populated when <see cref="GetServerVersionReq.Detail" /> is <c>true</c>.
    /// </summary>
    public string? BuildTime { get; }

    /// <summary>
    /// The git commit of the server build. Only populated when <see cref="GetServerVersionReq.Detail" /> is
    /// <c>true</c>.
    /// </summary>
    public string? GitCommit { get; }

    /// <summary>
    /// The Go version used to build the server. Only populated when <see cref="GetServerVersionReq.Detail" /> is
    /// <c>true</c>.
    /// </summary>
    public string? GoVersion { get; }

    /// <summary>
    /// The deploy mode of the server. Only populated when <see cref="GetServerVersionReq.Detail" /> is <c>true</c>.
    /// </summary>
    public string? DeployMode { get; }
}
