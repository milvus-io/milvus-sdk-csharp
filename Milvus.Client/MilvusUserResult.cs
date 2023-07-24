namespace Milvus.Client;

/// <summary>
/// milvus user result.
/// </summary>
public sealed class MilvusUserResult
{
    private MilvusUserResult(string username, IEnumerable<string> roles)
    {
        Username = username;
        Roles = roles;
    }

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Roles that user has.
    /// </summary>
    public IEnumerable<string> Roles { get; }

    internal static IEnumerable<MilvusUserResult> Parse(IEnumerable<UserResult> results)
    {
        if (results == null)
            yield break;

        foreach (UserResult result in results)
        {
            yield return new MilvusUserResult(
                result.User.Name,
                result.Roles?.Select(static r => r.Name) ?? Enumerable.Empty<string>());
        }
    }
}
