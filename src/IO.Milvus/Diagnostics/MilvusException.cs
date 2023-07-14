using System;
using IO.Milvus.Grpc;

namespace IO.Milvus.Diagnostics;

/// <summary>
///  Exception thrown for errors related to the Milvus client.
/// </summary>
public sealed class MilvusException : Exception
{
    // TODO: Decide how to expose the error code (string or enum?)

    /// <summary>
    /// The error code.
    /// </summary>
    public string? ErrorCode { get; }

    // TODO: Make sure whether we want this to be publicly-constructed

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

    internal MilvusException(ErrorCode errorCode, string reason)
        : base($"ErrorCode: {errorCode} Reason: {reason}")
    {
        ErrorCode = errorCode.ToString();
    }
}
