using IO.Milvus.Client;
using IO.Milvus.Param.Dml;
using IO.MilvusTests.Client.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Milvus.Client.Tests
{
    [TestClass]
    public class QueryTest : MilvusServiceClientTestsBase
    {
        [TestMethod()]
        [DataRow("book_id > 0 && book_id < 2000")]
        public void QueryDataTest(string expr)
        {
            var r = MilvusClient.Query(QueryParam.Create(
                "book",
                new List<string> { "_default" },
                new List<string>() { "book_id", "word_count", "book_intro" },
                expr: expr));

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Param.Status.Success);
        }
    }
}
