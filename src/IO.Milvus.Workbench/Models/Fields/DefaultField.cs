using IO.Milvus.Grpc;
using IO.Milvus.Param.Collection;
using System.Collections.Generic;

namespace IO.Milvus.Workbench.Models.Fields
{
    public class DefaultField:Field
    {
        public DataType FieldType { get; set; }

        public List<DataType> DataTypes { get; set; } = new List<DataType>
        {
            DataType.Int8,
            DataType.Int16,
            DataType.Int32,
            DataType.Int64,
            DataType.Float,
            DataType.Double,
            DataType.Bool,
        };

        public override FieldType ToFieldType()
        {
            return new FieldType()
            {
                DataType = FieldType,
                Description = Description,
                Name = Name,
            };
        }
    }
}
