using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetLoadingProgressRequest
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; set; }

    public static GetLoadingProgressRequest Create(string collectionName)
    {
        return new GetLoadingProgressRequest(collectionName);
    }

    public GetLoadingProgressRequest WithPartitionNames(IList<string> partitionName)
    {
        PartitionNames = partitionName;
        return this;
    }

    public Grpc.GetLoadingProgressRequest BuildGrpc()
    {
        Validate();

        var request = new Grpc.GetLoadingProgressRequest()
        {
            CollectionName = CollectionName,
        };

        if (PartitionNames?.Count > 0)
        {
            request.PartitionNames.AddRange(PartitionNames);
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/load/progress",
            payload: this);
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
    }

    #region Private ===============================================================================
    private GetLoadingProgressRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}