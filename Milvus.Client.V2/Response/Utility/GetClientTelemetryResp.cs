using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The client telemetry reported by <c>GetClientTelemetryAsync</c>: one entry per connected client, plus the
/// aggregated metrics. Mirrors the proto <c>ClientTelemetryService.GetClientTelemetry</c> response.
/// </summary>
public sealed class GetClientTelemetryResp
{
    internal GetClientTelemetryResp(IReadOnlyList<ClientTelemetryInfo> clients, MetricsInfo aggregated)
    {
        Clients = clients;
        Aggregated = aggregated;
    }

    /// <summary>
    /// The telemetry of each connected client.
    /// </summary>
    public IReadOnlyList<ClientTelemetryInfo> Clients { get; }

    /// <summary>
    /// The metrics aggregated across all reported clients.
    /// </summary>
    public MetricsInfo Aggregated { get; }

    internal static GetClientTelemetryResp FromGrpc(Grpc.GetClientTelemetryResponse response)
        => new(
            response.Clients.Select(ClientTelemetryInfo.FromGrpc).ToArray(),
            MetricsInfo.FromGrpc(response.Aggregated));
}

/// <summary>
/// The telemetry of a single connected client.
/// </summary>
public sealed class ClientTelemetryInfo
{
    internal ClientTelemetryInfo(
        ClientInfo clientInfo, long lastHeartbeatTime, string status,
        IReadOnlyList<string> databases, IReadOnlyList<OperationMetricsInfo> metrics)
    {
        ClientInfo = clientInfo;
        LastHeartbeatTime = lastHeartbeatTime;
        Status = status;
        Databases = databases;
        Metrics = metrics;
    }

    /// <summary>
    /// The identifying information of the client (SDK type/version, user, host, ...).
    /// </summary>
    public ClientInfo ClientInfo { get; }

    /// <summary>
    /// The time of the client's last heartbeat.
    /// </summary>
    public long LastHeartbeatTime { get; }

    /// <summary>
    /// The client's status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// The databases the client is connected to.
    /// </summary>
    public IReadOnlyList<string> Databases { get; }

    /// <summary>
    /// The per-operation metrics reported by the client, when <c>IncludeMetrics</c> was set on the request.
    /// </summary>
    public IReadOnlyList<OperationMetricsInfo> Metrics { get; }

    internal static ClientTelemetryInfo FromGrpc(Grpc.ClientTelemetry telemetry)
        => new(
            ClientInfo.FromGrpc(telemetry.ClientInfo),
            telemetry.LastHeartbeatTime,
            telemetry.Status,
            telemetry.Databases.ToArray(),
            telemetry.Metrics.Select(OperationMetricsInfo.FromGrpc).ToArray());
}
