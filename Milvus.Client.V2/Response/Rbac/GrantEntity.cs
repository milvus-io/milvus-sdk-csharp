#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class GrantEntity
{
    internal GrantEntity(string roleName, string objectName, string objectType, string dbName, string privilege)
    {
        RoleName = roleName;
        ObjectName = objectName;
        ObjectType = objectType;
        DbName = dbName;
        Privilege = privilege;
    }
    internal static GrantEntity FromGrpc(Grpc.GrantEntity entity)
        => new(
            entity.Role?.Name ?? "",
            entity.ObjectName,
            entity.Object?.Name ?? "",
            entity.DbName,
            entity.Grantor?.Privilege?.Name ?? "");
    public string RoleName { get; }
    public string ObjectName { get; }
    public string ObjectType { get; }
    public string DbName { get; }
    public string Privilege { get; }
}
