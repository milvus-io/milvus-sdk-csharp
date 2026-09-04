using Xunit;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class DqlConversionsTests
{
    [Fact]
    public void ProcessReturnedFieldData_reconstructs_null_rows_from_valid_data()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "age",
            Type = Grpc.DataType.Int64
        };
        field.Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray() };
        field.Scalars.LongData.Data.Add(25);
        field.Scalars.LongData.Data.Add(40);
        field.ValidData.Add(true);
        field.ValidData.Add(false);
        field.ValidData.Add(true);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        FieldData<long?> age = Assert.IsType<FieldData<long?>>(decoded[0]);
        Assert.Equal(3, age.RowCount);
        Assert.Equal(25L, age.Data[0]);
        Assert.Null(age.Data[1]);
        Assert.Equal(40L, age.Data[2]);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_positional_scalar_with_null_rows()
    {
        // Real servers send scalar columns positionally: one data slot per row (zero-filled for null rows)
        // plus a same-length valid_data mask. validData.Count == data.Count selects the positional branch.
        var field = new Grpc.FieldData
        {
            FieldName = "age",
            Type = Grpc.DataType.Int64
        };
        field.Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray() };
        field.Scalars.LongData.Data.Add(25);
        field.Scalars.LongData.Data.Add(0);   // null row, zero-filled
        field.Scalars.LongData.Data.Add(40);
        field.ValidData.Add(true);
        field.ValidData.Add(false);
        field.ValidData.Add(true);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        FieldData<long?> age = Assert.IsType<FieldData<long?>>(decoded[0]);
        Assert.Equal(3, age.RowCount);
        // Positional masking: each row's own slot is kept for valid rows, null substituted for invalid ones.
        Assert.Equal(25L, age.Data[0]);
        Assert.Null(age.Data[1]);
        Assert.Equal(40L, age.Data[2]);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_strings_with_null_rows()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "name",
            Type = Grpc.DataType.VarChar
        };
        field.Scalars = new Grpc.ScalarField { StringData = new Grpc.StringArray() };
        field.Scalars.StringData.Data.Add("alice");
        field.Scalars.StringData.Data.Add("carol");
        field.ValidData.Add(true);
        field.ValidData.Add(false);
        field.ValidData.Add(true);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        FieldData<string> name = Assert.IsType<FieldData<string>>(decoded[0]);
        Assert.Equal(3, name.RowCount);
        Assert.Equal("alice", name.Data[0]);
        Assert.Null(name.Data[1]);
        Assert.Equal("carol", name.Data[2]);
    }

    [Theory]
    [InlineData(Milvus.Client.V2.Types.DataType.Int8)]
    [InlineData(Milvus.Client.V2.Types.DataType.Int16)]
    [InlineData(Milvus.Client.V2.Types.DataType.Int32)]
    public void ProcessReturnedFieldData_preserves_int_width(Milvus.Client.V2.Types.DataType type)
    {
        var field = new Grpc.FieldData
        {
            FieldName = "count",
            Type = (Grpc.DataType)(int)type
        };
        field.Scalars = new Grpc.ScalarField { IntData = new Grpc.IntArray() };
        field.Scalars.IntData.Data.Add(1);
        field.Scalars.IntData.Data.Add(2);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        FieldData decodedField = decoded[0];
        Assert.Equal(type, decodedField.DataType);
    }

    [Fact]
    public void ProcessReturnedFieldData_reconstructs_null_array_rows_from_valid_data()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "tags",
            Type = Grpc.DataType.Array
        };
        field.Scalars = new Grpc.ScalarField { ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.VarChar } };
        var row0 = new Grpc.ScalarField { StringData = new Grpc.StringArray() };
        row0.StringData.Data.Add("a");
        row0.StringData.Data.Add("b");
        var row2 = new Grpc.ScalarField { StringData = new Grpc.StringArray() };
        row2.StringData.Data.Add("c");
        field.Scalars.ArrayData.Data.Add(row0);
        field.Scalars.ArrayData.Data.Add(row2);
        field.ValidData.Add(true);
        field.ValidData.Add(false);
        field.ValidData.Add(true);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        ArrayFieldData<string> tags = Assert.IsType<ArrayFieldData<string>>(decoded[0]);
        Assert.Equal(3, tags.RowCount);
        Assert.Equal(new[] { "a", "b" }, tags.Data[0]);
        Assert.Null(tags.Data[1]);
        Assert.Equal(new[] { "c" }, tags.Data[2]);
    }

    [Theory]
    [InlineData(DataType.Int8, typeof(ArrayFieldData<sbyte>))]
    [InlineData(DataType.Int16, typeof(ArrayFieldData<short>))]
    [InlineData(DataType.Int32, typeof(ArrayFieldData<int>))]
    public void ProcessReturnedFieldData_preserves_array_element_width(DataType elementType, Type expectedType)
    {
        var field = new Grpc.FieldData
        {
            FieldName = "nums",
            Type = Grpc.DataType.Array,
            Scalars = new Grpc.ScalarField
            {
                ArrayData = new Grpc.ArrayArray { ElementType = (Grpc.DataType)(int)elementType }
            }
        };
        var row = new Grpc.ScalarField { IntData = new Grpc.IntArray() };
        row.IntData.Data.Add(1);
        field.Scalars.ArrayData.Data.Add(row);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        Assert.IsType(expectedType, decoded[0]);
    }

    [Fact]
    public void ProcessReturnedFieldData_struct_vector_degrades_when_dimension_unset()
    {
        // A struct vector sub-field whose dim is unset (0) must degrade to an empty row instead of throwing
        // DivideByZeroException in the chunking helpers.
        var structField = new Grpc.FieldData
        {
            FieldName = "st",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var vecSub = new Grpc.FieldData { FieldName = "vec", Type = Grpc.DataType.ArrayOfVector };
        vecSub.Vectors = new Grpc.VectorField();
        vecSub.Vectors.VectorArray = new Grpc.VectorArray { ElementType = Grpc.DataType.Float16Vector };
        var packed = new Grpc.VectorField { Dim = 2 };
        packed.Float16Vector = Google.Protobuf.ByteString.CopyFrom(new byte[] { 0x00, 0x3C, 0x00, 0x40 });
        vecSub.Vectors.VectorArray.Data.Add(packed);
        structField.StructArrays.Fields.Add(vecSub);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(structField);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        var structData = Assert.IsType<StructFieldData>(decoded[0]);
        Assert.Equal(1, structData.RowCount);
        Assert.Single(structData.Data);
        Assert.Empty(structData.Data[0]!);
    }

    [Fact]
    public void ProcessReturnedFieldData_struct_empty_scalar_row_does_not_throw()
    {
        // A struct scalar sub-field row with no packed data (oneof DataCase None) must decode to an empty row
        // instead of throwing NullReferenceException in the typed accessors.
        var structField = new Grpc.FieldData
        {
            FieldName = "st",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var intSub = new Grpc.FieldData { FieldName = "int32", Type = Grpc.DataType.Array };
        intSub.Scalars = new Grpc.ScalarField { ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.Int32 } };
        // One row with an empty ScalarField (no int_data packed).
        intSub.Scalars.ArrayData.Data.Add(new Grpc.ScalarField());
        structField.StructArrays.Fields.Add(intSub);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(structField);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        var structData = Assert.IsType<StructFieldData>(decoded[0]);
        Assert.Single(structData.Data);
        Assert.Empty(structData.Data[0]!);
    }
}
