namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// A single privilege grant: a privilege on an object granted to a role.
/// </summary>
public sealed class GrantEntity
{
    internal GrantEntity(string roleName, string objectName, string objectType, string dbName, string privilege, string grantor)
    {
        RoleName = roleName;
        ObjectName = objectName;
        ObjectType = objectType;
        DbName = dbName;
        Privilege = privilege;
        Grantor = grantor;
    }
    internal static GrantEntity FromGrpc(Grpc.GrantEntity entity)
        => new(
            entity.Role?.Name ?? "",
            entity.ObjectName,
            entity.Object?.Name ?? "",
            entity.DbName,
            entity.Grantor?.Privilege?.Name ?? "",
            entity.Grantor?.User?.Name ?? "");

    /// <summary>
    /// The name of the role the privilege is granted to.
    /// </summary>
    public string RoleName { get; }

    /// <summary>
    /// The name of the object the privilege applies to.
    /// </summary>
    public string ObjectName { get; }

    /// <summary>
    /// The type of the object the privilege applies to (e.g. <c>Collection</c>, <c>Global</c>).
    /// </summary>
    public string ObjectType { get; }

    /// <summary>
    /// The database in which the object is defined.
    /// </summary>
    public string DbName { get; }

    /// <summary>
    /// The granted privilege (e.g. <c>Search</c>, <c>Query</c>, <c>Insert</c>).
    /// </summary>
    public string Privilege { get; }

    /// <summary>
    /// The user who granted the privilege.
    /// </summary>
    public string Grantor { get; }
}
