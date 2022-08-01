using IO.Milvus.Grpc;
using IO.Milvus.Param.Collection;

namespace IO.Milvus.Workbench.Models.Fields
{
    public class PrimaryField : Field
    {
        public DataType FieldType => DataType.Int64;

        public bool AutoID { get; set; } = true;

        public override FieldType ToFieldType()
        {
            return new FieldType()
            {
                DataType = FieldType,
                Description = Description,
                Name = Name,
                IsPrimaryKey = true,
                IsAutoID = AutoID
            };
        }
    }
}
