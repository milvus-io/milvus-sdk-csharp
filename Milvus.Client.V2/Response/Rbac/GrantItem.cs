namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// A privilege grant item belonging to a role, mirroring the C++ <c>GrantItem</c> / Java <c>GrantInfo</c>.
/// </summary>
public sealed class GrantItem
{
    internal GrantItem(
        string objectType, string objectName, string dbName, string roleName, string grantor, string privilege)
    {
        ObjectType = objectType;
        ObjectName = objectName;
        DbName = dbName;
        RoleName = roleName;
        Grantor = grantor;
        Privilege = privilege;
    }

    /// <summary>
    /// The object type (e.g. <c>Global</c>, <c>Collection</c>, <c>User</c>).
    /// </summary>
    public string ObjectType { get; }

    /// <summary>
    /// The object name the privilege applies to.
    /// </summary>
    public string ObjectName { get; }

    /// <summary>
    /// The database in which the grant takes effect.
    /// </summary>
    public string DbName { get; }

    /// <summary>
    /// The role the grant is assigned to.
    /// </summary>
    public string RoleName { get; }

    /// <summary>
    /// The grantor user.
    /// </summary>
    public string Grantor { get; }

    /// <summary>
    /// The granted privilege.
    /// </summary>
    public string Privilege { get; }

    internal static GrantItem FromGrpc(Grpc.GrantEntity entity)
        => new(
            entity.Object?.Name ?? "",
            entity.ObjectName,
            entity.DbName,
            entity.Role?.Name ?? "",
            entity.Grantor?.User?.Name ?? "",
            entity.Grantor?.Privilege?.Name ?? "");
}
