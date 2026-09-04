using Microsoft.Extensions.Logging;

namespace Milvus.Client.V2.Types;

/// <summary>
/// The connection parameters for <see cref="MilvusClientV2" />, mirroring the <c>ConnectParam</c> of the C++ SDK
/// and the <c>ConnectConfig</c> of the Java SDK.
/// </summary>
public sealed class ConnectConfig
{
    /// <summary>
    /// The URI to connect to. This can be a cloud instance endpoint or an address such as
    /// <c>"http://localhost:19530"</c>. If the scheme is omitted, <c>http://</c> is assumed.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Deliberately a string to mirror ConnectParam.uri (C++) / ConnectConfig.uri (Java) and allow scheme inference.")]
    public string Uri { get; set; } = "";

    /// <summary>
    /// The username to authenticate with.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// The password to authenticate with.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// An API key to authenticate with, instead of a username and password.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The database to connect to. Defaults to the default Milvus database.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// An optional timeout applied to all gRPC calls made by the client. When set, it is used as the gRPC call
    /// deadline. Mirrors the Java SDK's <c>rpcDeadlineMs</c>.
    /// </summary>
    public TimeSpan? ConnectTimeout { get; set; }

    /// <summary>
    /// The keepalive ping delay applied to the gRPC channel (Java's <c>keepAliveTimeMs</c>, default 10 seconds).
    /// When <see cref="ChannelOptions" /> is set, this is ignored. When null, the default of 10 seconds is used.
    /// Keepalive pings are applied on .NET 5 and later only; on the netstandard2.0 / net462 targets this value
    /// has no effect.
    /// </summary>
    public TimeSpan? KeepAliveTime { get; set; }

    /// <summary>
    /// The keepalive ping timeout (Java's <c>keepAliveTimeoutMs</c>, default 5 seconds). When
    /// <see cref="ChannelOptions" /> is set, this is ignored. When null, the default of 5 seconds is used.
    /// Keepalive pings are applied on .NET 5 and later only; on the netstandard2.0 / net462 targets this value
    /// has no effect.
    /// </summary>
    public TimeSpan? KeepAliveTimeout { get; set; }

    /// <summary>
    /// Whether keepalive pings are sent even when there are no active calls (Java's
    /// <c>keepAliveWithoutCalls</c>, default true). When <see cref="ChannelOptions" /> is set, this is ignored.
    /// When null, the default of true is used. Keepalive pings are applied on .NET 5 and later only; on the
    /// netstandard2.0 / net462 targets this value has no effect.
    /// </summary>
    public bool? KeepAliveWithoutCalls { get; set; }

    /// <summary>
    /// An optional logger factory through which the Milvus client will log.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Optional gRPC channel options used when creating the internal channel. When set, these are used as-is
    /// (for example a custom <c>HttpHandler</c> for testing); when unset, a default channel with keepalive pings is
    /// created.
    /// </summary>
    public GrpcChannelOptions? ChannelOptions { get; set; }

    /// <summary>
    /// The retry policy applied to RPC calls. When <c>null</c>, a default <see cref="RetryConfig" /> is used.
    /// </summary>
    public RetryConfig? Retry { get; set; }
}
