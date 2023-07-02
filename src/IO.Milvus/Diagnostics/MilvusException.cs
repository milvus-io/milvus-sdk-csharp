using IO.Milvus.ApiSchema;
using System;

namespace IO.Milvus.Diagnostics;

/// <summary>
///  Exception thrown for errors related to the Milvus connector.
/// </summary>
public sealed class MilvusException : Exception
{
    ///<inheritdoc/>
    public MilvusException()
    {
    }

    ///<inheritdoc/>
    public MilvusException(string message) : base(message)
    {
    }

    ///<inheritdoc/>
    public MilvusException(string message, Exception innerException) : base(message, innerException)
    {
    }

    internal MilvusException(ResponseStatus status) : base(GetErrorMsg(status.ErrorCode, status.Reason))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MilvusException"/> class with a status.
    /// </summary>
    /// <param name="status">Status.</param>
    internal MilvusException(Grpc.Status status) : base(GetErrorMsg(status.ErrorCode, status.Reason))
    {
    }

    private static string GetErrorMsg(Grpc.ErrorCode errorCode, string reason) => $"ErrorCode: {errorCode} Reason: {reason}";
}