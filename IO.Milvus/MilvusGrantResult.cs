using IO.Milvus.Grpc;

namespace IO.Milvus;

/// <summary>
/// Milvus grant entity.
/// </summary>
/// <remarks>
/// <see cref="Client.MilvusClient.SelectGrantForRoleAsync(string, System.Threading.CancellationToken)"/> and
/// <see cref="Client.MilvusClient.SelectGrantForRoleAndObjectAsync(string, string, string, System.Threading.CancellationToken)"/>
/// </remarks>
public sealed class MilvusGrantEntity
{
    private MilvusGrantEntity(
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
    private MilvusGrantorEntity(string privilege, string userName)
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
        => new(grantor.Privilege.Name, grantor.User.Name);
}
