using Google.Protobuf.Collections;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// Shared conversions between proto and V2 DTOs for the DQL domain (search/query results).
/// </summary>
internal static class DqlConversions
{
    /// <summary>
    /// Converts the proto field data of a search/query result into V2 <see cref="FieldData" /> objects.
    /// </summary>
    public static List<FieldData> ProcessReturnedFieldData(RepeatedField<Grpc.FieldData> grpcFields)
    {
        var results = new List<FieldData>(grpcFields.Count);
        foreach (Grpc.FieldData grpcField in grpcFields)
        {
            if (grpcField.IsDynamic)
            {
                // Dynamic fields are not yet surfaced as V2 FieldData; skip for now.
                continue;
            }

            results.Add(FromGrpcFieldData(grpcField));
        }

        return results;
    }

    private static FieldData FromGrpcFieldData(Grpc.FieldData fieldData)
    {
        switch (fieldData.FieldCase)
        {
            case Grpc.FieldData.FieldOneofCase.Vectors:
                return ConvertVectors(fieldData);

            case Grpc.FieldData.FieldOneofCase.Scalars:
                return fieldData.Scalars.DataCase switch
                {
                    Grpc.ScalarField.DataOneofCase.BoolData => FieldData.Create(fieldData.FieldName, fieldData.Scalars.BoolData.Data),
                    Grpc.ScalarField.DataOneofCase.IntData => FieldData.Create(fieldData.FieldName, fieldData.Scalars.IntData.Data.Select(x => (long)x).ToList()),
                    Grpc.ScalarField.DataOneofCase.LongData => FieldData.Create(fieldData.FieldName, fieldData.Scalars.LongData.Data),
                    Grpc.ScalarField.DataOneofCase.FloatData => FieldData.Create(fieldData.FieldName, fieldData.Scalars.FloatData.Data),
                    Grpc.ScalarField.DataOneofCase.DoubleData => FieldData.Create(fieldData.FieldName, fieldData.Scalars.DoubleData.Data),
                    Grpc.ScalarField.DataOneofCase.StringData => FieldData.CreateVarChar(fieldData.FieldName, fieldData.Scalars.StringData.Data),
                    Grpc.ScalarField.DataOneofCase.JsonData => FieldData.CreateJson(
                        fieldData.FieldName, fieldData.Scalars.JsonData.Data.Select(p => p.ToStringUtf8()).ToList()),
                    Grpc.ScalarField.DataOneofCase.ArrayData => ConvertArray(fieldData),
                    _ => throw new NotSupportedException($"{fieldData.Scalars.DataCase} not supported")
                };

            default:
                throw new NotSupportedException($"{fieldData.FieldCase} not supported");
        }
    }

    private static FieldData ConvertVectors(Grpc.FieldData fieldData)
    {
        Grpc.VectorField vectors = fieldData.Vectors;
        return vectors.DataCase switch
        {
            Grpc.VectorField.DataOneofCase.FloatVector
                => FieldData.CreateFloatVector(fieldData.FieldName, ChunkFloats(vectors.FloatVector.Data, (int)vectors.Dim)),

            Grpc.VectorField.DataOneofCase.Float16Vector => ConvertFloat16Vectors(fieldData, vectors),

            Grpc.VectorField.DataOneofCase.Bfloat16Vector => ConvertBFloat16Vectors(fieldData, vectors),

            Grpc.VectorField.DataOneofCase.Int8Vector => ConvertInt8Vectors(fieldData, vectors),

            Grpc.VectorField.DataOneofCase.BinaryVector => ConvertBinaryVectors(fieldData, vectors),

            Grpc.VectorField.DataOneofCase.SparseFloatVector => ConvertSparseVectors(fieldData, vectors),

            _ => throw new NotSupportedException($"VectorField.DataOneofCase.{vectors.DataCase} not supported")
        };
    }

    private static ReadOnlyMemory<float>[] ChunkFloats(RepeatedField<float> data, int dim)
    {
        int vectorCount = data.Count / dim;
        var vectors = new ReadOnlyMemory<float>[vectorCount];
        for (int i = 0; i < vectorCount; i++)
        {
            var vector = new float[dim];
            for (int j = 0; j < dim; j++)
            {
                vector[j] = data[i * dim + j];
            }
            vectors[i] = vector;
        }

        return vectors;
    }

