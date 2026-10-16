# Tutorial 8: Manage users and roles (advanced)

This advanced tutorial demonstrates Milvus RBAC with the C# SDK. It creates a temporary user, role,
and privilege group, grants the role to the user, inspects the resulting permissions, and then
revokes and removes everything it created.

## Prerequisites

- .NET SDK 8.0.x is installed.
- Milvus 2.6 or later is running.
- The connection in `MILVUS_TOKEN` has permission to manage users and roles when authorization is
  enabled.
- `Milvus.Client.V2` version `2.6.0` has been published to nuget.org.

Connection settings use `MILVUS_URI` and `MILVUS_TOKEN`, defaulting to `localhost:19530` and
`root:Milvus`.

## Run

```bash
dotnet run --project tutorial/8_rbac
```

The tutorial uses unique names so it can be run repeatedly. On normal completion it attempts every
teardown step for its user, role, grants, and privilege group. If any cleanup operation fails, it
reports all collected cleanup errors and exits unsuccessfully rather than silently leaving
server-level RBAC resources behind.

This tutorial does not require the repository's standalone server to enable
`common.security.authorizationEnabled=true`. With authorization disabled, Milvus may accept the
RBAC management calls, but it does not enforce the temporary user's `Query`/`Search` permissions;
the example therefore cannot validate that an unauthorized insert is rejected. To test actual
permission enforcement, run the same tutorial against a separately configured authorization-enabled
Milvus instance and provide an administrative `MILVUS_TOKEN`.

## Expected output

Names are unique, but the run should include messages like:

```text
Calling CreateUserAsync: create u...
CreateUserAsync completed
Calling CreateRoleAsync: create csharp_tutorial_role_...
CreateRoleAsync completed
Calling CreatePrivilegeGroupAsync: create csharp_tutorial_group_...
CreatePrivilegeGroupAsync completed
Calling AddPrivilegesToGroupAsync: add Query and Search to csharp_tutorial_group_...
AddPrivilegesToGroupAsync completed
Calling GrantPrivilegeAsync: attach csharp_tutorial_group_... to csharp_tutorial_role_...
GrantPrivilegeAsync completed
Calling GrantRoleAsync: assign csharp_tutorial_role_... to u...
GrantRoleAsync completed
...
```
