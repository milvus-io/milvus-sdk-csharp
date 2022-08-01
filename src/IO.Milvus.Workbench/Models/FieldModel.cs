using IO.Milvus.Grpc;
using System.Collections.Generic;

namespace IO.Milvus.Workbench.Models
{
    public class FieldModel
    {
        public FieldModel(
            bool isPrimaryKey, 
            string name, 
            long fieldID, 
            DataType dataType, 
            string description)
        {
            IsPrimaryKey = isPrimaryKey;
            Name = name;
            FieldID = fieldID;
            DataType = dataType;
            Description = description;
        }
        
        public bool IsPrimaryKey { get; set; }

        public string Name { get; set; }

        public long FieldID { get; set; }

        public DataType DataType { get; set; }

        public string Description { get; set; }

        public int? Dimension { get; set; }

        public string IndexType { get; set; }

        public string IndexParameters { get; set; }

        public override string ToString()
        {
            return $"{nameof(IsPrimaryKey)}:{IsPrimaryKey}\n{nameof(FieldID)}:{FieldID}\n{nameof(DataType)}:{DataType}\n{nameof(Dimension)}:{Dimension}"; 
        }
    }
}
