using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

#pragma warning disable CA1056 // ObjectUrl is a plain string to mirror the Rust SDK's BulkImportRequest.

/// <summary>
/// Represents a request to create a bulk-import job from data files (or cloud object/volume sources).
/// Mirrors the Rust SDK's <c>BulkImportRequest</c>.
/// </summary>
public sealed class BulkImportRequest
{
    /// <summary>
    /// The database to import into. Defaults to <c>"default"</c>.
    /// </summary>
    public string DatabaseName { get; set; } = "default";

    /// <summary>
    /// The target collection name.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional partition to import into.
    /// </summary>
    public string? PartitionName { get; set; }

    /// <summary>
    /// The data files to import, grouped: each inner list is one file group. Open-source Milvus only uses this
    /// import mode, and exactly one import source (<see cref="Files" />, object URLs or a volume) must be set.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Files { get; set; } = Array.Empty<IReadOnlyList<string>>();

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

    /// <summary>
    /// A singular object URL (Zilliz Cloud only; deprecated, prefer <see cref="ObjectUrls" />).
    /// </summary>
    public string? ObjectUrl { get; set; }

    /// <summary>
    /// The object URLs to import, grouped (Zilliz Cloud only).
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> ObjectUrls { get; set; } = Array.Empty<IReadOnlyList<string>>();

    /// <summary>
    /// The access key for object-URL import mode (Zilliz Cloud only).
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// The secret key for object-URL import mode (Zilliz Cloud only).
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// The token for object-URL import mode (Zilliz Cloud only).
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// The volume name for volume import mode (Zilliz Cloud only).
    /// </summary>
    public string? VolumeName { get; set; }

    /// <summary>
    /// The data paths for volume import mode (Zilliz Cloud only; requires <see cref="VolumeName" />).
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> DataPaths { get; set; } = Array.Empty<IReadOnlyList<string>>();

    /// <summary>
    /// The import options, e.g. <c>["format"] = "json"</c>.
    /// </summary>
    public IDictionary<string, string> Options { get; } = new Dictionary<string, string>();

    internal Dictionary<string, object?> ToBody()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        // The server binds ImportReq.Files as [][]string (one group per entry), so grouped files serialize to
        // a string[][] rather than a flat array, which the server rejects.
        bool hasFiles = Files.Count > 0;
        bool hasObjectUrls = ObjectUrls.Count > 0 || !string.IsNullOrEmpty(ObjectUrl);
        bool hasVolume = !string.IsNullOrEmpty(VolumeName) || DataPaths.Count > 0;

        // A request must describe exactly one import source; reject none or multiple up front (mirrors the
        // Rust builder's "exactly one import mode" validation).
        int modes = (hasFiles ? 1 : 0) + (hasObjectUrls ? 1 : 0) + (hasVolume ? 1 : 0);
        if (modes != 1)
        {
            throw new ArgumentException(
                "BulkImportRequest must specify exactly one import source: Files, object URLs (ObjectUrl/ObjectUrls) or a volume (VolumeName/DataPaths).",
                nameof(Files));
        }

        var payload = new Dictionary<string, object?>
        {
            ["dbName"] = DatabaseName,
            ["collectionName"] = CollectionName
        };

        if (!string.IsNullOrEmpty(PartitionName))
        {
            payload["partitionName"] = PartitionName;
        }

        if (hasFiles)
        {
            payload["files"] = Files.Select(g => g.ToArray()).ToArray();
        }

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

        if (!string.IsNullOrEmpty(ObjectUrl))
        {
            payload["objectUrl"] = ObjectUrl;
        }

        if (ObjectUrls.Count > 0)
        {
            payload["objectUrls"] = ObjectUrls.Select(g => g.ToArray()).ToArray();
        }

        if (!string.IsNullOrEmpty(AccessKey))
        {
            payload["accessKey"] = AccessKey;
        }

        if (!string.IsNullOrEmpty(SecretKey))
        {
            payload["secretKey"] = SecretKey;
        }

        if (!string.IsNullOrEmpty(Token))
        {
            payload["token"] = Token;
        }

        if (!string.IsNullOrEmpty(VolumeName))
        {
            payload["volumeName"] = VolumeName;
        }

        if (DataPaths.Count > 0)
        {
            payload["dataPaths"] = DataPaths.Select(g => g.ToArray()).ToArray();
        }

        if (Options.Count > 0)
        {
            payload["options"] = Options;
        }

        return payload;
    }
}
