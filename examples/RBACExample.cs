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
        // RBAC grants require an authenticated admin connection; default to the Milvus root account.
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri, "root:Milvus");
        await client.ConnectAsync();

        const string userName = "example_user";
        const string roleName = "example_role";

        // Clean up from previous runs, tolerating "not found".
        try { await client.DropUserAsync(new DropUserReq { UserName = userName }); } catch (MilvusException) { }
        try { await client.DropRoleAsync(new DropRoleReq { RoleName = roleName }); } catch (MilvusException) { }

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

        ListUsersResp users = await client.ListUsersAsync(new ListUsersReq());
        Console.WriteLine($"Users: {string.Join(", ", users.Users)}");

        ListGrantsForRoleResp grants = await client.ListGrantsForRoleAsync(new ListGrantsForRoleReq { RoleName = roleName });
        Console.WriteLine($"Grants for {roleName}: {grants.Grants.Count}");

        await client.RevokePrivilegeAsync(new RevokePrivilegeReq
        {
            RoleName = roleName,
            Object = "Collection",
            ObjectName = "*",
            Privilege = "Search"
        });
        await client.RevokeRoleAsync(new RevokeRoleReq { UserName = userName, RoleName = roleName });
        await client.DropRoleAsync(new DropRoleReq { RoleName = roleName });
        await client.DropUserAsync(new DropUserReq { UserName = userName });

        Console.WriteLine("Done.");
    }
}
