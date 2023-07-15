using IO.Milvus.Grpc;

namespace IO.Milvus;

/// <summary>
/// Check if Milvus is healthy.
/// </summary>
public sealed class MilvusHealthState
{
    internal MilvusHealthState(
        bool isHealthy,
        string errorMsg,
        ErrorCode errorCode)
    {
        IsHealthy = isHealthy;
        ErrorMsg = errorMsg;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Health flag.
    /// </summary>
    public bool IsHealthy { get; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string ErrorMsg { get; }

    /// <summary>
    /// Error code.
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// Get string data.
    /// </summary>
    public override string ToString()
    {
        string state = $"{{{nameof(IsHealthy)}:{IsHealthy}}}";
        if (!IsHealthy)
        {
            state = $"{state}, {{{nameof(ErrorCode)}:{ErrorCode}}}, {{{nameof(ErrorMsg)}:{ErrorMsg}}}";
        }

        return state;
    }
}
