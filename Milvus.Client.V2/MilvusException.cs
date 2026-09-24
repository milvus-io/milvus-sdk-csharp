#if NET462
using System.Runtime.Serialization;
#endif

namespace Milvus.Client.V2;

/// <summary>
/// Exception thrown for errors related to the Milvus client.
/// </summary>
#if NET462
[Serializable]
#endif
public sealed class MilvusException : Exception
{
    /// <summary>
    /// The error code.
    /// </summary>
    public MilvusErrorCode ErrorCode { get; }

    /// <summary>
    /// The underlying gRPC status code, when this exception wraps a transport-level <c>RpcException</c>.
    /// <c>null</c> for server-returned errors.
    /// </summary>
    public global::Grpc.Core.StatusCode? GrpcStatusCode { get; }

    /// <inheritdoc />
    public MilvusException()
    {
    }

    /// <inheritdoc />
    public MilvusException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public MilvusException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Instantiates a new <see cref="MilvusException" />.
    /// </summary>
    public MilvusException(MilvusErrorCode errorCode, string reason)
        : base($"ErrorCode: {errorCode} Reason: {reason}")
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Instantiates a new <see cref="MilvusException" /> with an inner exception.
    /// </summary>
    public MilvusException(MilvusErrorCode errorCode, string reason, Exception innerException)
        : base($"ErrorCode: {errorCode} Reason: {reason}", innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Instantiates a new <see cref="MilvusException" /> carrying the underlying gRPC status code.
    /// </summary>
    public MilvusException(MilvusErrorCode errorCode, string reason, global::Grpc.Core.StatusCode grpcStatusCode, Exception innerException)
        : base($"ErrorCode: {errorCode} Reason: {reason} gRPC StatusCode: {grpcStatusCode}", innerException)
    {
        ErrorCode = errorCode;
        GrpcStatusCode = grpcStatusCode;
    }

#if NET462
#pragma warning disable SYSLIB0051 // Formatter-based serialization is obsolete on .NET 8+, but required for the net462 target.
    private MilvusException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        ErrorCode = (MilvusErrorCode)info.GetValue(nameof(ErrorCode), typeof(MilvusErrorCode))!;
        // GetObjectData stores the boxed StatusCode enum (never a boxed Nullable<StatusCode>); asking for the
        // nullable type here would route through Convert.ChangeType, which net462 cannot convert to a
        // Nullable<T>, so read with the plain enum type instead.
        if (info.GetValue(nameof(GrpcStatusCode), typeof(global::Grpc.Core.StatusCode)) is global::Grpc.Core.StatusCode code)
        {
            GrpcStatusCode = code;
        }
    }

    /// <inheritdoc />
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
        info.AddValue(nameof(GrpcStatusCode), GrpcStatusCode);
    }
#pragma warning restore SYSLIB0051
#endif
}
