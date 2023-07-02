using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using System;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Do a manual compaction
/// </summary>
internal sealed class ManualCompactionRequest
{
    /// <summary>
    /// Collection Id
    /// </summary>
    [JsonPropertyName("collectionID")]
    public long CollectionId { get; set; }

    /// <summary>
    /// Timetravel
    /// </summary>
    [JsonPropertyName("timetravel")]
    public long TimeTravel { get; set; }

    internal static ManualCompactionRequest Create(long collectionId)
    {
        return new ManualCompactionRequest(collectionId);
    }

    public Grpc.ManualCompactionRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.ManualCompactionRequest()
        {
            CollectionID = this.CollectionId,
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/compaction",
            payload: this);
    }

    public void Validate()
    {
        Verify.GreaterThan(CollectionId, 0);
    }

    internal ManualCompactionRequest WithTimetravel(DateTime? timetravel)
    {
        if (timetravel != null)
        {
            this.TimeTravel = TimestampUtils.ToUtcTimestamp(timetravel.Value);
        }

        return this;
    }

    #region Private ======================================================================
    private ManualCompactionRequest(long collectionId)
    {
        CollectionId = collectionId;
    }
    #endregion
}
