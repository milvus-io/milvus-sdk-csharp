using IO.Milvus.ApiSchema;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace IO.Milvus;

//TODO: More properties support

/// <summary>
/// Mutation result wrapper
/// </summary>
public class MilvusMutationResult
{
    internal MilvusMutationResult(long insertCnt) { }

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
        Timestamp  = dateTime;
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
    /// Id field
    /// </summary>
    public IdField IdField { get; set; }

    #region Private =========================================================
    internal static MilvusIds From(IDs ids)
    {
        if (ids == null) return null;

        var idField = new IdField()
        {
            IdFieldCase = (MilvusIdFieldOneofCase)ids.IdFieldCase,
        };

        if (ids.IntId?.Data?.Any() == true)
        {
            idField.IntId = new MilvusId<long>
            {
                Data = ids.IntId.Data.ToList(),
            };
        }

        if (ids.StrId?.Data?.Any() == true)
        {
            idField.StrId = new MilvusId<String>
            {
                Data = ids.StrId.Data.ToList(),
            };
        }

        return new MilvusIds()
        {
            IdField = idField
        };
    }
    #endregion
}

/// <summary>
/// Id field
/// </summary>
public class IdField
{
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
/// <typeparam name="TId"></typeparam>
public class MilvusId<TId>
{
    /// <summary>
    /// Value
    /// </summary>
    [JsonPropertyName("data")]
    public IList<TId> Data { get; set;}
}