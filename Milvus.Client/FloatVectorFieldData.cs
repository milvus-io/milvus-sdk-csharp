using System.Runtime.InteropServices;
using Google.Protobuf.Collections;

namespace Milvus.Client;

/// <summary>
/// Float vector field
/// </summary>
public sealed class FloatVectorFieldData : FieldData<ReadOnlyMemory<float>>
{
    /// <summary>
    /// Create a float vector field.
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="data">data</param>
    public FloatVectorFieldData(
        string fieldName,
        IReadOnlyList<ReadOnlyMemory<float>> data)
        : base(fieldName, data, MilvusDataType.FloatVector, false)
    {
    }

    /// <summary>
    /// Row count.
    /// </summary>
    public override long RowCount => Data.Count;

    /// <summary>
    /// Convert to grpc field data
    /// </summary>
    /// <returns>Field data</returns>
    /// <exception cref="MilvusException"></exception>
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        Grpc.FloatArray floatArray = new();
        RepeatedField<float> destination = floatArray.Data;

        if (Data.Count == 0)
        {
            throw new MilvusException("The number of vectors must be positive.");
        }

        int dim = Data[0].Length;

        // The gRPC representation of the vector data is a flat list of the elements (the dimension is known and all
        // vectors have the same dimension)
        destination.Capacity = dim * Data.Count;

        foreach (ReadOnlyMemory<float> vector in Data)
        {
            if (vector.Length != dim)
            {
                throw new MilvusException("All vectors must have the same dimensionality.");
            }

            // Special-case optimization for when the vector is an entire array
            if (MemoryMarshal.TryGetArray(vector, out ArraySegment<float> segment) &&
                segment.Offset == 0 &&
                segment.Count == segment.Array!.Length)
            {
                destination.AddRange(segment.Array);
            }
            else
            {
                foreach (float f in vector.Span)
                {
                    destination.Add(f);
                }
            }
        }

        return new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField
            {
                FloatVector = floatArray,
                Dim = dim,
            }
        };
    }

    internal override object GetValueAsObject(int index)
        => throw new NotSupportedException("Dynamic vector fields are not supported");
}
