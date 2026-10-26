namespace Milvus.Client.V2.Types;

/// <summary>
/// Information about a connected SDK client, as reported by <c>GetClientTelemetryAsync</c>.
/// </summary>
public sealed class ClientInfo
{
    internal ClientInfo(
        string sdkType, string sdkVersion, string localTime, string user, string host,
        IReadOnlyDictionary<string, string> reserved)
    {
        SdkType = sdkType;
        SdkVersion = sdkVersion;
        LocalTime = localTime;
        User = user;
        Host = host;
        Reserved = reserved;
    }

    /// <summary>
    /// The SDK type, e.g. <c>"CSharp"</c>.
    /// </summary>
    public string SdkType { get; }

    /// <summary>
    /// The SDK version.
    /// </summary>
    public string SdkVersion { get; }

    /// <summary>
    /// The client's local time.
    /// </summary>
    public string LocalTime { get; }

    /// <summary>
    /// The authenticated user.
    /// </summary>
    public string User { get; }

    /// <summary>
    /// The client host.
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Additional reserved client fields.
    /// </summary>
    public IReadOnlyDictionary<string, string> Reserved { get; }

    internal static ClientInfo FromGrpc(Grpc.ClientInfo? info)
    {
        if (info is null)
        {
            return new ClientInfo("", "", "", "", "", new Dictionary<string, string>());
        }

        return new ClientInfo(
            info.SdkType,
            info.SdkVersion,
            info.LocalTime,
            info.User,
            info.Host,
            info.Reserved.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }
}
