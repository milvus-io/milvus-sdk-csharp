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

#if NET462
#pragma warning disable SYSLIB0051 // Formatter-based serialization is obsolete on .NET 8+, but required for the net462 target.
    private MilvusException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        ErrorCode = (MilvusErrorCode)info.GetValue(nameof(ErrorCode), typeof(MilvusErrorCode))!;
    }

    /// <inheritdoc />
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
    }
#pragma warning restore SYSLIB0051
#endif
}
