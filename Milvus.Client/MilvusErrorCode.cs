namespace Milvus.Client;

#pragma warning disable CS1591 // Missing XML docs

/// <summary>
/// An error code returned in <see cref="MilvusException.ErrorCode" />.
/// </summary>
public enum MilvusErrorCode
{
    Success = Grpc.ErrorCode.Success,
    UnexpectedError = Grpc.ErrorCode.UnexpectedError,
    ConnectFailed = Grpc.ErrorCode.ConnectFailed,
    PermissionDenied = Grpc.ErrorCode.PermissionDenied,
    CollectionNotExists = Grpc.ErrorCode.CollectionNotExists,
    IllegalArgument = Grpc.ErrorCode.IllegalArgument,
    IllegalDimension = Grpc.ErrorCode.IllegalDimension,
    IllegalIndexType = Grpc.ErrorCode.IllegalIndexType,
    IllegalCollectionName = Grpc.ErrorCode.IllegalCollectionName,
    IllegalTopk = Grpc.ErrorCode.IllegalTopk,
    IllegalRowRecord = Grpc.ErrorCode.IllegalRowRecord,
    IllegalVectorId = Grpc.ErrorCode.IllegalVectorId,
    IllegalSearchResult = Grpc.ErrorCode.IllegalSearchResult,
    FileNotFound = Grpc.ErrorCode.FileNotFound,
    MetaFailed = Grpc.ErrorCode.MetaFailed,
    CacheFailed = Grpc.ErrorCode.CacheFailed,
    CannotCreateFolder = Grpc.ErrorCode.CannotCreateFolder,
    CannotCreateFile = Grpc.ErrorCode.CannotCreateFile,
    CannotDeleteFolder = Grpc.ErrorCode.CannotDeleteFolder,
    CannotDeleteFile = Grpc.ErrorCode.CannotDeleteFile,
    BuildIndexError = Grpc.ErrorCode.BuildIndexError,
    IllegalNlist = Grpc.ErrorCode.IllegalNlist,
    IllegalMetricType = Grpc.ErrorCode.IllegalMetricType,
    OutOfMemory = Grpc.ErrorCode.OutOfMemory,
    IndexNotExist = Grpc.ErrorCode.IndexNotExist,
    EmptyCollection = Grpc.ErrorCode.EmptyCollection,
    UpdateImportTaskFailure = Grpc.ErrorCode.UpdateImportTaskFailure,
    CollectionNameNotFound = Grpc.ErrorCode.CollectionNameNotFound,
    CreateCredentialFailure = Grpc.ErrorCode.CreateCredentialFailure,
    UpdateCredentialFailure = Grpc.ErrorCode.UpdateCredentialFailure,
    DeleteCredentialFailure = Grpc.ErrorCode.DeleteCredentialFailure,
    GetCredentialFailure = Grpc.ErrorCode.GetCredentialFailure,
    ListCredUsersFailure = Grpc.ErrorCode.ListCredUsersFailure,
    GetUserFailure = Grpc.ErrorCode.GetUserFailure,
    CreateRoleFailure = Grpc.ErrorCode.CreateRoleFailure,
    DropRoleFailure = Grpc.ErrorCode.DropRoleFailure,
    OperateUserRoleFailure = Grpc.ErrorCode.OperateUserRoleFailure,
    SelectRoleFailure = Grpc.ErrorCode.SelectRoleFailure,
    SelectUserFailure = Grpc.ErrorCode.SelectUserFailure,
    SelectResourceFailure = Grpc.ErrorCode.SelectResourceFailure,
    OperatePrivilegeFailure = Grpc.ErrorCode.OperatePrivilegeFailure,
    SelectGrantFailure = Grpc.ErrorCode.SelectGrantFailure,
    RefreshPolicyInfoCacheFailure = Grpc.ErrorCode.RefreshPolicyInfoCacheFailure,
    ListPolicyFailure = Grpc.ErrorCode.ListPolicyFailure,
    NotShardLeader = Grpc.ErrorCode.NotShardLeader,
    NoReplicaAvailable = Grpc.ErrorCode.NoReplicaAvailable,
    SegmentNotFound = Grpc.ErrorCode.SegmentNotFound,
    ForceDeny = Grpc.ErrorCode.ForceDeny,
    RateLimit = Grpc.ErrorCode.RateLimit,
    NodeIdnotMatch = Grpc.ErrorCode.NodeIdnotMatch,
    UpsertAutoIdtrue = Grpc.ErrorCode.UpsertAutoIdtrue,
    InsufficientMemoryToLoad = Grpc.ErrorCode.InsufficientMemoryToLoad,
    MemoryQuotaExhausted = Grpc.ErrorCode.MemoryQuotaExhausted,
    DiskQuotaExhausted = Grpc.ErrorCode.DiskQuotaExhausted,
    TimeTickLongDelay = Grpc.ErrorCode.TimeTickLongDelay,
    NotReadyServe = Grpc.ErrorCode.NotReadyServe,

    /// <summary>
    /// Coord is switching from standby mode to active mode
    /// </summary>
    NotReadyCoordActivating = Grpc.ErrorCode.NotReadyCoordActivating,

    /// <summary>
    /// Service availability.
    /// NA: Not Available.
    /// </summary>
    DataCoordNa = Grpc.ErrorCode.DataCoordNa,

    /// <summary>
    /// Internal error code.
    /// </summary>
    DdrequestRace = Grpc.ErrorCode.DdrequestRace
  }
