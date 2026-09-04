#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;
public sealed class UpdateResourceGroupsReq
{
    public IReadOnlyDictionary<string, string> ResourceGroups { get; set; } = new Dictionary<string, string>();
    internal Grpc.UpdateResourceGroupsRequest ToGrpcUpdateResourceGroupsRequest()
    {
        Verify.NotNullOrEmpty(ResourceGroups.Keys.ToList());
        var request = new Grpc.UpdateResourceGroupsRequest();
        foreach (KeyValuePair<string, string> entry in ResourceGroups)
        {
            request.ResourceGroups.Add(entry.Key, Grpc.ResourceGroupConfig.Parser.ParseJson(entry.Value));
        }
        return request;
    }
}
