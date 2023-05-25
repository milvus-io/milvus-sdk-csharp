using Google.Protobuf;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
public class MilvusVectors
{
    /// <summary>
    /// Create BinaryVectors for calculating distance.
    /// </summary>
    /// <param name="binaryVectorFields"></param>
    /// <returns></returns>
    public MilvusVectors CreateBinaryVectors(BinaryVectorField binaryVectorFields)
    {
        return new MilvusVectors(binaryVectorFields);
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <returns></returns>
    public MilvusVectors CreateFloatVectors(IList<Field<float>> floatFields)
    {
        return new MilvusVectors(floatFields.Select(p => p.Data.ToList()).ToList());
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <returns></returns>
    public MilvusVectors CreateFloatVectors(IList<List<float>> floatFields)
    {
        return new MilvusVectors(floatFields);
    }

    /// <summary>
    /// Create ids for calculating distance.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public MilvusVectors CreateIds(MilvusVectorIds ids)
    {
        return new MilvusVectors(ids: ids);
    }

    internal Grpc.VectorsArray ToVectorsArray()
    {
        var vectorArray = new Grpc.VectorsArray();

        if (MilvusVectorsType  == MilvusVectorsType.FloatVectors)
        {
            var (floatArray,dim)= this.Vectors.ToFloatArray();

            vectorArray.DataArray = new Grpc.VectorField() 
            { 
                FloatVector = floatArray,
                Dim = dim
            };
        }else if(MilvusVectorsType == MilvusVectorsType.BinaryVectors)
        {
            vectorArray.DataArray = new Grpc.VectorField()
            {
                BinaryVector = this.BinaryVectorField.ToGrpcFieldData().ToByteString(),
                Dim = this.Dim
            };
        }else if(MilvusVectorsType == MilvusVectorsType.Ids)
        {
            var grpcIds = Ids.ToGrpcIds();
            vectorArray.IdArray = grpcIds;
        }

        return vectorArray;
    }

    /// <summary>
    /// Ids
    /// </summary>
    [JsonPropertyName("ids")]
    public MilvusVectorIds Ids { get; }

    /// <summary>
    /// Vectors is an array of binary vector divided by given dim. Disabled when IDs is set.
    /// </summary>
    [JsonPropertyName("vectors")]
    public IList<List<float>> Vectors { get; }

    /// <summary>
    /// Binary vector field.
    /// </summary>
    [JsonPropertyName("binary_vectors")]
    public BinaryVectorField BinaryVectorField { get; }

    /// <summary>
    /// Mivlus vectos type.
    /// </summary>
    [JsonIgnore]
    public MilvusVectorsType MilvusVectorsType { get; }

    /// <summary>
    /// Dim of vectors or binary_vectors, not needed when use ids
    /// </summary>
    [JsonPropertyName("dim")]
    public long Dim { get; set; }

    #region Private Methods ===================================================================
    private MilvusVectors(BinaryVectorField binaryVectorField)
    {
        MilvusVectorsType = MilvusVectorsType.BinaryVectors;
        this.BinaryVectorField = binaryVectorField;
        Dim = binaryVectorField.RowCount;
    }

    private MilvusVectors(MilvusVectorIds ids)
    {
        MilvusVectorsType = MilvusVectorsType.Ids;
        this.Ids = ids;
    }

    private MilvusVectors(IList<List<float>> floatFields)
    {
        MilvusVectorsType = MilvusVectorsType.FloatVectors;
        this.Vectors = floatFields;
        Dim = floatFields.First().Count;
    }
    #endregion

    /// <summary>
    /// Vector when calculating distance.
    /// </summary>
    public class MilvusVectorIds
    {
        /// <summary>
        /// Collection name.
        /// </summary>
        [JsonPropertyName("collection_name")]
        public string CollectionName { get; }

        /// <summary>
        /// Field name.
        /// </summary>
        [JsonPropertyName("field_name")]
        public string FieldName { get; }

        /// <summary>
        /// Ids.
        /// </summary>
        [JsonPropertyName("id_array")]
        public IList<long> IntIds { get; }

        /// <summary>
        /// String Ids.
        /// </summary>
        public IList<string> StringIds { get; }

        /// <summary>
        /// Partition names.
        /// </summary>
        [JsonPropertyName("partition_names")]
        public IList<string> PartitionNames { get; }

        /// <summary>
        /// Create a long MivlsuVectorIds.
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
        /// Create a string MivlsuVectorIds.
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
            var idArray = new Grpc.VectorIDs()
            {
                FieldName = this.FieldName,
                CollectionName = this.CollectionName,
            };

            var ids = new Grpc.IDs();

            if (IntIds != null)
            {
                ids.IntId = new Grpc.LongArray();
                ids.IntId.Data.AddRange(IntIds);
            }else if (StringIds != null)
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
}
