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
    public MilvusException(ErrorCode errorCode):base(GetErrorMsg(errorCode))
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

    private static string GetErrorMsg(ErrorCode errorCode)
    {
        return $"ErrorCode:{errorCode}";
    }
}
