using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace IO.Milvus;

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
    public MilvusVectors CreateBinaryVectors(IList<BinaryVectorField> binaryVectorFields)
    {
        return new MilvusVectors(binaryVectorFields: binaryVectorFields);
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <returns></returns>
    public MilvusVectors CreateFloatVectors(IList<Field<float>> floatFields)
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

    /// <summary>
    /// Ids
    /// </summary>
    [JsonPropertyName("ids")]
    public MilvusVectorIds Ids { get; }

    /// <summary>
    /// Vectors is an array of binary vector divided by given dim. Disabled when IDs is set
    /// </summary>
    [JsonPropertyName("binary_vectors")]
    public IList<Field> VectorFields { get; }

    /// <summary>
    /// Dim of vectors or binary_vectors, not needed when use ids
    /// </summary>
    [JsonPropertyName("dim")]
    public long Dim { get; set; }

    #region Private Methods ===================================================================
    private MilvusVectors(IList<BinaryVectorField> binaryVectorFields)
    {
        this.VectorFields = binaryVectorFields.Cast<Field>().ToList();
        Dim = binaryVectorFields.First().RowCount;
    }

    private MilvusVectors(MilvusVectorIds ids)
    {
        this.Ids = ids;
    }

    private MilvusVectors(IList<Field<float>> floatFields)
    {
        this.VectorFields = floatFields.Cast<Field>().ToList();
        Dim = floatFields.First().RowCount;
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
        public IList<long> Ids { get; }

        /// <summary>
        /// Partition names.
        /// </summary>
        [JsonPropertyName("partition_names")]
        public IList<string> PartitionNames { get; }

        /// <summary>
        /// Create a MivlsuVectorIds.
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

        #region Private =================================================================
        private MilvusVectorIds(
        string collectionName,
        string fieldName,
        IList<long> ids,
        IList<string> partitionNames = null)
        {
            CollectionName = collectionName;
            FieldName = fieldName;
            Ids = ids;
            PartitionNames = partitionNames;
        }
        #endregion
    }
}
