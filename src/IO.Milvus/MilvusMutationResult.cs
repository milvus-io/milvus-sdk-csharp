using IO.Milvus.ApiSchema;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace IO.Milvus;
/// <summary>
/// Mutation result wrapper
/// </summary>
public class MilvusMutationResult
{
    internal MilvusMutationResult() { }

    internal MilvusMutationResult(
        long insertCount,
        long deletedCount,
        long upsertCount,
        bool acknowledged,
        IList<uint> successIndex,
        IList<uint> errorIndex,
        DateTime dateTime,
        MilvusIds ids,
        Grpc.MutationResult mutationResult = null)
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
    {
        return new MilvusMutationResult(
            mutationResult.InsertCnt,
            mutationResult.DeleteCnt,
            mutationResult.UpsertCnt,
            mutationResult.Acknowledged,
            mutationResult.SuccIndex.ToList(),
            mutationResult.ErrIndex.ToList(),
            TimestampUtils.GetTimeFromTimstamp((long)mutationResult.Timestamp),
            MilvusIds.From(mutationResult.IDs),
            mutationResult
            );
    }

    internal static MilvusMutationResult From(MilvusMutationResponse insertResponse)
    {
        return new MilvusMutationResult(
            insertResponse.InsertCount,
            insertResponse.DeletedCount,
            insertResponse.UpsertCount,
            insertResponse.Acknowledged,
            insertResponse.SuccessIndex,
            insertResponse.ErrorIndex,
            TimestampUtils.GetTimeFromTimstamp(insertResponse.Timestamp),
            insertResponse.Ids
            );
    }

    /// <summary>
    /// Source mutation result from grpc response.
    /// </summary>
    public Grpc.MutationResult MutationResult { get; }

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
    public MilvusIds Ids { get; set; }
}

/// <summary>
/// Ids
/// </summary>
public class MilvusIds
{
    /// <summary>
    /// Construct a new instance of <see cref="MilvusIds"/>
    /// </summary>
    public MilvusIds() { }

    /// <summary>
    /// Construct a new instance of <see cref="MilvusIds"/>
    /// </summary>
    /// <param name="idField"></param>
    public MilvusIds(IdField idField)
    {
        this.IdField = idField;
    }

    /// <summary>
    /// Id field
    /// </summary>
    public IdField IdField { get; set; }

    /// <summary>
    /// Create a new instance of <see cref="MilvusIds"/> from string ids.
    /// </summary>
    /// <param name="stringIds"></param>
    /// <returns></returns>
    public static MilvusIds CreateStrIds(IList<string> stringIds)
    {
        return new MilvusIds(new IdField(stringIds));
    }

    /// <summary>
    /// Create a new instance of <see cref="MilvusIds"/> from int ids.
    /// </summary>
    /// <param name="intIds"></param>
    /// <returns></returns>
    public static MilvusIds CreateIntIds(IList<long> intIds)
    {
        return new MilvusIds(new IdField(intIds));
    }

    #region Private =========================================================
    internal static MilvusIds From(IDs ids)
    {
        if (ids == null) return null;

        var idField = new IdField()
        {
            IdFieldCase = (MilvusIdFieldOneofCase)ids.IdFieldCase,
        };

        if (ids.IntId?.Data?.Count > 0)
        {
            idField.IntId = new MilvusId<long>
            {
                Data = ids.IntId.Data.ToList(),
            };
        }

        if (ids.StrId?.Data?.Count > 0)
        {
            idField.StrId = new MilvusId<String>
            {
                Data = ids.StrId.Data.ToList(),
            };
        }

        return new MilvusIds(idField);
    }
    #endregion
}

/// <summary>
/// Id field
/// </summary>
public class IdField
{
    /// <summary>
    /// Construct a new instance of <see cref="IdField"/>
    /// </summary>
    public IdField() { }

    /// <summary>
    /// Construct a new instance of <see cref="IdField"/>
    /// </summary>
    /// <param name="stringIds"></param>
    public IdField(IList<string> stringIds)
    {
        IdFieldCase = MilvusIdFieldOneofCase.StrId;
        StrId = new MilvusId<string>(stringIds);
    }

    /// <summary>
    /// Construct a new instance of <see cref="IdField"/>
    /// </summary>
    /// <param name="longIds"></param>
    public IdField(IList<long> longIds)
    {
        IdFieldCase = MilvusIdFieldOneofCase.IntId;
        IntId = new MilvusId<long>(longIds);
    }

    /// <summary>
    /// Id field case.
    /// </summary>
    public MilvusIdFieldOneofCase IdFieldCase { get; set; }

    /// <summary>
    /// Int id.
    /// </summary>
    public MilvusId<long> IntId { get; set; }

    /// <summary>
    /// String id.
    /// </summary>
    public MilvusId<string> StrId { get; set; }
}

/// <summary>
/// Id field type.
/// </summary>
public enum MilvusIdFieldOneofCase
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Int id.
    /// </summary>
    IntId = 1,

    /// <summary>
    /// String id.
    /// </summary>
    StrId = 2,
}

/// <summary>
/// Milvus id
/// </summary>
/// <typeparam name="TId"><see cref="int"/> or <see cref="string"/></typeparam>
public class MilvusId<TId>
{
    /// <summary>
    /// Create a new instance of <see cref="MilvusId{TId}"/>
    /// </summary>
    public MilvusId() { }

    /// <summary>
    /// Create a int or string Milvus id.
    /// </summary>
    /// <param name="ids"></param>
    public MilvusId(IList<TId> ids)
    {
        Data = ids;
    }

    /// <summary>
    /// Value
    /// </summary>
    [JsonPropertyName("data")]
    public IList<TId> Data { get; set; }
}