    private static BinaryVectorFieldData ConvertBinaryVectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        int dim = (int)vectors.Dim;
        int bytesPerVector = dim / 8;
        byte[] raw = vectors.BinaryVector.ToByteArray();
        var rows = new ReadOnlyMemory<byte>[raw.Length / bytesPerVector];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = raw.AsMemory(i * bytesPerVector, bytesPerVector);
        }

        return FieldData.CreateBinaryVectors(fieldData.FieldName, rows);
    }

    private static Float16VectorFieldData ConvertFloat16Vectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        int dim = (int)vectors.Dim;
        byte[] raw = vectors.Float16Vector.ToByteArray();
        var rows = new ReadOnlyMemory<ushort>[raw.Length / (dim * 2)];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new ushort[dim];
            int offset = i * dim * 2;
            for (int j = 0; j < dim; j++)
            {
                row[j] = (ushort)(raw[offset + j * 2] | (raw[offset + j * 2 + 1] << 8));
            }

            rows[i] = row;
        }

        return new Float16VectorFieldData(fieldData.FieldName, rows);
    }

    private static BFloat16VectorFieldData ConvertBFloat16Vectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        int dim = (int)vectors.Dim;
        byte[] raw = vectors.Bfloat16Vector.ToByteArray();
        var rows = new ReadOnlyMemory<ushort>[raw.Length / (dim * 2)];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new ushort[dim];
            int offset = i * dim * 2;
            for (int j = 0; j < dim; j++)
            {
                row[j] = (ushort)(raw[offset + j * 2] | (raw[offset + j * 2 + 1] << 8));
            }

            rows[i] = row;
        }

        return new BFloat16VectorFieldData(fieldData.FieldName, rows);
    }

    private static Int8VectorFieldData ConvertInt8Vectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        int dim = (int)vectors.Dim;
        byte[] raw = vectors.Int8Vector.ToByteArray();
        var rows = new ReadOnlyMemory<sbyte>[raw.Length / dim];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new sbyte[dim];
            for (int j = 0; j < dim; j++)
            {
                row[j] = unchecked((sbyte)raw[i * dim + j]);
            }

            rows[i] = row;
        }

        return new Int8VectorFieldData(fieldData.FieldName, rows);
    }

    private static SparseFloatVectorFieldData ConvertSparseVectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        var sparseVectors = new MilvusSparseVector<float>[vectors.SparseFloatVector.Contents.Count];
        for (int i = 0; i < sparseVectors.Length; i++)
        {
            sparseVectors[i] = MilvusSparseVector<float>.FromBytes(vectors.SparseFloatVector.Contents[i].Span);
        }

        return FieldData.CreateSparseFloatVector(fieldData.FieldName, sparseVectors);
    }

    private static FieldData ConvertArray(Grpc.FieldData fieldData)
    {
        Grpc.ArrayArray arrayData = fieldData.Scalars.ArrayData;
        return arrayData.ElementType switch
        {
            Grpc.DataType.Bool => new FieldData<IReadOnlyList<bool>>(fieldData.FieldName,
                arrayData.Data.Select(x => (IReadOnlyList<bool>)(x.BoolData?.Data ?? [])).ToList(),
                DataType.Array, false),
            Grpc.DataType.Int8 or Grpc.DataType.Int16 or Grpc.DataType.Int32 => new FieldData<IReadOnlyList<int>>(fieldData.FieldName,
                arrayData.Data.Select(x => (IReadOnlyList<int>)(x.IntData?.Data ?? [])).ToList(),
                DataType.Array, false),
            Grpc.DataType.Int64 => new FieldData<IReadOnlyList<long>>(fieldData.FieldName,
                arrayData.Data.Select(x => (IReadOnlyList<long>)(x.LongData?.Data ?? [])).ToList(),
                DataType.Array, false),
            Grpc.DataType.Float => new FieldData<IReadOnlyList<float>>(fieldData.FieldName,
                arrayData.Data.Select(x => (IReadOnlyList<float>)(x.FloatData?.Data ?? [])).ToList(),
                DataType.Array, false),
            Grpc.DataType.Double => new FieldData<IReadOnlyList<double>>(fieldData.FieldName,
                arrayData.Data.Select(x => (IReadOnlyList<double>)(x.DoubleData?.Data ?? [])).ToList(),
                DataType.Array, false),
            Grpc.DataType.String or Grpc.DataType.VarChar => new FieldData<IReadOnlyList<string>>(fieldData.FieldName,
                arrayData.Data.Select(x => (IReadOnlyList<string>)(x.StringData?.Data ?? [])).ToList(),
                DataType.Array, false),
            _ => throw new NotSupportedException($"Array element type {arrayData.ElementType} not supported")
        };
    }
}
