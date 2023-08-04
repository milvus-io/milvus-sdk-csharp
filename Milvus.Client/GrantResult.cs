namespace Milvus.Client;

/// <summary>
/// Milvus grant entity.
/// </summary>
/// <remarks>
/// <see cref="MilvusClient.ListGrantsForRoleAsync"/> and
/// <see cref="Client.MilvusClient.SelectGrantForRoleAndObjectAsync(string, string, string, System.Threading.CancellationToken)"/>
/// </remarks>
public sealed class GrantEntity
{
    internal GrantEntity(
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
