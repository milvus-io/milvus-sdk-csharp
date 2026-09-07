using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class DmlTests
{
    [Fact]
    public async Task Insert_forwards_data_and_updates_ts_cache()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 100 } };
        using MilvusClientV2 client = server.CreateClient();

        CollectionTsCache.Instance.Clear();
        MutationResp response = await client.InsertAsync(
            new InsertReq
            {
                CollectionName = "dml_coll",
                Data =
                [
                    FieldData.Create("id", new long[] { 1, 2, 3 }),
                    FieldData.CreateVarChar("name", new[] { "a", "b", "c" })
                ]
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("dml_coll", server.Service.LastInsertedCollection);
        Assert.Equal(3, server.Service.LastInsertedRows);
        Assert.Equal(3, response.InsertCount);
        Assert.Equal(100UL, response.Timestamp);

        // The ts cache should be populated for Session consistency.
        Assert.Equal(100L, CollectionTsCache.Instance.Get(server.Uri, "default", "dml_coll"));
    }

    [Fact]
    public async Task Delete_forwards_expression()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DeleteAsync(
            new DeleteReq { CollectionName = "coll", Expression = "id in [1, 2]" },
            TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastDeletedCollection);
        Assert.Equal("id in [1, 2]", server.Service.LastDeleteExpression);
    }
}
