using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// Mutation result wrapper
/// </summary>
public sealed class MilvusMutationResult
{
    internal MilvusMutationResult(
        long insertCount,
        long deletedCount,
        long upsertCount,
        bool acknowledged,
        IList<uint> successIndex,
        IList<uint> errorIndex,
        DateTime dateTime,
        MilvusIds? ids,
        Grpc.MutationResult? mutationResult = null)
    {
        InsertCount = insertCount;
        DeleteCount = deletedCount;
        UpsertCount = upsertCount;
        Acknowledged = acknowledged;
        SuccessIndex = successIndex;
        ErrorIndex = errorIndex;
        Timestamp = dateTime;
        Ids = ids;
        MutationResult = mutationResult;
    }

    internal static MilvusMutationResult From(Grpc.MutationResult mutationResult)
        => new(
            mutationResult.InsertCnt,
            mutationResult.DeleteCnt,
            mutationResult.UpsertCnt,
            mutationResult.Acknowledged,
            mutationResult.SuccIndex.ToList(),
            mutationResult.ErrIndex.ToList(),
            TimestampUtils.GetTimeFromTimstamp((long)mutationResult.Timestamp),
            MilvusIds.From(mutationResult.IDs),
            mutationResult);

    /// <summary>
    /// Source mutation result from grpc response.
    /// </summary>
    public Grpc.MutationResult? MutationResult { get; }

    /// <summary>
    /// Acknowledged.
    /// </summary>

    public bool Acknowledged { get; }

    /// <summary>
    /// Timestamp.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Insert count.
    /// </summary>
    public long InsertCount { get; }

    /// <summary>
    /// Error count.
    /// </summary>
    public long DeleteCount { get; }

    /// <summary>
    /// Upsert count.
    /// </summary>
    public long UpsertCount { get; }

    /// <summary>
    /// Success index.
    /// </summary>
    public IList<uint> SuccessIndex { get; }

    /// <summary>
    /// Error index.
    /// </summary>
    public IList<uint> ErrorIndex { get; }

    /// <summary>
    /// Ids
    /// </summary>
    public MilvusIds? Ids { get; set; } // TODO NULLABILITY: Confirm nullability
}

/// <summary>
/// Ids
/// </summary>
public sealed class MilvusIds
{
    /// <summary>
    /// Construct a new instance of <see cref="MilvusIds"/>
    /// </summary>
    /// <param name="idField"></param>
    public MilvusIds(IdField idField)
        => IdField = idField;

    /// <summary>
    /// Id field
    /// </summary>
    public IdField IdField { get; set; }

    internal static MilvusIds From(Grpc.IDs ids)
        => ids switch
        {
            { IntId.Data.Count: > 0 } => new MilvusIds(new IdField(ids.IntId.Data.ToList())),
            { StrId.Data.Count: > 0 } => new MilvusIds(new IdField(ids.StrId.Data.ToList())),
            _ => new MilvusIds(new IdField())
        };
}

/// <summary>
/// Id field
/// </summary>
public sealed class IdField
{
    /// <summary>
    /// Construct a new instance of <see cref="IdField"/>
    /// </summary>
    public IdField()
    {
    }

    /// <summary>
    /// Construct a new instance of <see cref="IdField"/>
    /// </summary>
    /// <param name="stringIds"></param>
    public IdField(IList<string> stringIds)
        => StrId = new MilvusId<string>(stringIds);

    /// <summary>
    /// Construct a new instance of <see cref="IdField"/>
    /// </summary>
    /// <param name="longIds"></param>
    public IdField(IList<long> longIds)
        => IntId = new MilvusId<long>(longIds);

    /// <summary>
    /// Int id.
    /// </summary>
    public MilvusId<long>? IntId { get; set; }

    /// <summary>
    /// String id.
    /// </summary>
    public MilvusId<string>? StrId { get; set; }
}

/// <summary>
/// Milvus id
/// </summary>
/// <typeparam name="TId"><see cref="int"/> or <see cref="string"/></typeparam>
public sealed class MilvusId<TId>
{
    /// <summary>
    /// Create a int or string Milvus id.
    /// </summary>
    /// <param name="ids"></param>
    public MilvusId(IList<TId> ids)
        => Data = ids;

    /// <summary>
    /// Value
    /// </summary>
    public IList<TId> Data { get; }
}