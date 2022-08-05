using System;

namespace IO.Milvus.Param.Collection
{
    /// <summary>
    /// Load a collection to memory
    /// </summary>
    public class LoadCollectionParam
    {
        public string CollectionName { get; set; }

        public static LoadCollectionParam Create(string collectionName)
        {
            var param = new LoadCollectionParam()
            {
                CollectionName = collectionName
            };
            param.Check();

            return param;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName,nameof(CollectionName));
        }
    }
}
