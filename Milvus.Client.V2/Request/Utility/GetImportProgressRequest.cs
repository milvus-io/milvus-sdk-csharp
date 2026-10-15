using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Parameters identifying one import job whose progress should be described.
/// Mirrors the Rust SDK's <c>GetImportProgressRequest</c> / <c>DescribeImportRequest</c>.
/// </summary>
public sealed class GetImportProgressRequest
{
    /// <summary>
    /// The database the job was created in. Defaults to <c>"default"</c>; carried on the <c>DB-Name</c> header
    /// because the progress endpoint derives the database from there rather than the body.
    /// </summary>
    public string DatabaseName { get; set; } = "default";

    /// <summary>
    /// The import job id.
    /// </summary>
    public string JobId { get; set; } = "";

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
        Verify.NotNullOrWhiteSpace(JobId);

        var payload = new Dictionary<string, object?>
        {
            ["jobID"] = JobId
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
