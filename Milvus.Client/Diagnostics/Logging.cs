using Microsoft.Extensions.Logging;

namespace Milvus.Client.Diagnostics;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Unhealthy: {Reasons}")]
    public static partial void HealthCheckFailed(this ILogger logger, IEnumerable<string> reasons);

    [LoggerMessage(2, LogLevel.Debug, "{OperationName} invoked: {Argument}")]
    public static partial void OperationInvoked(this ILogger logger, string operationName, object argument);

    [LoggerMessage(3, LogLevel.Error, "{OperationName} failed: {ErrorCode}, {Reason}")]
    public static partial void OperationFailed(this ILogger logger, string operationName, ErrorCode errorCode, string reason);
}
