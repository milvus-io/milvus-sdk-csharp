namespace Milvus.Client;

/// <summary>
/// Exception thrown for errors related to the Milvus client.
/// </summary>
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
}
