using System;

namespace IO.Milvus.Param.Control
{
    public class GetPersistentSegmentInfoParam
    {
        public static GetPersistentSegmentInfoParam Create(string collectionName)
        {
            var param = new GetPersistentSegmentInfoParam()
            {
                CollectionName = collectionName
            };
            param.Check();

            return param;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, nameof(CollectionName));
        }

        public string CollectionName{ get; set; }
    }
}
