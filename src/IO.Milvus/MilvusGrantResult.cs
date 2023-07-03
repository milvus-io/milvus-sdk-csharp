using IO.Milvus.Grpc;
using System.Collections.Generic;

namespace IO.Milvus;

/// <summary>
/// Milvus grant entity.
/// </summary>
/// <remarks>
/// <see cref="Client.IMilvusClient.SelectGrantForRoleAsync(string, System.Threading.CancellationToken)"/> and
/// <see cref="Client.IMilvusClient.SelectGrantForRoleAndObjectAsync(string, string, string, System.Threading.CancellationToken)"/>
/// </remarks>
public sealed class MilvusGrantEntity
{
    internal MilvusGrantEntity(
        MilvusGrantorEntity grantor,
        string dbName,
        string @object,
        string role,
        string objectName)
    {
        Grantor = grantor;
        DbName = dbName;
        Object = @object;
        Role = role;
        ObjectName = objectName;
    }

    /// <summary>
    /// Grantor.
    /// </summary>
    public MilvusGrantorEntity Grantor { get; }

    /// <summary>
    /// Database name.
    /// </summary>
    public string DbName { get; }

    /// <summary>
    /// Object.
    /// </summary>
    public string Object { get; }

    /// <summary>
    /// Role.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Object name.
    /// </summary>
    public string ObjectName { get; }

    internal static IEnumerable<MilvusGrantEntity> Parse(IEnumerable<GrantEntity> entities)
    {
        if (entities == null)
            yield break;

        foreach (GrantEntity entity in entities)
        {
            yield return new MilvusGrantEntity(
                MilvusGrantorEntity.Parse(entity.Grantor),
                entity.DbName,
                entity.Object.Name,
                entity.Role.Name,
                entity.ObjectName);
        }
    }
}

/// <summary>
/// Milvus grantor result.
/// </summary>
public sealed class MilvusGrantorEntity
{
    internal MilvusGrantorEntity(string privilege, string userName)
    {
        Privilege = privilege;
        UserName = userName;
    }

    /// <summary>
    /// Privilege  name.
    /// </summary>
    public string Privilege { get; }

    /// <summary>
    /// User name.
    /// </summary>
    public string UserName { get; }

    internal static MilvusGrantorEntity Parse(GrantorEntity grantor)
    {
        return new MilvusGrantorEntity(grantor.Privilege.Name, grantor.User.Name);
    }
}