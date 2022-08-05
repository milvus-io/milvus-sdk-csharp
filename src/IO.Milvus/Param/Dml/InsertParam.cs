using Google.Protobuf.Collections;
using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IO.Milvus.Param.Dml
{
    public class InsertParam
    {
        public static InsertParam Create(
            string collectionName,
            string partitionName,
            List<Field> fields)
        {
            var param = new InsertParam()
            {
                CollectionName = collectionName,
                PartitionName = partitionName,
                Fields = fields
            };
            param.Check();
            return param;
        }

        public List<Field> Fields { get; set; } = new List<Field>();

        public string CollectionName { get; set; }

        public string PartitionName { get; set; } = "_default";

        public uint RowCount { get; set; }

        internal void Check()
        {
            if (Fields.IsEmpty())
            {
                throw new ParamException("Fields cannot be empty");
            }

            foreach (var field in Fields)
            {
                if (Fields.Any(f => f == null))
                {
                    throw new ParamException("Field cannot be null." +
             " If the field is auto-id, just ignore it from Fields");
                }

                ParamUtils.CheckNullEmptyString(field.FieldName, "Field Name");

                //if (field.Vectors == null || field.Vectors.Dim == 0)
                //{
                //    throw new ParamException("Field value cannot be empty." +
                //" If the field is auto-id, just ignore it from withFields()");
                //}
                
            }

            var count = Fields.First().RowCount;
            if (!Fields.All(p => p.RowCount == count))
            {
                throw new ParamException("Field Row count should be same");
            }
            RowCount = (uint)count;
            //Check dim count
            //var count = Fields.First().Vectors.Dim;
            //if (count == 0)
            //{
            //    throw new ParamException("Row count is zero");
            //}
            //if (!Fields.All(p => p.Vectors.Dim == count))
            //{
            //    throw new ParamException("Row count of fields must be equal");
            //}

            //TODO : More check for DataType
        }

    }
}
