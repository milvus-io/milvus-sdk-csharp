using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus.Param.Collection
{
    public class ShowCollectionsParam
    {
        public List<string> CollectionNames { get; } = new List<string>();

        public ShowType ShowType { get; set; }

        public static ShowCollectionsParam Create(List<string> collectionNames,ShowType showType = ShowType.All)
        {
            var param = new ShowCollectionsParam()
            {
                ShowType = showType
            };

            if (collectionNames != null)
            {
                foreach (var collectionName in collectionNames)
                {
                    if (!param.CollectionNames.Contains(collectionName))
                    {
                        param.CollectionNames.Add(collectionName);
                    }
                }
            }
            param.Check();

            return param;
        }

        internal void Check()
        {
            if (!CollectionNames.IsEmpty())
            {
                foreach (var collectionName in CollectionNames)
                {
                    ParamUtils.CheckNullEmptyString(collectionName, $"{nameof(ShowCollectionsParam)}'s {nameof(collectionName)} in {nameof(CollectionNames)}");
                }
                ShowType = ShowType.InMemory;
            }
        }
    }
}
