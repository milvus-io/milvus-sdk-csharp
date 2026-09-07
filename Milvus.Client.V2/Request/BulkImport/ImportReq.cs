#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.BulkImport;
public sealed class ImportReq
{
    public string CollectionName { get; set; } = "";
    public string? PartitionName { get; set; }
    public bool RowBased { get; set; }
    public IReadOnlyList<string> Files { get; set; } = Array.Empty<string>();
    public IDictionary<string, string> Options { get; } = new Dictionary<string, string>();
    internal Grpc.ImportRequest ToGrpcImportRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(Files);
        var request = new Grpc.ImportRequest
        {
            CollectionName = CollectionName,
            PartitionName = PartitionName ?? "",
            RowBased = RowBased
        };
        request.Files.AddRange(Files);
        foreach (KeyValuePair<string, string> option in Options)
        {
            request.Options.Add(new Grpc.KeyValuePair { Key = option.Key, Value = option.Value });
        }
        return request;
    }
}
