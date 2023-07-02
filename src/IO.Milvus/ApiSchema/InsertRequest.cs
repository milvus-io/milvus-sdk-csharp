using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Insert rows of data entities into a collection
/// </summary>
internal sealed class InsertRequest
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
    public long NumRows { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static InsertRequest Create(string collectionName, string dbName)
    {
        return new InsertRequest(collectionName, dbName);
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
            payload: this
            );
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        
        Verify.NotNullOrEmpty(FieldsData);
        long count = FieldsData[0].RowCount;
        for (int i = 1; i < FieldsData.Count; i++)
        {
            if (FieldsData[i].RowCount != count)
            {
                throw new ArgumentOutOfRangeException($"{nameof(FieldsData)}[{i}])", "Fields length is not same");
            }
        }
    }

    #region Private ===============================================================
    private InsertRequest(string collectionName, string dbName)
    {
        this.CollectionName = collectionName;
        this.DbName = dbName;
    }
    #endregion
}