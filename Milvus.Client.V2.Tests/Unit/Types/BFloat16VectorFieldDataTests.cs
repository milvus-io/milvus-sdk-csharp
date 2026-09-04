using Xunit;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class BFloat16VectorFieldDataTests
{
    [Fact]
    public void BFloat16VectorFieldData_encodes_rows_as_little_endian_bytes()
    {
        var field = FieldData.CreateBFloat16Vector("vec", new[]
        {
            new ReadOnlyMemory<ushort>(new ushort[] { 0x3F80, 0xC000 }),
            new ReadOnlyMemory<ushort>(new ushort[] { 0x4000, 0xBF80 })
        });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(Grpc.DataType.Bfloat16Vector, grpc.Type);
        Assert.Equal(2, grpc.Vectors.Dim);

        byte[] bytes = grpc.Vectors.Bfloat16Vector.ToByteArray();
        // Row 0: 0x3F80 -> [0x80, 0x3F], 0xC000 -> [0x00, 0xC0]
        Assert.Equal(new byte[] { 0x80, 0x3F, 0x00, 0xC0, 0x00, 0x40, 0x80, 0xBF }, bytes);
    }

    [Fact]
    public void DqlConversions_roundtrips_bfloat16_vector()
    {
        ushort[][] rows =
        {
            new ushort[] { 0x3F80, 0xC000, 0x4000 },
            new ushort[] { 0xBF80, 0x3F00, 0x4040 }
        };

        var field = FieldData.CreateBFloat16Vector("vec", rows.Select(r => new ReadOnlyMemory<ushort>(r)).ToArray());

        Grpc.FieldData grpc = field.ToGrpcFieldData();
        Grpc.SearchResults searchResults = new()
        {
            Results = new Grpc.SearchResultData { FieldsData = { grpc } }
        };
        Grpc.QueryResults queryResults = new() { FieldsData = { grpc } };

        // Both the search and query conversion paths should decode the bfloat16 rows.
        IReadOnlyList<FieldData> viaSearch = DqlConversions.ProcessReturnedFieldData(searchResults.Results.FieldsData);
        IReadOnlyList<FieldData> viaQuery = DqlConversions.ProcessReturnedFieldData(queryResults.FieldsData);

        foreach (IReadOnlyList<FieldData> fields in new[] { viaSearch, viaQuery })
        {
            BFloat16VectorFieldData decoded = Assert.IsType<BFloat16VectorFieldData>(Assert.Single(fields));
            Assert.Equal(2, decoded.RowCount);
            for (int i = 0; i < rows.Length; i++)
            {
                Assert.Equal(rows[i], decoded.Data[i].ToArray());
            }
        }
    }

    [Fact]
    public void CreateFloat16Vector_produces_float16_encoding()
    {
        var field = FieldData.CreateFloat16Vector("vec", new[]
        {
            new ReadOnlyMemory<ushort>(new ushort[] { 0x3C00, 0xBC00 })
        });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(Grpc.DataType.Float16Vector, grpc.Type);
        Assert.Equal(2, grpc.Vectors.Dim);
        Assert.Equal(new byte[] { 0x00, 0x3C, 0x00, 0xBC }, grpc.Vectors.Float16Vector.ToByteArray());
    }
}
