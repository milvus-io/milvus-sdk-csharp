namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to list bulk-import jobs of a collection (or all collections when
/// <see cref="CollectionName" /> is empty). Mirrors the Rust SDK's <c>ListImportJobsRequest</c>, minus its
/// paging fields: the 2.6 server binds only <c>collectionName</c> + <c>dbName</c> for the list-imports route,
/// so <c>pageSize</c>/<c>currentPage</c> are not honored on the 2.6 API surface.
/// </summary>
public sealed class ListImportJobsRequest
{
    /// <summary>
    /// The database whose import jobs to list. Defaults to <c>"default"</c>.
    /// </summary>
    public string DatabaseName { get; set; } = "default";

    /// <summary>
    /// The collection whose import jobs to list; empty lists jobs of all collections.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The cloud deployment target (Zilliz Cloud only).
    /// </summary>
    public string? ClusterId { get; set; }

    /// <summary>
    /// The cloud project id (Zilliz Cloud only).
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// The cloud region id (Zilliz Cloud only).
    /// </summary>
    public string? RegionId { get; set; }

    internal Dictionary<string, object?> ToBody()
    {
        var payload = new Dictionary<string, object?>
        {
            ["collectionName"] = CollectionName,
            ["dbName"] = DatabaseName
        };

        if (!string.IsNullOrEmpty(ClusterId))
        {
            payload["clusterId"] = ClusterId;
        }

        if (!string.IsNullOrEmpty(ProjectId))
        {
            payload["projectId"] = ProjectId;
        }

        if (!string.IsNullOrEmpty(RegionId))
        {
            payload["regionId"] = RegionId;
        }

        return payload;
    }
}
