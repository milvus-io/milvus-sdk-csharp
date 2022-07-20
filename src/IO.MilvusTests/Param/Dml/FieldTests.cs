using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.Milvus.Param.Dml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Milvus.Param.Dml.Tests
{
    [TestClass()]
    public class FieldTests
    {
        [TestMethod()]
        public void ToGrpcFieldDataBoolTest()
        {
            var boolField = new Field<bool>
            {
                Datas = new List<bool>()
                {
                    true,true,
                },
                FieldName = "test"
            };

            Assert.IsNotNull(boolField);
            var grpcField = boolField.ToGrpcFieldData();
            Assert.IsNotNull(grpcField);
            Assert.IsTrue(grpcField.Scalars.BoolData.Data.Count > 0);
        }

        [TestMethod()]
        public void ToGrpcFieldDataIntTest()
        {
            var intField = new Field<int>
            {
                Datas = new List<int>()
                {
                    1,2,
                },
                FieldName = "test"
            };

            Assert.IsNotNull(intField);

            var grpcField = intField.ToGrpcFieldData();
            Assert.IsNotNull(grpcField);
            Assert.IsTrue(grpcField.Scalars.IntData.Data.Count > 0);
        }

        [TestMethod()]
        public void ToGrpcFieldDataInt16Test()
        {
            var intField = new Field<Int16>
            {
                Datas = new List<Int16>()
                {
                    1,2,
                },
                FieldName = "test"
            };
            
            Assert.IsNotNull(intField);

            var grpcField = intField.ToGrpcFieldData();
            Assert.IsNotNull(grpcField);
            Assert.IsTrue(grpcField.Scalars.IntData.Data.Count > 0);
        }

        [TestMethod()]
        public void ToGrpcFieldDataInt32Test()
        {
            var intField = new Field<Int32>
            {
                Datas = new List<Int32>()
                {
                    1,2,
                },
                FieldName = "test"
            };

            Assert.IsNotNull(intField);

            var grpcField = intField.ToGrpcFieldData();
            Assert.IsNotNull(grpcField);
            Assert.IsTrue(grpcField.Scalars.IntData.Data.Count > 0);
        }

        [TestMethod()]
        public void ToGrpcFieldDataInt64Test()
        {
            var intField = new Field<Int64>
            {
                Datas = new List<Int64>()
                {
                    1,2,
                },
                FieldName = "test"
            };

            Assert.IsNotNull(intField);

            var grpcField = intField.ToGrpcFieldData();
            Assert.IsNotNull(grpcField);
            Assert.IsTrue(grpcField.Scalars.LongData.Data.Count > 0);
        }

        [TestMethod()]
        public void ToGrpcFieldDataLongTest()
        {
            var intField = new Field<long>
            {
                Datas = new List<long>()
                {
                    1,2,
                },
                FieldName = "test"
            };

            Assert.IsNotNull(intField);

            var grpcField = intField.ToGrpcFieldData();
            Assert.IsNotNull(grpcField);
            Assert.IsTrue(grpcField.Scalars.LongData.Data.Count > 0);
        }
    }
}