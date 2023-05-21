using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IO.Milvus;

//TODO: More properties support

/// <summary>
/// Mutation result wrapper
/// </summary>
public class MilvusMutationResult
{
    internal MilvusMutationResult(long insertCnt, IList<uint> succIndex, bool acknowledged, DateTime timestamp, MutationResult mutationResult)
    {
        InsertCnt = insertCnt;
        SuccIndex = succIndex;
        Acknowledged = acknowledged;
        Timestamp = timestamp;
        MutationResult = mutationResult;
    }

    internal static MilvusMutationResult From(Grpc.MutationResult mutationResult)
    {
        return new MilvusMutationResult(
            mutationResult.InsertCnt,
            mutationResult.SuccIndex.ToList(),
            mutationResult.Acknowledged,
            TimestampUtils.GetTimeFromTimstamp((long)mutationResult.Timestamp),
            mutationResult
            );
    }

    /// <summary>
    /// Source mutation result from grpc response.
    /// </summary>
    public Grpc.MutationResult MutationResult { get; }

    /// <summary>
    /// Acknowledged
    /// </summary>
    public bool Acknowledged { get; }

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Insert count
    /// </summary>
    public long InsertCnt { get; }

    /// <summary>
    /// Success index
    /// </summary>
    public IList<uint> SuccIndex { get; }
}