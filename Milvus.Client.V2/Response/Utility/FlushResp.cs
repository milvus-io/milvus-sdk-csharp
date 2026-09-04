#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class FlushResp
{
    internal FlushResp(IReadOnlyDictionary<string, IReadOnlyList<long>> collSegIDs) => CollSegIDs = collSegIDs;
    internal static FlushResp FromGrpc(Grpc.FlushResponse response)
    {
        var collSegIDs = new Dictionary<string, IReadOnlyList<long>>();
        foreach (KeyValuePair<string, Grpc.LongArray> entry in response.CollSegIDs)
        {
            collSegIDs[entry.Key] = entry.Value.Data.ToList();
        }
        return new FlushResp(collSegIDs);
    }
    public IReadOnlyDictionary<string, IReadOnlyList<long>> CollSegIDs { get; }
}
