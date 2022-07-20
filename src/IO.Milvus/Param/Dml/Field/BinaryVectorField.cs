using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus.Param.Dml
{
    public class BinaryVectorField : Field
    {
        public List<List<float>> Datas { get; set; }

        public override int RowCount => Datas?.Count ?? 0;

        public override FieldData ToGrpcFieldData()
        {
            var floatArray = new FloatArray();

            var count = Datas.First().Count;
            if (!Datas.All(p =>p.Count == count))
            {
                throw new ParamException("Row count of fields must be equal");
            }
            foreach (var data in Datas)
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
