using IO.Milvus.Grpc;
using IO.Milvus.Param.Collection;

namespace IO.Milvus.Workbench.Models.Fields
{
    public class VectorField : Field
    {
        private int _selectedField;
        private int _dimension = 128;

        public DataType FieldType { get; set; } = DataType.FloatVector;

        public int Dimension { get => _dimension; set => _dimension = value; }

        public int SelectedField
        {
            get => _selectedField; set
            {
                _selectedField = value;
                if (value == 0)
                {
                    FieldType = DataType.FloatVector;
                }
                else if (value == 1)
                {
                    FieldType = DataType.BinaryVector;
                }
            }
        }

        public override FieldType ToFieldType()
        {
            return new FieldType()
            {
                DataType = FieldType,
                Description = Description,
                Name = Name,
                Dimension = Dimension,
            };
        }
    }
}
