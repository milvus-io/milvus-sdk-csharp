namespace Milvus.Client;

/// <summary>
/// Sparse float vector field data.
/// </summary>
public sealed class SparseFloatVectorFieldData : FieldData<MilvusSparseVector<float>>
{
    /// <summary>
    /// Creates a sparse float vector field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="data">The sparse vector data.</param>
    public SparseFloatVectorFieldData(
        string fieldName,
        IReadOnlyList<MilvusSparseVector<float>> data)
        : base(fieldName, data, MilvusDataType.SparseFloatVector, isDynamic: false)
    {
    }

    /// <summary>
    /// Row count.
    /// </summary>
    public override long RowCount => Data.Count;

    /// <summary>
    /// Converts to gRPC field data.
    /// </summary>
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        if (Data.Count == 0)
        {
            throw new MilvusException("The number of vectors must be positive.");
        }

        Grpc.SparseFloatArray sparseArray = new();

        long maxDim = 0;
        foreach (MilvusSparseVector<float> vector in Data)
        {
            sparseArray.Contents.Add(ByteString.CopyFrom(vector.ToBytes()));
            if (vector.MaxIndex >= 0)
            {
                maxDim = Math.Max(maxDim, vector.MaxIndex + 1);
            }
        }

        sparseArray.Dim = maxDim;

        return new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField
            {
                SparseFloatVector = sparseArray,
                Dim = maxDim
            }
        };
    }

    internal override object GetValueAsObject(int index)
        => throw new NotSupportedException("Dynamic sparse vector fields are not supported");
}
