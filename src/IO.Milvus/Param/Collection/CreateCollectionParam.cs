using IO.Milvus.Exception;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IO.Milvus.Utils;
using System;

namespace IO.Milvus.Param.Collection
{
    /// <summary>
    /// Param for <see cref="IO.Milvus.Client.IMilvusClient.CreateCollection(CreateCollectionParam)"/>
    /// </summary>
    public class CreateCollectionParam
    {
        #region Ctor
        public CreateCollectionParam()
        {
        }

        public static CreateCollectionParam Create(
            string collectionName,
            int shardsNum,
            IEnumerable<FieldType> fieldTypes,
            string description = ""
            )
        {
            var param = new CreateCollectionParam()
            {
                CollectionName = collectionName,
                ShardsNum = shardsNum,
                Description = description,
            };

            if (fieldTypes != null)
            {
                param.FieldTypes.AddRange(fieldTypes);
            }

            param.Check();

            return param;
        }

        #endregion

        #region Properties
        public string CollectionName { get; set; }

        public int ShardsNum { get; set; } = 2;

        public string Description { get; set; }

        public List<FieldType> FieldTypes { get; } = new List<FieldType>();
        #endregion

        #region Methods
        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, "Collection name");

            if (ShardsNum <= 0)
            {
                throw new ParamException("ShardNum must be larger than 0");
            }

            if (FieldTypes.IsEmpty())
            {
                throw new ParamException("Field numbers must be larger than 0");
            }

            if (FieldTypes.Any(p => p == null))
            {
                throw new ParamException("Collection field cannot be null");
            }

            if (!FieldTypes.First().IsPrimaryKey || FieldTypes.First().DataType != Grpc.DataType.Int64)
            {
                throw new ParamException("The first filedType's IsPrimaryKey must be true and DataType == Int64");
            }

            FieldTypes.ForEach(p => p.Check());
        }

        /// <summary>
        /// Constructs a <code>String</code> by <see cref="CreateCollectionParam"/> instance.
        /// </summary>
        /// <returns><see cref="string"/></returns>
        public override string ToString()
        {
            return $"{nameof(CreateCollectionParam)}{{{nameof(CollectionName)}=\'{CollectionName}\', {nameof(ShardsNum)}=\'{ShardsNum}\', {nameof(Description)}=\'{Description}\', {nameof(FieldTypes)}=\'{FieldTypes}\', }}";
        }
        #endregion
    }
}
