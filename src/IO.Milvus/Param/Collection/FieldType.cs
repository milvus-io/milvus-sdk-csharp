using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace IO.Milvus.Param.Collection
{
    /// <summary>
    /// Parameters for a collection field.
    /// <see cref="CreateCollectionParam"/>
    /// </summary>
    public class FieldType
    {
        private int dimension;
        #region Fields
        #endregion

        #region Ctor
        public FieldType()
        {

        }

        public static FieldType Create(
            string name,
            DataType dataType,
            Dictionary<string, string> typeParams = null,
            bool isPrimaryLey = false,
            bool isAutoID = false)
        {
            var field = new FieldType()
            {
                Name = name,
                DataType = dataType,
                IsAutoID = isAutoID,
                IsPrimaryKey = isPrimaryLey
            };

            if (typeParams != null)
            {
                foreach (var typeParam in typeParams)
                {
                    field.TypeParams[typeParam.Key] = typeParam.Value;
                }
            }
            field.Check();

            return field;
        }

        public static FieldType Create(
            string name,
            string description,
            DataType dataType,
            int dimension,
            int maxLength,
            Dictionary<string, string> typeParams = null,
            bool isPrimaryLey = false,
            bool isAutoID = false)
        {
            var field = new FieldType()
            {
                Name = name,
                Description = description,
                DataType = dataType,
                Dimension = dimension,
                MaxLength = maxLength,
                IsAutoID = isAutoID,
                IsPrimaryKey = isPrimaryLey
            };

            if (typeParams != null)
            {
                foreach (var typeParam in typeParams)
                {
                    field.TypeParams[typeParam.Key] = typeParam.Value;
                }
            }
            field.Check();

            return field;
        }
        #endregion

        #region Properties
        public int Dimension
        {
            get => dimension; set
            {
                dimension = value;
                TypeParams[Constant.VECTOR_DIM] = Dimension.ToString();
            }
        }

        public int MaxLength { get; set; }

        public string Description { get; set; } = "";

        public bool IsPrimaryKey { get; set; } = false;

        public string Name { get; set; }

        public DataType DataType { get; set; }

        public Dictionary<string, string> TypeParams { get; } = new Dictionary<string, string>();

        public bool IsAutoID { get; set; } = false;
        #endregion

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(Name, "Field name");

            if (DataType == DataType.None)
            {
                throw new ParamException("Field data type is illegal");
            }

            //TODO: Need Check
            //if (dataType == DataType.String)
            //{
            //    throw new ParamException("String type is not supported, use VarChar instead");
            //}

            if (DataType == DataType.FloatVector || DataType == DataType.BinaryVector)
            {
                if (!TypeParams.ContainsKey(Constant.VECTOR_DIM))
                {
                    throw new ParamException("Vector field dimension must be specified");
                }

                try
                {
                    int dim = int.Parse(TypeParams[Constant.VECTOR_DIM]);
                    if (dim <= 0)
                    {
                        throw new ParamException("Vector field dimension must be larger than zero");
                    }
                }
                catch (FormatException)
                {
                    throw new ParamException("Vector field dimension must be an integer number");
                }
            }

            if (DataType == DataType.String)
            {
                if (!TypeParams.ContainsKey(Constant.VARCHAR_MAX_LENGTH))
                {
                    throw new ParamException("Varchar field max length must be specified");
                }

                try
                {
                    int len = int.Parse(TypeParams[Constant.VARCHAR_MAX_LENGTH]);
                    if (len <= 0)
                    {
                        throw new ParamException("Varchar field max length must be larger than zero");
                    }
                }
                catch (FormatException)
                {
                    throw new ParamException("Varchar field max length must be an integer number");
                }
            }
        }

        /// <summary>
        /// Construct a <code>string</code> by <see cref="FieldType"/> instance.
        /// </summary>
        /// <returns><see cref="string"/></returns>
        public override string ToString()
        {
            return $"FieldType{{{nameof(Name)}={Name}\', {nameof(DataType)}={DataType}\', {nameof(IsPrimaryKey)}={IsPrimaryKey}, {nameof(IsAutoID)}={IsAutoID}, {nameof(TypeParams)}={TypeParams}}}";
        }
    }
}
