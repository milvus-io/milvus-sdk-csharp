#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Types;
public enum ImportState
{
    Pending = 0,
    Failed = 1,
    Started = 2,
    Persisted = 5,
    Completed = 6,
    FailedAndCleaned = 7,
    Flushed = 8
}
