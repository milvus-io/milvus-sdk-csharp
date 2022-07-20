namespace IO.Milvus.Param.Collection
{
    /// <summary>
    /// Parameters for <code>dropCollection</code> interface.
    /// </summary>
    public class DropCollectionParam
    {
        /// <summary>
        /// Create a <see cref="DropCollectionParam"/>
        /// </summary>
        /// <param name="collectionName">collection name</param>
        /// <returns></returns>
        public static DropCollectionParam Create(string collectionName)
        {
            var param = new DropCollectionParam()
            {
                CollectionName = collectionName
            };
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, $"{nameof(DropCollectionParam)}.{nameof(CollectionName)}");
        }

        public override string ToString()
        {
            return $"{nameof(DropCollectionParam)}{{{nameof(CollectionName)}=/'{CollectionName}/'}}";
        }
    }
}
