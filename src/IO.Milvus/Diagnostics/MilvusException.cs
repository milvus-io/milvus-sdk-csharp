using IO.Milvus.ApiSchema;
using System.Runtime.Serialization;

namespace IO.Milvus.Diagnostics;

/// <summary>
///  Exception thrown for errors related to the Milvus connector.
/// </summary>
public class MilvusException : System.Exception
{
    ///<inheritdoc/>
    public MilvusException()
    {
    }

    ///<inheritdoc/>
    public MilvusException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MilvusException"/> class with a provided error code.
    /// </summary>
    /// <param name="errorCode"><see cref="ErrorCode"/></param>
    /// <param name="reason"><see cref="ResponseStatus.Reason"/></param>
    internal MilvusException(Grpc.ErrorCode errorCode,string reason = ""):base(GetErrorMsg(errorCode,reason))
    {
        ErrorCode = errorCode;
    }

    internal MilvusException(ResponseStatus status):base(GetErrorMsg(status.ErrorCode,status.Reason))
    {
        ErrorCode = status.ErrorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MilvusException"/> class with a status.
    /// </summary>
    /// <param name="status">Status.</param>
    public MilvusException(Grpc.Status status) : base(GetErrorMsg(status.ErrorCode, status.Reason))
    {
        ErrorCode = status.ErrorCode;
    }

    ///<inheritdoc/>
    public MilvusException(string message, System.Exception innerException) : base(message, innerException)
    {
    }

    ///<inheritdoc/>
    protected MilvusException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <summary>
    /// Error code.
    /// </summary>
    public Grpc.ErrorCode ErrorCode { get; }

    #region Private =====================================================================================
    private static string GetErrorMsg(Grpc.ErrorCode errorCode, string reason)
    {
        return $"ErrorCode: {errorCode} Reason: {reason}";
    }
    #endregion
}