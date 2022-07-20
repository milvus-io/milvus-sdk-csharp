using System;

namespace IO.Milvus.Param.Control
{
    public class GetMetricsParam
    {
        public static GetMetricsParam Create(string request)
        {
            var param = new GetMetricsParam()
            {
                Request = request
            };
            param.Check();

            return param;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(Request, nameof(Request));
        }

        public string Request { get; set; }
    }
}
