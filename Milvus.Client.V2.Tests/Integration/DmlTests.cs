using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Collection;
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
                ColumnsData =
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
    public async Task Insert_forwards_explicit_auto_id_primary_key_values()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 100 } };
        using MilvusClientV2 client = server.CreateClient();

        server.Service.DescribeSchema = new CollectionSchema
        {
            Name = "dml_coll",
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true),
                FieldSchema.CreateFloatVector("embedding", dimension: 2)
            }
        };
        SchemaCache.Instance.Clear();

        // An insert that explicitly provides auto-id primary-key values is forwarded to the server, which
        // decides whether allow_insert_auto_id permits them (matching the C++/Java SDKs).
        await client.InsertAsync(
            new InsertReq
            {
                CollectionName = "dml_coll",
                ColumnsData =
                [
                    FieldData.Create("id", new long[] { 10, 20 }),
                    FieldData.CreateFloatVector("embedding", new[]
                    {
                        new ReadOnlyMemory<float>(new[] { 1f, 2f }),
                        new ReadOnlyMemory<float>(new[] { 3f, 4f })
                    })
                ]
            },
            TestContext.Current.CancellationToken);

        Grpc.InsertRequest insert = Assert.IsType<Grpc.InsertRequest>(server.Service.Requests["Insert"]);
        Assert.Equal(new long[] { 10, 20 }, insert.FieldsData.Single(f => f.FieldName == "id").Scalars.LongData.Data);
    }

    [Fact]
    public async Task Insert_forwards_request_level_database_name()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 100 } };
        using MilvusClientV2 client = server.CreateClient();

        CollectionTsCache.Instance.Clear();
        await client.InsertAsync(
            new InsertReq
            {
                CollectionName = "dml_coll",
                DatabaseName = "other_db",
                ColumnsData =
                [
                    FieldData.Create("id", new long[] { 1, 2 }),
                    FieldData.CreateVarChar("name", new[] { "a", "b" })
                ]
            },
            TestContext.Current.CancellationToken);

        // The insert RPC carries the request-level database.
        Grpc.InsertRequest insert = Assert.IsType<Grpc.InsertRequest>(server.Service.Requests["Insert"]);
        Assert.Equal("other_db", insert.DbName);

        // The schema describe (cache-miss for this db) also targets that database.
        Grpc.DescribeCollectionRequest describe =
            Assert.IsType<Grpc.DescribeCollectionRequest>(server.Service.Requests["DescribeCollection"]);
        Assert.Equal("other_db", describe.DbName);

        // The ts cache is keyed by the resolved (overridden) database, not the client's default.
        Assert.Equal(100L, CollectionTsCache.Instance.Get(server.Uri, "other_db", "dml_coll"));
    }

    [Fact]
    public async Task Insert_stamps_schema_timestamp_from_describe()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 100, DescribeUpdateTimestamp = 4242 } };
        using MilvusClientV2 client = server.CreateClient();

        CollectionTsCache.Instance.Clear();
        SchemaCache.Instance.Clear();

        await client.InsertAsync(
            new InsertReq
            {
                CollectionName = "dml_coll",
                ColumnsData = [FieldData.Create("id", new long[] { 1, 2 })]
            },
            TestContext.Current.CancellationToken);

        // The schema timestamp stamped from the describe's UpdateTimestamp is carried on the wire, so the
        // server can detect a stale schema (the SchemaMismatch retry path).
        Grpc.InsertRequest insert = Assert.IsType<Grpc.InsertRequest>(server.Service.Requests["Insert"]);
        Assert.Equal(4242UL, insert.SchemaTimestamp);
    }

    [Fact]
    public async Task Insert_rejects_wrong_vector_dimension_against_described_schema()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 100 } };
        using MilvusClientV2 client = server.CreateClient();

        server.Service.DescribeSchema = new CollectionSchema
        {
            Name = "dml_coll",
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true),
                new FieldSchema("embedding", DataType.FloatVector) { Dimension = 2 }
            }
        };
        SchemaCache.Instance.Clear();

        // The SDK validates the wire encoding against the described schema client-side (ValidateDataAgainstSchema):
        // a 4-dim vector for a 2-dim field must fail fast with ArgumentException.
        await Assert.ThrowsAsync<ArgumentException>(() => client.InsertAsync(
            new InsertReq
            {
                CollectionName = "dml_coll",
                ColumnsData =
                [
                    FieldData.Create("id", new long[] { 1 }),
                    FieldData.CreateFloatVector("embedding", new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) })
                ]
            },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Insert_forwards_row_data()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 200 } };
        using MilvusClientV2 client = server.CreateClient();

        server.Service.DescribeSchema = new CollectionSchema
        {
            Name = "dml_coll",
            EnableDynamicFields = true,
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true),
                new FieldSchema("name", DataType.VarChar) { MaxLength = 128 },
                new FieldSchema("embedding", DataType.FloatVector) { Dimension = 2 },
                new FieldSchema("score", DataType.Float)
            }
        };

        CollectionTsCache.Instance.Clear();
        SchemaCache.Instance.Clear();
        DescribeCollectionResp desc = await client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = "dml_coll" }, TestContext.Current.CancellationToken);
        Assert.Equal(4, desc.Schema.Fields.Count);

        MutationResp response = await client.InsertAsync(
            new InsertReq
            {
                CollectionName = "dml_coll",
                RowsData =
                [
                    new Dictionary<string, object?>
                    {
                        ["name"] = "alice",
                        ["embedding"] = new[] { 1f, 2f },
                        ["score"] = 9.5f,
                        ["tag"] = "admin"
                    },
                    new Dictionary<string, object?>
                    {
                        ["name"] = "bob",
                        ["embedding"] = new[] { 3f, 4f },
                        ["score"] = 8.0,
                        ["tag"] = "user"
                    }
                ]
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("dml_coll", server.Service.LastInsertedCollection);
        Assert.Equal(2, server.Service.LastInsertedRows);
        Assert.Equal(200UL, response.Timestamp);
        Assert.Equal(200L, CollectionTsCache.Instance.Get(server.Uri, "default", "dml_coll"));
    }

    [Fact]
    public async Task Insert_forwards_struct_rows()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        server.Service.DescribeSchema = new CollectionSchema
        {
            Name = "dml_coll",
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true),
                new FieldSchema("name", DataType.VarChar) { MaxLength = 128 }
            },
            StructFields =
            {
                new StructFieldSchema("st")
                {
                    MaxCapacity = 8,
                    Fields =
                    {
                        new FieldSchema("int32", DataType.Int32),
                        new FieldSchema("vector", DataType.FloatVector) { Dimension = 2 }
                    }
                }
            }
        };

        CollectionTsCache.Instance.Clear();
        SchemaCache.Instance.Clear();

        await client.InsertAsync(
            new InsertReq
            {
                CollectionName = "dml_coll",
                RowsData =
                [
                    new Dictionary<string, object?>
                    {
                        ["name"] = "alice",
                        ["st"] = new object[]
                        {
                            new Dictionary<string, object?> { ["int32"] = 1, ["vector"] = new[] { 1f, 2f } },
                            new Dictionary<string, object?> { ["int32"] = 2, ["vector"] = new[] { 3f, 4f } }
                        }
                    }
                ]
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("dml_coll", server.Service.LastInsertedCollection);
        Assert.Equal(1, server.Service.LastInsertedRows);

        // Verify the nested struct was actually encoded on the wire: the struct column is an ArrayOfStruct
        // FieldData whose sub-fields carry the per-row packed values.
        Assert.NotNull(server.Service.LastInsertedFieldData);
        Milvus.Client.Grpc.FieldData structField = server.Service.LastInsertedFieldData.Single(f => f.FieldName == "st");
        Assert.Equal(Milvus.Client.Grpc.DataType.ArrayOfStruct, structField.Type);
        Assert.Equal(2, structField.StructArrays.Fields.Count);

        Milvus.Client.Grpc.FieldData intSub = structField.StructArrays.Fields[0];
        Assert.Equal(Milvus.Client.Grpc.DataType.Array, intSub.Type);
        Assert.Equal(Milvus.Client.Grpc.DataType.Int32, intSub.Scalars.ArrayData.ElementType);
        Assert.Equal(new[] { 1, 2 }, intSub.Scalars.ArrayData.Data[0].IntData.Data.ToArray());

        Milvus.Client.Grpc.FieldData vectorSub = structField.StructArrays.Fields[1];
        Assert.Equal(Milvus.Client.Grpc.DataType.ArrayOfVector, vectorSub.Type);
        Assert.Equal(new[] { 1f, 2f, 3f, 4f }, vectorSub.Vectors.VectorArray.Data[0].FloatVector.Data.ToArray());
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

    [Fact]
    public async Task Insert_retries_once_on_schema_mismatch_and_invalidates_schema_cache()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 100 } };
        using MilvusClientV2 client = server.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        CollectionTsCache.Instance.Clear();
        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildSchema();

        // Connect explicitly so the lazy connect does not consume the schema-mismatch flag below.
        await client.ConnectAsync(ct);

        // The first Insert RPC fails with SchemaMismatch; the retry succeeds.
        server.Service.FailNextMutationsWithSchemaMismatch = 1;

        MutationResp response = await client.InsertAsync(
            new InsertReq
            {
                CollectionName = "dml_coll",
                ColumnsData = [FieldData.Create("id", new long[] { 1, 2, 3 })]
            },
            ct);
        // Connect + Describe (cache miss) + Insert fail + Describe (cache re-populated after invalidate) + Insert success.
        Assert.Equal(5, server.Service.TotalCalls);
        Assert.Equal(3, response.InsertCount);
        Assert.Equal(100L, CollectionTsCache.Instance.Get(server.Uri, "default", "dml_coll"));
        // The schema cache was invalidated on the mismatch and re-populated by the retry's describe.
        Assert.Equal(1, SchemaCache.Instance.Count);
    }

    [Fact]
    public async Task Insert_propagates_schema_mismatch_when_retry_also_fails()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        CollectionTsCache.Instance.Clear();

        // Connect explicitly so the lazy connect does not consume the schema-mismatch flag below.
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        server.Service.DescribeSchema = BuildSchema();

        // Both the initial call and the single retry fail with SchemaMismatch.
        server.Service.FailNextMutationsWithSchemaMismatch = 2;

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            client.InsertAsync(
                new InsertReq { CollectionName = "dml_coll", ColumnsData = [FieldData.Create("id", new long[] { 1 })] },
                TestContext.Current.CancellationToken));

        Assert.Equal(MilvusErrorCode.SchemaMismatch, exception.ErrorCode);
        // Connect + Describe + Insert + Describe + Insert = exactly one retry, no infinite loop.
        Assert.Equal(5, server.Service.TotalCalls);
    }

    [Fact]
    public async Task Upsert_retries_once_on_schema_mismatch()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        CollectionTsCache.Instance.Clear();

        // Connect explicitly so the lazy connect does not consume the schema-mismatch flag below.
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        server.Service.DescribeSchema = BuildSchema();

        server.Service.FailNextMutationsWithSchemaMismatch = 1;

        MutationResp response = await client.UpsertAsync(
            new UpsertReq
            {
                CollectionName = "dml_coll",
                ColumnsData = [FieldData.Create("id", new long[] { 1, 2, 3 })]
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(5, server.Service.TotalCalls);
        Assert.Equal(3, response.UpsertCount);
    }

    [Fact]
    public async Task Upsert_forwards_data_and_updates_ts_cache()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 200 } };
        using MilvusClientV2 client = server.CreateClient();

        CollectionTsCache.Instance.Clear();
        MutationResp response = await client.UpsertAsync(
            new UpsertReq
            {
                CollectionName = "dml_coll",
                ColumnsData =
                [
                    FieldData.Create("id", new long[] { 1L, 2L }),
                    FieldData.CreateVarChar("name", new[] { "a", "b" })
                ]
            },
            TestContext.Current.CancellationToken);

        var grpcRequest = Assert.IsType<Milvus.Client.Grpc.UpsertRequest>(server.Service.Requests["Upsert"]);
        Assert.Equal("dml_coll", grpcRequest.CollectionName);
        Assert.Equal(2U, grpcRequest.NumRows);
        Assert.Equal(2, grpcRequest.FieldsData.Count);

        Assert.Equal(2, response.UpsertCount);
        Assert.Equal(200UL, response.Timestamp);
        Assert.Equal(200L, CollectionTsCache.Instance.Get(server.Uri, "default", "dml_coll"));
    }

    private static CollectionSchema BuildSchema()
    {
        var schema = new CollectionSchema { Name = "dml_coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        return schema;
    }
}
