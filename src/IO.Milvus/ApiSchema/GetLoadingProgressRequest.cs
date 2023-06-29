using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetLoadingProgressRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.GetLoadingProgressRequest>
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; set;}

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
        this.Validate();

        var request = new Grpc.GetLoadingProgressRequest()
        {
            CollectionName = this.CollectionName,
        };

        if (PartitionNames?.Any() == true)
        {
            request.PartitionNames.AddRange(PartitionNames);
        }
        
        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/load/progress",
            payload:this);
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
    }

    #region Private ===============================================================================
    private GetLoadingProgressRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}