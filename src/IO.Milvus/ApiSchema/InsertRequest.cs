using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Insert rows of data entities into a collection
/// </summary>
internal class InsertRequest:
    IValidatable,
    IRestRequest
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Partition name
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }

    /// <summary>
    /// Fields data
    /// </summary>
    [JsonPropertyName("fields_data")]
    public IList<Field> FieldsData { get; set; }

    /// <summary>
    /// Number of rows
    /// </summary>
    [JsonPropertyName("num_rows")]
    public int NumRows { get; set; }

    public static InsertRequest Create(string collectionName)
    {
        return new InsertRequest(collectionName);
    }

    public InsertRequest WithPartitionName(string partitionName)
    {
        PartitionName = partitionName;
        return this;
    }

    public InsertRequest WithFields(IList<Field> fields)
    {
        FieldsData = fields;
        return this;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();
        NumRows = this.FieldsData.First().RowCount;

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/entities",
            payload:this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.True(FieldsData?.Any() == true, "Fields cannot be null or empty");

        var count = this.FieldsData.First().RowCount;
        Verify.True(this.FieldsData.All(p => p.RowCount == count), "Fields length is not same");
    }

    #region Private ===============================================================
    private InsertRequest(string collectionName) 
    {
        CollectionName = collectionName;
    }
    #endregion
}