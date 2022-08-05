using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace IO.Milvus.Response
{
    public class SearchResultsWrapper<TFieldData>
    {
        private SearchResultData results;

        public SearchResultsWrapper(SearchResultData results)
        {
            this.results = results;
        }

        public List<TFieldData> GetFieldData(string fieldName, int indexOfTarget)
        {
            FieldDataWrapper wrapper = null;
            for (int i = 0; i < results.FieldsData.Count; ++i)
            {
                FieldData data = results.FieldsData[i];
                if (fieldName.CompareTo(data.FieldName) == 0)
                {
                    wrapper = new FieldDataWrapper(data);
                }
            }

            if (wrapper == null)
            {
                throw new ParamException("Illegal field name: " + fieldName);
            }

            Position position = GetOffsetByIndex(indexOfTarget);
            long offset = position.Offset;
            long k = position.K;

            var allData = wrapper.GetFieldData();
            if (offset + k > allData.Count)
            {
                throw new IllegalResponseException("Field data row count is wrong");
            }

            List<TFieldData> datas = new List<TFieldData>();
            for (int i = (int)offset; i < ((int)offset + (int)k); i++)
            {
                datas.Add((TFieldData)allData[i]);
            }
            return datas;
        }

        public List<IDScore> getIDScore(int indexOfTarget)
        {
            Position position = GetOffsetByIndex(indexOfTarget);

            long offset = position.Offset;
            long k = position.K;
            if (offset + k > results.Scores.Count)
            {
                throw new IllegalResponseException("Result scores count is wrong");
            }

            List<IDScore> idScore = new List<IDScore>();

            IDs ids = results.Ids;
            if (ids.IntId.Data.Count > 0)
            {
                LongArray longIDs = ids.IntId;
                if (offset + k > longIDs.Data.Count)
                {
                    throw new IllegalResponseException("Result ids count is wrong");
                }

                for (int n = 0; n < k; ++n)
                {
                    idScore.Add(new IDScore("", longIDs.Data[(int)offset + n], results.Scores[(int)offset + n]));
                }
            }
            else if (ids.StrId.Data.Count > 0)
            {
                StringArray strIDs = ids.StrId;
                if (offset + k > strIDs.Data.Count)
                {
                    throw new IllegalResponseException("Result ids count is wrong");
                }

                for (int n = 0; n < k; ++n)
                {
                    idScore.Add(new IDScore(strIDs.Data[(int)offset + n], 0, results.Scores[(int)offset + n]));
                }
            }
            else
            {
                throw new IllegalResponseException("Result ids is illegal");
            }

            return idScore;
        }

        private class Position
        {
            public Position(long offset, long k)
            {
                this.Offset = offset;
                this.K = k;
            }

            public long Offset { get; set; }
            public long K { get; set; }
        }

        private Position GetOffsetByIndex(int indexOfTarget)
        {
            List<long> kList = results.Topks.ToList();

            // if the server didn't return separate topK, use same topK value
            if (kList == null || kList.Count == 0)
            {
                kList = new List<long>();
                for (long i = 0; i < results.NumQueries; ++i)
                {
                    kList.Add(results.TopK);
                }
            }

            if (indexOfTarget < 0 || indexOfTarget >= kList.Count)
            {
                throw new ParamException("Illegal index of target: " + indexOfTarget);
            }

            long offset = 0;
            for (int i = 0; i < indexOfTarget; ++i)
            {
                offset += kList[i];
            }

            long k = kList[indexOfTarget];
            return new Position(offset, k);
        }

        /// <summary>
        /// Internal-use class to wrap response of <code>search</code> interface.
        /// </summary>
        public class IDScore
        {
            internal string StrID { get; set; }

            internal long LongID { get; set; }

            internal float Score { get; set; }

            public IDScore(String strID, long longID, float score)
            {
                this.StrID = strID;
                this.LongID = longID;
                this.Score = score;
            }


            public override string ToString()
            {
                if (string.IsNullOrEmpty(StrID))
                {
                    return "(ID: " + LongID + " Score: " + Score + ")";
                }
                else
                {
                    return "(ID: '" + StrID + "' Score: " + Score + ")";
                }
            }
        }
    }

}
