#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;
public sealed class AlterCollectionFieldReq
{
    public string CollectionName { get; set; } = "";
    public string FieldName { get; set; } = "";
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();
    public IReadOnlyList<string>? DeleteKeys { get; set; }
    internal Grpc.AlterCollectionFieldRequest ToGrpcAlterCollectionFieldRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FieldName);
        var request = new Grpc.AlterCollectionFieldRequest
        {
            CollectionName = CollectionName,
            FieldName = FieldName
        };
        foreach (KeyValuePair<string, string> property in Properties)
        {
            request.Properties.Add(new Grpc.KeyValuePair { Key = property.Key, Value = property.Value });
        }
        if (DeleteKeys is not null)
        {
            request.DeleteKeys.AddRange(DeleteKeys);
        }
        return request;
    }
}
