using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to load a collection into memory.
/// </summary>
public sealed class LoadCollectionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to load.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The replica number to load. Defaults to 1.
    /// </summary>
    public int ReplicaNumber { get; set; } = 1;

    /// <summary>
    /// Whether to wait until the collection is fully loaded. Defaults to <c>true</c>, matching the C++ and Java
    /// SDKs.
    /// </summary>
    public bool Sync { get; set; } = true;

    /// <summary>
    /// The maximum time in milliseconds to wait for the collection to load when <see cref="Sync" /> is enabled.
    /// Defaults to 60000. A value of 0 means wait forever.
    /// </summary>
    public long TimeoutMs { get; set; } = 60000;

    /// <summary>
    /// When true, loads with refresh enabled, causing the server to reload the collection metadata.
    /// </summary>
    public bool Refresh { get; set; }

    /// <summary>
    /// When set, only the listed fields are loaded into memory (field partial loading).
    /// </summary>
    public IReadOnlyList<string> LoadFields { get; set; } = Array.Empty<string>();

    /// <summary>
    /// When true, dynamic fields are not loaded even when partial loading is not otherwise requested.
    /// </summary>
    public bool SkipLoadDynamicField { get; set; }

    /// <summary>
    /// The resource groups to load the collection replicas into.
    /// </summary>
    public IReadOnlyList<string> TargetResourceGroups { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The load priority, e.g. <c>"Low"</c>, <c>"Medium"</c> or <c>"High"</c>. When set, it is sent to the server
    /// as the <c>load_priority</c> load parameter (normalized to lower case), matching the Java SDK.
    /// </summary>
    public string? Priority { get; set; }

    internal Grpc.LoadCollectionRequest ToGrpcLoadCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(LoadFields);
        Verify.NotNull(TargetResourceGroups);

        var request = new Grpc.LoadCollectionRequest
        {
            CollectionName = CollectionName,
            ReplicaNumber = ReplicaNumber,
            Refresh = Refresh,
            SkipLoadDynamicField = SkipLoadDynamicField
        };

        request.LoadFields.AddRange(LoadFields);
        request.ResourceGroups.AddRange(TargetResourceGroups);

        if (Priority is not null)
        {
            // The proxy's GetLoadPriority only special-cases the exact lowercase "low"; any other value
            // (including "medium"/"high") resolves to HIGH. The value is sent normalized to lower case
            // matching the Java SDK, but callers should be aware that only "Low" yields a low-priority load.
#pragma warning disable CA1308, CA1847 // The server contract requires lowercase load-priority values; the string overload is required for netstandard2.0/net462.
            request.LoadParams[Constants.LoadPriority] = Priority.ToLowerInvariant();
#pragma warning restore CA1308, CA1847
        }

        request.DbName = DatabaseName ?? "";
        return request;
    }
}
