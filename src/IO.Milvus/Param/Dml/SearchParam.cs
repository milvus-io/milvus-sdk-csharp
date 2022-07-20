using IO.Milvus.Common.ClientEnum;
using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IO.Milvus.Param.Dml
{
    public class SearchParam<TVector>
    {
        public static SearchParam<TVector> Create(
            string collectionName,
            string vectorFieldName,
            MetricType metricType,
            List<TVector> vectors,
            ConsistencyLevelEnum consistencyLevel = ConsistencyLevelEnum.STRONG,
            List<string> outfields = null,
            int topk = 0,
            int roundDecimal = 1,
            string param = "{}",
            ulong travelTimestamp = 0L,
            ulong guaranteeTimestamp = Constant.GUARANTEE_EVENTUALLY_TS,
            ulong gracefulTime = 5000L
            )
        {
            var sparam = new SearchParam<TVector>()
            {
                CollectionName = collectionName,
                VectorFieldName = vectorFieldName,
                MetricType = metricType,
                Vectors = vectors,
                ConsistencyLevel = consistencyLevel,
                OutFields = outfields,
                TopK = topk,
                RoundDecimal = roundDecimal,
                TravelTimestamp = travelTimestamp,
                GuaranteeTimestamp = guaranteeTimestamp,
                GracefulTime = gracefulTime
            };
            sparam.Check();

            return sparam;
        }

        public string CollectionName { get; set; }

        public List<string> PartitionNames { get; set; } = new List<string>();

        public MetricType MetricType { get; set; }

        public string VectorFieldName { get; set; }

        public int TopK { get; set; }

        public string Expr { get; set; } = "";

        public List<string> OutFields { get; set; } = new List<string>();

        public List<TVector> Vectors { get; set; }

        public int RoundDecimal { get; set; } = 1;

        public string Params { get; set; } = "{}";

        public ulong TravelTimestamp { get; set; } = 0L;

        public ulong GuaranteeTimestamp { get; set; } = Constant.GUARANTEE_EVENTUALLY_TS;

        public ulong GracefulTime { get; set; } = 5000L;

        public ConsistencyLevelEnum ConsistencyLevel { get; set; }

        internal void Check()
        {

            ParamUtils.CheckNullEmptyString(CollectionName, "Collection name");
            ParamUtils.CheckNullEmptyString(VectorFieldName, "Target field name");

            if (TopK <= 0)
            {
                throw new ParamException("T opK value is illegal");
            }

            if (TravelTimestamp < 0)
            {
                throw new ParamException("The travel timestamp must be greater than 0");
            }

            if (GuaranteeTimestamp < 0)
            {
                throw new ParamException("The guarantee timestamp must be greater than 0");
            }

            if (MetricType == Param.MetricType.INVALID)
            {
                throw new ParamException("Metric type is invalid");
            }

            if (Vectors.IsEmpty())
            {
                throw new ParamException("Target vectors can not be empty");
            }

            if (typeof(TVector) == typeof(List<float>))
            {
                int dim = (Vectors.First() as List<float>).Count;
                for (int i = 1; i < Vectors.Count; ++i)
                {
                    List<float> temp = Vectors[i] as List<float>;
                    if (dim != temp.Count)
                    {
                        throw new ParamException("Target vector dimension must be equal");
                    }
                }

                if (!ParamUtils.IsFloatMetric(MetricType))
                {
                    throw new ParamException("Target vector is float but metric type is incorrect");
                }
            }
            else if (typeof(TVector) == typeof(MemoryStream))
            {
                // binary vectors
                MemoryStream first = Vectors[0] as MemoryStream;
                var dim = first.Position;
                for (int i = 1; i < Vectors.Count; ++i)
                {
                    MemoryStream temp = Vectors[i] as MemoryStream;
                    if (dim != temp.Position)
                    {
                        throw new ParamException("Target vector dimension must be equal");
                    }
                }

                if (!ParamUtils.IsBinaryMetric(MetricType))
                {
                    throw new ParamException("Target vector is binary but metric type is incorrect");
                }
            }
            else
            {
                throw new ParamException("Target vector type must be List<Float> or ByteBuffer");
            }
        }

        internal SearchRequest ToRequset(bool check = false)
        {
            if (check)
            {
                Check();
            }

            var request = new SearchRequest()
            {
                CollectionName = CollectionName,
                GuaranteeTimestamp = GuaranteeTimestamp,
                TravelTimestamp = TravelTimestamp,
            };

            request.OutputFields.AddRange(OutFields);
            request.PartitionNames.AddRange(PartitionNames);

            //TODO add vectors
            PlaceholderType plType = PlaceholderType.None;
            foreach (var vector in Vectors)
            {
                if (typeof(TVector) == typeof(float))
                {
                    plType = PlaceholderType.FloatVector;

                    
                }
            }

            return request;
        }
    }
}
