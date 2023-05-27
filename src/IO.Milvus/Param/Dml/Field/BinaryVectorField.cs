using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus.Param.Dml
{
    /// <summary>
    /// Binary vector field.
    /// </summary>
    public class BinaryVectorField : Field
    {
        public BinaryVectorField(string name, List<List<float>> data)
        {
            FieldName = name;
            Data = data;
        }

        /// <summary>
        /// Float vector data.
        /// </summary>
        public List<List<float>> Data { get; set; }

        public override int RowCount => Data?.Count ?? 0;

        public override FieldData ToGrpcFieldData()
        {
            var floatArray = new FloatArray();

            var count = Data.First().Count;
            if (!Data.All(p =>p.Count == count))
            {
                throw new ParamException("Row count of fields must be equal");
            }
            foreach (var data in Data)
            {
                floatArray.Data.AddRange(data);
            }

            return new FieldData()
            {
                FieldName = FieldName,
                Type = DataType,               
                Vectors = new VectorField()
                {
                    FloatVector = floatArray,                    
                    Dim = count,
                },
            };
        }
    }
}
