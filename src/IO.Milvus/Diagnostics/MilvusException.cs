using System;

namespace IO.Milvus.Diagnostics;

/// <summary>
///  Exception thrown for errors related to the Milvus client.
/// </summary>
public sealed class MilvusException : Exception
{
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

    internal static string GetErrorMessage<TErrorCode>(TErrorCode errorCode, string reason) => $"ErrorCode: {errorCode} Reason: {reason}";
}