namespace Milvus.Client;

#pragma warning disable CS1591 // Missing XML docs

/// <summary>
/// An error code returned in <see cref="MilvusException.ErrorCode" />.
/// </summary>
public enum MilvusErrorCode
{
    Success = 0,
    UnexpectedError = 1,
    RateLimit = 8,
    ForceDeny = 9,
    CollectionNotFound = 100,
    IndexNotFound = 700
  }
