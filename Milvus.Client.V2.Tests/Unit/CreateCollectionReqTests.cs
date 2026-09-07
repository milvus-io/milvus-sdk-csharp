using Xunit;

using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit;

[Trait("Category", "Unit")]
public class CreateCollectionReqTests
{
    [Fact]
    public void ToGrpc_maps_schema_fields_and_type_params()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            ConsistencyLevel = ConsistencyLevel.Strong,
            ShardsNum = 2,
            Schema = new CollectionSchema
            {
                Name = "book",
                Description = "books",
                EnableDynamicFields = true,
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("title", maxLength: 100),
                    FieldSchema.CreateFloatVector("embedding", dimension: 4)
                }
            }
        };

        Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal((int)ConsistencyLevel.Strong, (int)grpc.ConsistencyLevel);
        Assert.Equal(2, grpc.ShardsNum);

        Grpc.CollectionSchema schema = Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);
        Assert.Equal("book", schema.Name);
        Assert.Equal("books", schema.Description);
        Assert.True(schema.EnableDynamicField);
        Assert.Equal(3, schema.Fields.Count);

        Grpc.FieldSchema title = schema.Fields.Single(f => f.Name == "title");
        Assert.Equal((int)DataType.VarChar, (int)title.DataType);
        Assert.Equal("100", title.TypeParams.Single(p => p.Key == "max_length").Value);

        Grpc.FieldSchema vector = schema.Fields.Single(f => f.Name == "embedding");
        Assert.Equal((int)DataType.FloatVector, (int)vector.DataType);
        Assert.Equal("4", vector.TypeParams.Single(p => p.Key == "dim").Value);
    }

    [Fact]
    public void ToGrpc_throws_when_collection_name_blank()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = " ",
            Schema = new CollectionSchema { Fields = { new FieldSchema("id", DataType.Int64) } }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateCollectionRequest());
    }

    [Fact]
    public void ToGrpc_throws_when_schema_missing()
    {
        var request = new CreateCollectionReq { CollectionName = "book" };

        Assert.Throws<ArgumentNullException>(() => request.ToGrpcCreateCollectionRequest());
    }
}
