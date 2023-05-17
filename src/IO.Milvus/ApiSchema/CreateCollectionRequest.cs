using Google.Protobuf;
using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create a collection
/// </summary>
internal sealed class CreateCollectionRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.CreateCollectionRequest>
{
    #region Properties
    /// <summary>
    /// The unique collection name in milvus.(Required)
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The consistency level that the collection used, modification is not supported now.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item>"Strong": 0</item>
    /// <item>"Session": 1</item>
    /// <item>"Bounded": 2</item>
    /// <item>"Eventually": 3</item>
    /// <item>"Customized": 4</item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("consistency_level")]
    public ConsistencyLevel ConsistencyLevel { get; set; }

    /// <summary>
    /// Once set, no modification is allowed (Optional)
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/milvus-io/milvus/issues/6690"/>
    /// </remarks>
    [JsonPropertyName("shards_num")]
    public int ShardsNum { get; set; } = 5;

    /// <summary>
    /// Collection schema
    /// </summary>
    [JsonPropertyName("schema")]
    public CollectionSchema Schema { get; set; } = new CollectionSchema();
    #endregion

    #region Methods
    public static CreateCollectionRequest Create(string collectionName)
    {
        return new CreateCollectionRequest(collectionName);
    }

    public CreateCollectionRequest WithFieldTypes(IList<FieldType> fieldTypes)
    {
        Schema.Fields = fieldTypes;

        return this;
    }

    public CreateCollectionRequest WithDescription(string description)
    {
        Schema.Description = description;
        return this;
    }

    public CreateCollectionRequest WithConsistencyLevel(ConsistencyLevel consistencyLevel)
    {
        ConsistencyLevel = consistencyLevel;
        return this;
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");

    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/collection",
            this);
    }

    public Grpc.CreateCollectionRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.CreateCollectionRequest()
        {
            CollectionName = CollectionName,
            ConsistencyLevel = (Grpc.ConsistencyLevel)((int)ConsistencyLevel),
            ShardsNum = ShardsNum,
            Schema = Schema.ConvertCollectionSchema().ToByteString(),
        };
    }
    #endregion

    #region Private ======================================================================
    private CreateCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }

    
    #endregion
}