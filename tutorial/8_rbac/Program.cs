using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Rbac;
using Milvus.Client.V2.Responses.Rbac;
using Milvus.Client.V2.Types;

string uri = Environment.GetEnvironmentVariable("MILVUS_URI") ?? "localhost:19530";
string token = Environment.GetEnvironmentVariable("MILVUS_TOKEN") ?? "root:Milvus";
string suffix = UniqueSuffix();
// Milvus 2.6 defaults maxUsernameLength to 32 bytes. Keep the username compact while retaining
// the process ID and millisecond timestamp for uniqueness across tutorial runs.
string username = $"u{suffix}";
string role = $"csharp_tutorial_role_{suffix}";
string group = $"csharp_tutorial_group_{suffix}";

// MilvusClientV2 connects with an administrator credential. The token must be allowed to create
// users, roles, privilege groups, and grants.
Console.WriteLine("Calling MilvusClientV2: connect with an administrator credential");
using var client = CreateClient(uri, token);
await client.ConnectAsync();
Console.WriteLine("MilvusClientV2 connected");

// Capture the tutorial result so cleanup still runs if a later operation fails.
Exception? tutorialError = await DemonstrateRbac(client, username, role, group);
Exception? cleanupError = await CleanupRbac(client, username, role, group);

if (cleanupError is not null)
{
    Console.WriteLine($"RBAC cleanup failed: {cleanupError.Message}");
}

if (tutorialError is not null)
{
    throw tutorialError;
}

if (cleanupError is not null)
{
    throw cleanupError;
}

async Task<Exception?> DemonstrateRbac(MilvusClientV2 client, string username, string role, string group)
{
    try
    {
        // CreateUserAsync creates a login identity. `UserName` is the login name and `Password` is its
        // secret.
        Console.WriteLine($"Calling CreateUserAsync: create {username}");
        await client.CreateUserAsync(new CreateUserReq
        {
            UserName = username,
            Password = "CsharpTutorial!123",
            Description = "temporary C# SDK tutorial user"
        });
        Console.WriteLine("CreateUserAsync completed");

        // CreateRoleAsync creates a named permission container; privileges are attached in later calls.
        Console.WriteLine($"Calling CreateRoleAsync: create {role}");
        await client.CreateRoleAsync(new CreateRoleReq
        {
            RoleName = role,
            Description = "temporary C# SDK tutorial role"
        });
        Console.WriteLine("CreateRoleAsync completed");

        // CreatePrivilegeGroupAsync creates a reusable custom group identified by `GroupName`.
        Console.WriteLine($"Calling CreatePrivilegeGroupAsync: create {group}");
        await client.CreatePrivilegeGroupAsync(new CreatePrivilegeGroupReq { GroupName = group });
        Console.WriteLine("CreatePrivilegeGroupAsync completed");

        // AddPrivilegesToGroupAsync adds built-in privileges to the group. `Query` permits scalar reads
        // and `Search` permits vector searches.
        Console.WriteLine($"Calling AddPrivilegesToGroupAsync: add Query and Search to {group}");
        await client.AddPrivilegesToGroupAsync(new AddPrivilegesToGroupReq
        {
            GroupName = group,
            Privileges = ["Query", "Search"]
        });
        Console.WriteLine("AddPrivilegesToGroupAsync completed");

        // GrantPrivilegeV2Async attaches the custom group to the role. `CollectionName` "*" covers all
        // collections and `Privilege` names the group.
        Console.WriteLine($"Calling GrantPrivilegeV2Async: attach {group} to {role}");
        await client.GrantPrivilegeV2Async(new GrantPrivilegeReqV2
        {
            RoleName = role,
            DatabaseName = "default",
            CollectionName = "*",
            Privilege = group
        });
        Console.WriteLine("GrantPrivilegeV2Async completed");

        // GrantRoleAsync assigns the named role to the named user.
        Console.WriteLine($"Calling GrantRoleAsync: assign {role} to {username}");
        await client.GrantRoleAsync(new GrantRoleReq { UserName = username, RoleName = role });
        Console.WriteLine("GrantRoleAsync completed");

        Console.WriteLine($"Calling DescribeUserAsync: inspect {username} and its roles");
        DescribeUserResp user = await client.DescribeUserAsync(new DescribeUserReq { UserName = username });
        Console.WriteLine($"DescribeUserAsync completed: user={user.User?.User}, roles={string.Join(", ", user.User?.Roles ?? Array.Empty<string>())}");

        Console.WriteLine($"Calling DescribeRoleAsync: inspect grants for {role}");
        DescribeRoleResp roleDescription = await client.DescribeRoleAsync(new DescribeRoleReq { RoleName = role, DatabaseName = "default" });
        Console.WriteLine($"DescribeRoleAsync completed: users={string.Join(", ", roleDescription.Users)}, grants={roleDescription.Grants.Count}");

        Console.WriteLine("Calling ListUsersAsync");
        ListUsersResp users = await client.ListUsersAsync(new ListUsersReq());
        Console.WriteLine($"ListUsersAsync completed: users={string.Join(", ", users.Users)}");

        Console.WriteLine("Calling ListPrivilegeGroupsAsync");
        ListPrivilegeGroupsResp groups = await client.ListPrivilegeGroupsAsync(new ListPrivilegeGroupsReq());
        Console.WriteLine($"ListPrivilegeGroupsAsync completed: groups={string.Join(", ", groups.GroupNames)}");

        Console.WriteLine($"Calling RevokePrivilegeV2Async: remove {group} from {role}");
        await client.RevokePrivilegeV2Async(new RevokePrivilegeReqV2
        {
            RoleName = role,
            DatabaseName = "default",
            CollectionName = "*",
            Privilege = group
        });
        Console.WriteLine("RevokePrivilegeV2Async completed");

        Console.WriteLine($"Calling RevokeRoleAsync: unassign {role} from {username}");
        await client.RevokeRoleAsync(new RevokeRoleReq { UserName = username, RoleName = role });
        Console.WriteLine("RevokeRoleAsync completed");

        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

async Task<Exception?> CleanupRbac(MilvusClientV2 client, string username, string role, string group)
{
    Exception? firstError = null;
    try
    {
        Console.WriteLine($"Calling DropRoleAsync: remove {role}");
        await client.DropRoleAsync(new DropRoleReq { RoleName = role });
        Console.WriteLine("DropRoleAsync completed");
    }
    catch (Exception ex)
    {
        firstError ??= ex;
    }

    try
    {
        Console.WriteLine($"Calling DropUserAsync: remove {username}");
        await client.DropUserAsync(new DropUserReq { UserName = username });
        Console.WriteLine("DropUserAsync completed");
    }
    catch (Exception ex)
    {
        firstError ??= ex;
    }

    try
    {
        Console.WriteLine($"Calling DropPrivilegeGroupAsync: remove {group}");
        await client.DropPrivilegeGroupAsync(new DropPrivilegeGroupReq { GroupName = group });
        Console.WriteLine("DropPrivilegeGroupAsync completed");
    }
    catch (Exception ex)
    {
        firstError ??= ex;
    }

    return firstError;
}

static MilvusClientV2 CreateClient(string uri, string token)
{
    var config = new ConnectConfig { Uri = uri };
    int colon = token.IndexOf(':');
    if (colon > 0)
    {
        config.Username = token[..colon];
        config.Password = token[(colon + 1)..];
    }
    else
    {
        config.ApiKey = token;
    }

    return new MilvusClientV2(config);
}

static string UniqueSuffix()
{
    return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Environment.ProcessId}".Replace("-", "");
}
