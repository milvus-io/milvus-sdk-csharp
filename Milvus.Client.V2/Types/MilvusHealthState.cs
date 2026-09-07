using Milvus.Client.V2;
namespace Milvus.Client.V2.Types;

/// <summary>
/// The health state of the Milvus server, as reported by <see cref="MilvusClientV2.HealthAsync" />.
/// </summary>
/// <param name="IsHealthy">Whether the server reported itself as healthy.</param>
/// <param name="Reason">The reason reported by the server when unhealthy.</param>
/// <param name="ErrorCode">The error code reported by the server.</param>
public sealed record MilvusHealthState(bool IsHealthy, string Reason, MilvusErrorCode ErrorCode);
