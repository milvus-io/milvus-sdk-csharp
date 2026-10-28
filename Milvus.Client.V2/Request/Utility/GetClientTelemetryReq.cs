namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to retrieve the server-side client-telemetry report, via the proto
/// <c>ClientTelemetryService.GetClientTelemetry</c> RPC.
/// </summary>
/// <remarks>
/// When authorization is enabled, the server gates this RPC behind <c>checkTelemetryAdmin</c> and requires the
/// calling user to be the <c>root</c> user; a non-root caller receives a permission <see cref="MilvusException" />.
/// </remarks>
public sealed class GetClientTelemetryReq
{
    /// <summary>
    /// An optional database name to filter the reported clients by. When empty, all databases are reported.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// An optional client id to limit the report to a single connected client.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Whether to include per-operation metrics in the report.
    /// </summary>
    public bool IncludeMetrics { get; set; }

    /// <summary>
    /// When set, only the reply to this command is returned, instead of every reply the matching clients have
    /// accumulated.
    /// </summary>
    public string? CommandId { get; set; }

    internal Grpc.GetClientTelemetryRequest ToGrpcGetClientTelemetryRequest()
    {
        var request = new Grpc.GetClientTelemetryRequest { IncludeMetrics = IncludeMetrics };

        if (Database is not null)
        {
            request.Database = Database;
        }

        if (ClientId is not null)
        {
            request.ClientId = ClientId;
        }

        if (CommandId is not null)
        {
            request.CommandId = CommandId;
        }

        return request;
    }
}
