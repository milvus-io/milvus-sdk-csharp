using Google.Protobuf;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Milvus vectors type.
/// </summary>
public enum MilvusVectorsType
{
    /// <summary>
    /// Float vectors.
    /// </summary>
    FloatVectors,

    /// <summary>
    /// Binary vectors.
    /// </summary>
    BinaryVectors,

    /// <summary>
    /// Ids.
    /// </summary>
    Ids,
}

/// <summary>
/// Vectors when calculating distance.
/// </summary>
public sealed class MilvusVectors
{
    /// <summary>
    /// Create BinaryVectors for calculating distance.
    /// </summary>
    /// <param name="binaryVectorFields"></param>
    /// <returns></returns>
    public static MilvusVectors CreateBinaryVectors(BinaryVectorField binaryVectorFields)
    {
        Verify.NotNull(binaryVectorFields);
        return new MilvusVectors(binaryVectorFields);
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <returns></returns>
    public static MilvusVectors CreateFloatVectors(IList<Field<float>> floatFields)
    {
        Verify.NotNullOrEmpty(floatFields);

        // Flatten all of the fields into a single vector.
        // Every field is expected to have the same dimension.

        int numLists = floatFields.Count;
        int dim = floatFields[0].Data.Count;
        int totalLength = numLists * dim;

        var floats = new float[totalLength];
        int pos = 0;
        for (int i = 0; i < numLists; i++)
        {
            IList<float> list = floatFields[i].Data;
            int listCount = list.Count;
            if (listCount != dim)
            {
                throw new MilvusException("Row count of fields must be equal.");
            }

            list.CopyTo(floats, pos);
            pos += listCount;
        }

        return new MilvusVectors(floats, dim);
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <returns></returns>
    public static MilvusVectors CreateFloatVectors(IList<List<float>> floatFields)
    {
        Verify.NotNullOrEmpty(floatFields);

        // Flatten all of the fields into a single vector.
        // Every field is expected to have the same dimension.

        int numLists = floatFields.Count;
        int dim = floatFields[0].Count;
        int totalLength = numLists * dim;

        var floats = new float[totalLength];
        int pos = 0;
        for (int i = 0; i < numLists; i++)
        {
            List<float> list = floatFields[i];
            if (list.Count != dim)
            {
                throw new MilvusException("Row count of fields must be equal.");
            }

            list.CopyTo(floats, pos);
            pos += list.Count;
        }

        return new MilvusVectors(floats, dim);
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <param name="dim">dimension of float fields.</param>
    /// <returns></returns>
    public static MilvusVectors CreateFloatVectors(IList<float> floatFields, int dim)
    {
        Verify.NotNullOrEmpty(floatFields);
        return new MilvusVectors(floatFields, dim);
    }

    /// <summary>
    /// Create ids for calculating distance.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public static MilvusVectors CreateIds(MilvusVectorIds ids)
    {
        Verify.NotNull(ids);

        return new MilvusVectors(ids);
    }

    /// <summary>
    /// Create a id array for calculating distance.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="ids">Ids</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <returns></returns>
    public static MilvusVectors CreateIds(
        string collectionName,
        string fieldName,
        IList<long> ids,
        IList<string> partitionNames = null)
    {
        return new MilvusVectors(ids: MilvusVectorIds.Create(collectionName, fieldName, ids, partitionNames));
    }

    internal Grpc.VectorsArray ToVectorsArray()
    {
        Grpc.VectorsArray vectorArray = new();

        if (MilvusVectorsType == MilvusVectorsType.FloatVectors)
        {
            vectorArray.DataArray = new Grpc.VectorField()
            {
                FloatVector = new Grpc.FloatArray(),
                Dim = Dim
            };
            vectorArray.DataArray.FloatVector.Data.AddRange(Vectors);
        }
        else if (MilvusVectorsType == MilvusVectorsType.BinaryVectors)
        {
            vectorArray.DataArray = new Grpc.VectorField()
            {
                BinaryVector = BinaryVectorField.ToGrpcFieldData().ToByteString(),
                Dim = Dim
            };
        }
        else if (MilvusVectorsType == MilvusVectorsType.Ids)
        {
            Grpc.VectorIDs grpcIds = Ids.ToGrpcIds();
            vectorArray.IdArray = grpcIds;
        }

        return vectorArray;
    }

    /// <summary>
    /// Ids
    /// </summary>
    public MilvusVectorIds Ids { get; }

    /// <summary>
    /// Vectors is an array of binary vector divided by given dim. Disabled when IDs is set.
    /// </summary>
    public IList<float> Vectors { get; }

    /// <summary>
    /// Binary vector field.
    /// </summary>
    public BinaryVectorField BinaryVectorField { get; }

    /// <summary>
    /// Milvus vectors type.
    /// </summary>
    public MilvusVectorsType MilvusVectorsType { get; }

    /// <summary>
    /// Dim of vectors or binary_vectors, not needed when use ids
    /// </summary>
    public long Dim { get; set; }

    #region Private Methods ===================================================================
    private MilvusVectors(BinaryVectorField binaryVectorField)
    {
        MilvusVectorsType = MilvusVectorsType.BinaryVectors;
        BinaryVectorField = binaryVectorField;
        Dim = binaryVectorField.RowCount;
    }

    private MilvusVectors(MilvusVectorIds ids)
    {
        MilvusVectorsType = MilvusVectorsType.Ids;
        Ids = ids;
        Dim = ids.Dim;
    }

    private MilvusVectors(IList<float> floatFields, int dim)
    {
        MilvusVectorsType = MilvusVectorsType.FloatVectors;
        Dim = dim;
        Vectors = floatFields;
    }
    #endregion
}

/// <summary>
/// Vector when calculating distance.
/// </summary>
public sealed class MilvusVectorIds
{
    /// <summary>
    /// Collection name.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// Field name.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Ids.
    /// </summary>
    public IList<long> IntIds { get; }

    /// <summary>
    /// String Ids.
    /// </summary>
    public IList<string> StringIds { get; }

    /// <summary>
    /// Ids.
    /// </summary>
    public object IdArray
    {
        get
        {
            if (IntIds != null) { return IntIds; }
            else { return StringIds; }
        }
    }

    /// <summary>
    /// Partition names.
    /// </summary>
    public IList<string> PartitionNames { get; }

    /// <summary>
    /// Dimension of ids.
    /// </summary>
    public long Dim => IntIds?.Count ?? StringIds?.Count ?? 0;

    /// <summary>
    /// Create a long MilvusVectorIds.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="ids">IDs</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <returns></returns>
    public static MilvusVectorIds Create(
        string collectionName,
        string fieldName,
        IList<long> ids,
        IList<string> partitionNames = null)
    {
        return new MilvusVectorIds(collectionName, fieldName, ids, partitionNames);
    }

    /// <summary>
    /// Create a string MilvusVectorIds.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="ids">IDs</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <returns></returns>
    public static MilvusVectorIds Create(
        string collectionName,
        string fieldName,
        IList<string> ids,
        IList<string> partitionNames = null)
    {
        return new MilvusVectorIds(collectionName, fieldName, ids, partitionNames);
    }

    internal Grpc.VectorIDs ToGrpcIds()
    {
        Grpc.VectorIDs idArray = new()
        {
            FieldName = FieldName,
            CollectionName = CollectionName,
        };

        Grpc.IDs ids = new();

        if (IntIds != null)
        {
            ids.IntId = new Grpc.LongArray();
            ids.IntId.Data.AddRange(IntIds);
        }
        else if (StringIds != null)
        {
            ids.StrId = new Grpc.StringArray();
            ids.StrId.Data.AddRange(StringIds);
        }

        idArray.IdArray = ids;

        return idArray;
    }

    #region Private =================================================================
    private MilvusVectorIds(
        string collectionName,
        string fieldName,
        IList<long> ids,
        IList<string> partitionNames = null)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        IntIds = ids;
        PartitionNames = partitionNames;
    }

    private MilvusVectorIds(
        string collectionName,
        string fieldName,
        IList<string> ids,
        IList<string> partitionNames = null)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        StringIds = ids;
        PartitionNames = partitionNames;
    }
    #endregion
}
