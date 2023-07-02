using IO.Milvus.Grpc;
using System.Linq;
using System.Collections.Generic;

namespace IO.Milvus;

/// <summary>
/// milvus user result.
/// </summary>
public sealed class MilvusUserResult
{
    internal MilvusUserResult(string username, IEnumerable<string> roles)
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

        foreach (var result in results)
        {
            yield return new MilvusUserResult(
                result.User.Name,
                result.Roles?.Select(r => r.Name) ?? Enumerable.Empty<string>());
        }
    }
}
