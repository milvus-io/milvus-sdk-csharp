using IO.Milvus.Param;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace IO.Milvus.Exception
{

    [Serializable]
    public class MilvusException : System.Exception
    {
        public MilvusException() { }
        
        public MilvusException(string message) : base(message) { }

        public MilvusException(string message,Status status) : base(message) { }

        public MilvusException(string message, System.Exception inner) : base(message, inner) { }
       
        protected MilvusException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }

        public Status Status { get; internal set; }
    }
}
