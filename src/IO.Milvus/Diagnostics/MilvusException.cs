using IO.Milvus.ApiSchema;
using IO.Milvus.Grpc;
using System;
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
    internal MilvusException(ErrorCode errorCode,string reason = ""):base(GetErrorMsg(errorCode,reason))
    {

    }

    internal MilvusException(ResponseStatus responseStatus):base(GetErrorMsg(responseStatus.ErrorCode,responseStatus.Reason))
    {
            
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MilvusException"/> class with a status.
    /// </summary>
    /// <param name="status">Status.</param>
    public MilvusException(Grpc.Status status) : base(GetErrorMsg(status.ErrorCode, status.Reason))
    {

    }

    ///<inheritdoc/>
    public MilvusException(string message, System.Exception innerException) : base(message, innerException)
    {
    }

    ///<inheritdoc/>
    protected MilvusException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    private static string GetErrorMsg(ErrorCode errorCode, string reason)
    {
        return $"ErrorCode: {errorCode} Reason: {reason}";
    }
}
