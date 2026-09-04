using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to switch the default database of this client.
/// </summary>
public sealed class UseDatabaseReq
{
    /// <summary>
    /// The name of the database to switch to.
    /// </summary>
    public string DatabaseName { get; set; } = "";

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);
    }
}
