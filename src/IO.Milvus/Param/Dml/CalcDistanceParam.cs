using IO.Milvus.Exception;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus.Param.Dml
{
    public class CalcDistanceParam
    {
        public static CalcDistanceParam Create(
            List<List<float>> vectorsLeft, 
            List<List<float>> vectorsRight, 
            MetricType metricType)
        {
            var param = new CalcDistanceParam()
            {
                VectorsLeft = vectorsLeft,
                VectorsRight = vectorsRight,
                MetricType = metricType,
            };
            param.Check();

            return param;
        }

        public List<List<float>> VectorsLeft { get; set; }

        public List<List<float>> VectorsRight { get; set; }

        public MetricType MetricType { get; set; } = MetricType.INVALID;

        internal void Check()
        {
            if (MetricType == MetricType.INVALID)
            {
                throw new ParamException("Metric type is illegal");
            }

            if (MetricType != MetricType.L2 && MetricType != MetricType.IP)
            {
                throw new ParamException("Only support L2 or IP metric type now!");
            }

            if (VectorsLeft.IsEmpty())
            {
                throw new ParamException("Left vectors can not be empty");
            }

            int count = VectorsLeft.First().Count;
            if (VectorsLeft.All(p => p.Count == count))
            {
                throw new ParamException("Left vector's dimension must be equal");
            }

            if (VectorsRight.IsEmpty())
            {
                throw new ParamException("Right vectors can not be empty");
            }

            count = VectorsRight.First().Count;
            if (VectorsRight.All(p => p.Count == count))
            {
                throw new ParamException("Right vector's dimension must be equal");
            }
        }
    }
}
