#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Types;
namespace Milvus.Client.V2.Responses.BulkImport;
public sealed class ImportJobInfo
{
    internal ImportJobInfo(long id, ImportState state, long rowCount, IReadOnlyDictionary<string, string> infos)
    {
        Id = id;
        State = state;
        RowCount = rowCount;
        Infos = infos;
    }
    internal static ImportJobInfo FromGrpc(Grpc.GetImportStateResponse response)
    {
        var infos = new Dictionary<string, string>();
        foreach (Grpc.KeyValuePair info in response.Infos)
        {
            infos[info.Key] = info.Value;
        }
        return new ImportJobInfo(response.Id, (ImportState)response.State, response.RowCount, infos);
    }
    public long Id { get; }
    public ImportState State { get; }
    public long RowCount { get; }
    public IReadOnlyDictionary<string, string> Infos { get; }
}
