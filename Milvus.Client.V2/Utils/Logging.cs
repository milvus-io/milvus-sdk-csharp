using Microsoft.Extensions.Logging;

using Milvus.Client.V2;

namespace Milvus.Client.V2.Utils;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Unhealthy: {Reasons}")]
    public static partial void HealthCheckFailed(this ILogger logger, IEnumerable<string> reasons);

    [LoggerMessage(2, LogLevel.Debug, "{OperationName} invoked: {Argument}")]
    public static partial void OperationInvoked(this ILogger logger, string operationName, object argument);

    [LoggerMessage(3, LogLevel.Error, "{OperationName} failed: {ErrorCode}, {Reason}")]
    public static partial void OperationFailed(this ILogger logger, string operationName, MilvusErrorCode errorCode, string reason);

    [LoggerMessage(4, LogLevel.Information, "Global cluster primary changed from '{Current}' to '{New}'; reconnecting.")]
    public static partial void GlobalPrimaryChanged(this ILogger logger, string current, string @new);

    [LoggerMessage(5, LogLevel.Warning, "Failed to parse global topology response: {Message}")]
    public static partial void GlobalTopologyParseFailed(this ILogger logger, string message);

    [LoggerMessage(6, LogLevel.Warning, "Failed to apply global topology change: {Message}")]
    public static partial void GlobalTopologyApplyFailed(this ILogger logger, string message);

    [LoggerMessage(7, LogLevel.Information, "Ignoring stale global topology version {Version} (applied {Applied}).")]
    public static partial void GlobalTopologyStale(this ILogger logger, long version, long applied);

    [LoggerMessage(8, LogLevel.Warning, "Ignoring global topology with no primary (writable) cluster (version {Version}).")]
    public static partial void GlobalTopologyNoPrimary(this ILogger logger, long version);

    [LoggerMessage(9, LogLevel.Warning, "SRV resolution failed for '{Hostname}' (attempt {Attempt}/{Max}): {Message}")]
    public static partial void GlobalSrvResolutionFailed(this ILogger logger, string hostname, int attempt, int max, string message);

    [LoggerMessage(10, LogLevel.Warning, "No usable topology response from {Count} seed(s) for '{Hostname}' (attempt {Attempt}/{Max}): {Message}")]
    public static partial void GlobalSeedsFailed(this ILogger logger, int count, string hostname, int attempt, int max, string message);

    [LoggerMessage(11, LogLevel.Warning, "Topology probe failed for {Target}: {Message}")]
    public static partial void GlobalProbeFailed(this ILogger logger, string target, string message);

    [LoggerMessage(12, LogLevel.Warning, "Topology replacement callback failed: {Message}")]
    public static partial void GlobalReplacementFailed(this ILogger logger, string message);
}
