using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Rbac;
using Milvus.Client.V2.Responses.Rbac;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates users, roles and privileges. Mirrors cpp examples/src/v2/rbac.cpp and java RBACExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show the RBAC lifecycle: create a user and a role, grant the role to the user,
/// grant/revoke a privilege, and list the results before cleaning up.</para>
/// <para><b>APIs used:</b> <c>CreateUserAsync</c>, <c>CreateRoleAsync</c>, <c>GrantRoleAsync</c>,
/// <c>GrantPrivilegeAsync</c>, <c>ListUsersAsync</c>, <c>ListGrantsForRoleAsync</c>,
/// <c>RevokePrivilegeAsync</c>, <c>RevokeRoleAsync</c>, <c>DropRoleAsync</c>, <c>DropUserAsync</c>.</para>
/// <para><b>Expected output:</b> "Users: …" and "Grants for example_role: 1", then "Done.".</para>
/// </remarks>
public static class RBACExample
{
    public static async Task Run(string uri)
    {
        // RBAC operations require an authenticated admin connection; the documented docker setup does not
        // enable Milvus auth, so fail fast with a clear message instead of defaulting to the well-known
        // root:Milvus credential (which would fail against an auth-disabled server anyway).
        string? token = Environment.GetEnvironmentVariable("MILVUS_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("RBACExample requires an auth-enabled server. Start Milvus with " +
                "MILVUS_AUTHORIZATION_ENABLED=true and set MILVUS_TOKEN=<user:password> (e.g. root:Milvus).");
            return;
        }

        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri, token);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string userName = "example_user";
        const string roleName = "example_role";
        const string privilegeGroupName = "example_privilege_group";

        // Clean up from previous runs, tolerating "not found".
        Console.WriteLine("Cleaning up previous runs...");
        try { await client.DropUserAsync(new DropUserReq { UserName = userName }); } catch (MilvusException) { }
        try { await client.DropRoleAsync(new DropRoleReq { RoleName = roleName }); } catch (MilvusException) { }
        try { await client.DropPrivilegeGroupAsync(new DropPrivilegeGroupReq { GroupName = privilegeGroupName }); } catch (MilvusException) { }

        Console.WriteLine("Creating user and role...");
        #region Snippet:MilvusRbac_Grant
        await client.CreateUserAsync(new CreateUserReq { UserName = userName, Password = "password" });
        await client.CreateRoleAsync(new CreateRoleReq { RoleName = roleName });

        await client.GrantRoleAsync(new GrantRoleReq { UserName = userName, RoleName = roleName });
        await client.GrantPrivilegeAsync(new GrantPrivilegeReq
        {
            RoleName = roleName,
            Object = "Collection",
            ObjectName = "*",
            Privilege = "Search"
        });
        #endregion

        Console.WriteLine("Updating password...");
        await client.UpdatePasswordAsync(new UpdatePasswordReq
        {
            UserName = userName,
            OldPassword = "password",
            NewPassword = "new_password"
        });
        Console.WriteLine($"Updated password for {userName}");

        Console.WriteLine("Creating privilege group...");
        await client.CreatePrivilegeGroupAsync(new CreatePrivilegeGroupReq { GroupName = privilegeGroupName });
        await client.AddPrivilegesToGroupAsync(new AddPrivilegesToGroupReq
        {
            GroupName = privilegeGroupName,
            Privileges = new[] { "Search", "Query" }
        });
        Console.WriteLine($"Added Search/Query privileges to privilege group {privilegeGroupName}");

        Console.WriteLine("Listing privilege groups...");
        ListPrivilegeGroupsResp privilegeGroups = await client.ListPrivilegeGroupsAsync(new ListPrivilegeGroupsReq());
        Console.WriteLine($"Privilege groups: {string.Join(", ", privilegeGroups.GroupNames)}");

        Console.WriteLine("Describing role...");
        DescribeRoleResp roleDesc = await client.DescribeRoleAsync(new DescribeRoleReq { RoleName = roleName });
        Console.WriteLine($"Role '{roleDesc.RoleName}' users: {string.Join(", ", roleDesc.Users)}");
        Console.WriteLine($"Role '{roleDesc.RoleName}' grants: {roleDesc.Grants.Count}");

        Console.WriteLine("Describing user...");
        DescribeUserResp userDesc = await client.DescribeUserAsync(new DescribeUserReq { UserName = userName });
        Console.WriteLine($"User '{userDesc.User?.User}' roles: {string.Join(", ", userDesc.User?.Roles ?? Array.Empty<string>())}");

        Console.WriteLine("Listing users...");
        ListUsersResp users = await client.ListUsersAsync(new ListUsersReq());
        Console.WriteLine($"Users: {string.Join(", ", users.Users)}");

        Console.WriteLine("Listing grants...");
        ListGrantsForRoleResp grants = await client.ListGrantsForRoleAsync(new ListGrantsForRoleReq { RoleName = roleName });
        Console.WriteLine($"Grants for {roleName}: {grants.Grants.Count}");

        Console.WriteLine("Revoking privilege...");
        await client.RevokePrivilegeAsync(new RevokePrivilegeReq
        {
            RoleName = roleName,
            Object = "Collection",
            ObjectName = "*",
            Privilege = "Search"
        });
        Console.WriteLine("Revoking role...");
        await client.RevokeRoleAsync(new RevokeRoleReq { UserName = userName, RoleName = roleName });
        Console.WriteLine("Cleaning up...");
        await client.DropRoleAsync(new DropRoleReq { RoleName = roleName });
        await client.DropUserAsync(new DropUserReq { UserName = userName });
        await client.DropPrivilegeGroupAsync(new DropPrivilegeGroupReq { GroupName = privilegeGroupName });

        Console.WriteLine("Done.");
    }
}
