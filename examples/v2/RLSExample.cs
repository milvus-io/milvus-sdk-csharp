using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Requests.Rls;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Responses.Rls;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates row-level security (RLS): row policies and principal tags, and how a request that carries an
/// <c>RlsPrincipal</c> is enforced by Milvus (row filtering and write checks). Mirrors java RLSExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show the RLS lifecycle: create/update/list/drop a row policy, set/get/list/delete
/// principal tags, and demonstrate enforcement on insert (check expression) and query (using expression).</para>
/// <para><b>APIs used:</b> <c>CreateRowPolicyAsync</c>, <c>UpdateRowPolicyAsync</c>, <c>ListRowPoliciesAsync</c>,
/// <c>DropRowPolicyAsync</c>, <c>SetRLSPrincipalTagsAsync</c>, <c>GetRLSPrincipalTagsAsync</c>,
/// <c>ListRLSPrincipalsAsync</c>, <c>DeleteRLSPrincipalTagsAsync</c>, plus <c>InsertAsync</c>/<c>QueryAsync</c>
/// with <c>RlsPrincipal</c>/<c>SkipRls</c>.</para>
/// <para><b>Expected output:</b> a "Row policy created" line, the listed/updated policies, the principal tags,
/// and the enforced row counts for alice/bob, then "Done.".</para>
/// <para><b>Notes:</b> RLS requires an auth-enabled server. The collection must be created with
/// <c>rls.enabled=true</c>, the <c>check_expr</c> only applies to write actions (INSERT/DELETE/UPSERT), and the
/// expressions must reference existing fields.</para>
/// </remarks>
public static class RLSExample
{
    public static async Task Run(string uri)
    {
        string? token = Environment.GetEnvironmentVariable("MILVUS_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("RLSExample requires an auth-enabled server. Start Milvus with " +
                "MILVUS_AUTHORIZATION_ENABLED=true and set MILVUS_TOKEN=<user:password> (e.g. root:Milvus).");
            return;
        }

        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri, token);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "csharp_sdk_example_rls_collection";
        const string demoPolicy = "csharp_sdk_example_policy";
        const string alice = "alice";
        const string bob = "bob";

        // Clean up from previous runs, tolerating "not found".
        Console.WriteLine("Cleaning up previous runs...");
        try { await client.DropRowPolicyAsync(new DropRowPolicyReq { CollectionName = collectionName, PolicyName = demoPolicy }); }
        catch (MilvusException) { }
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        Console.WriteLine("Creating collection with rls.enabled=true...");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: 2),
                    FieldSchema.CreateVarchar("team", maxLength: 64)
                }
            },
            Properties = { ["rls.enabled"] = "true" }
        });

        // A policy expression may reference the current principal and its tags:
        //   $current_principal_tags['team']  ->  the 'team' tag of the request principal
        Console.WriteLine("Creating row policy...");
        await client.CreateRowPolicyAsync(new CreateRowPolicyReq
        {
            CollectionName = collectionName,
            PolicyName = demoPolicy,
            PolicyType = RowPolicyType.Permissive,
            Actions = [RowPolicyAction.Search, RowPolicyAction.Query, RowPolicyAction.Insert],
            UsingExpr = "team == $current_principal_tags['team']",
            CheckExpr = "team == $current_principal_tags['team']",
            Description = "A principal only sees or writes rows of its own team"
        });
        Console.WriteLine($"  Created {demoPolicy}");

        ListRowPoliciesResp policies = await client.ListRowPoliciesAsync(new ListRowPoliciesReq { CollectionName = collectionName });
        Console.WriteLine($"  Row policies: {string.Join(", ", policies.Policies.Select(p => p.PolicyName))}");

        Console.WriteLine("Updating row policy...");
        await client.UpdateRowPolicyAsync(new UpdateRowPolicyReq
        {
            CollectionName = collectionName,
            PolicyName = demoPolicy,
            PolicyType = RowPolicyType.Permissive,
            Actions = [RowPolicyAction.Search, RowPolicyAction.Query, RowPolicyAction.Insert, RowPolicyAction.Delete],
            UsingExpr = "team == $current_principal_tags['team']",
            CheckExpr = "team == $current_principal_tags['team']",
            Description = "Updated: a principal sees or writes only rows of its own team"
        });
        Console.WriteLine("  Updated " + demoPolicy);

        Console.WriteLine("Setting principal tags...");
        await client.SetRLSPrincipalTagsAsync(new SetRLSPrincipalTagsReq
        {
            CollectionName = collectionName,
            PrincipalName = alice,
            Tags = { ["team"] = "engineering", ["level"] = 3 }
        });
        await client.SetRLSPrincipalTagsAsync(new SetRLSPrincipalTagsReq
        {
            CollectionName = collectionName,
            PrincipalName = bob,
            Tags = { ["team"] = "marketing" }
        });
        Console.WriteLine($"  {alice}=engineering, {bob}=marketing");

        GetRLSPrincipalTagsResp aliceTags = await client.GetRLSPrincipalTagsAsync(
            new GetRLSPrincipalTagsReq { CollectionName = collectionName, PrincipalName = alice });
        Console.WriteLine($"  Alice tags: {string.Join(", ", aliceTags.Tags.Select(t => $"{t.Key}={t.Value}"))}");

        ListRLSPrincipalsResp principals = await client.ListRLSPrincipalsAsync(new ListRLSPrincipalsReq { CollectionName = collectionName });
        Console.WriteLine($"  Principals with tags: {string.Join(", ", principals.PrincipalNames)}");

        Console.WriteLine("Deleting a principal tag...");
        await client.DeleteRLSPrincipalTagsAsync(new DeleteRLSPrincipalTagsReq
        {
            CollectionName = collectionName,
            PrincipalName = alice,
            TagKeys = ["level"]
        });
        Console.WriteLine("  Deleted alice's 'level' tag");

        // ---- enforcement: writes are checked against the policy ----
        Console.WriteLine("Enforcing RLS on writes...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 1 }),
                FieldData.CreateFloatVector("vector", new[] { new ReadOnlyMemory<float>(new[] { 1.0f, 2.0f }) }),
                FieldData.CreateVarChar("team", new[] { "engineering" })
            ],
            RlsPrincipal = alice
        });
        Console.WriteLine("  Insert engineering row as alice: allowed");

        try
        {
            await client.InsertAsync(new InsertReq
            {
                CollectionName = collectionName,
                ColumnsData =
                [
                    FieldData.Create("id", new long[] { 2 }),
                    FieldData.CreateFloatVector("vector", new[] { new ReadOnlyMemory<float>(new[] { 1.0f, 2.0f }) }),
                    FieldData.CreateVarChar("team", new[] { "marketing" })
                ],
                RlsPrincipal = alice
            });
            Console.WriteLine("  Insert marketing row as alice: unexpected success");
        }
        catch (MilvusException)
        {
            Console.WriteLine("  Insert marketing row as alice: rejected by RLS check expression");
        }

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 3 }),
                FieldData.CreateFloatVector("vector", new[] { new ReadOnlyMemory<float>(new[] { 1.0f, 2.0f }) }),
                FieldData.CreateVarChar("team", new[] { "marketing" })
            ],
            RlsPrincipal = bob
        });
        Console.WriteLine("  Insert marketing row as bob: allowed");

        // Load the collection so query can run.
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("vector", indexType: IndexType.Flat, metricType: MetricType.L2)]
        });
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        // ---- enforcement: reads are filtered by the principal's tags ----
        Console.WriteLine("Enforcing RLS on reads...");
        QueryResp aliceRows = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id > 0",
            RlsPrincipal = alice
        });
        Console.WriteLine($"  Query rows as alice: {aliceRows.RowCount}");

        QueryResp bobRows = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id > 0",
            RlsPrincipal = bob
        });
        Console.WriteLine($"  Query rows as bob: {bobRows.RowCount}");

        QueryResp allRows = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id > 0",
            SkipRls = true
        });
        Console.WriteLine($"  Query rows with SkipRls: {allRows.RowCount}");

        Console.WriteLine("Dropping row policy...");
        await client.DropRowPolicyAsync(new DropRowPolicyReq { CollectionName = collectionName, PolicyName = demoPolicy });
        Console.WriteLine("Done.");
    }
}
