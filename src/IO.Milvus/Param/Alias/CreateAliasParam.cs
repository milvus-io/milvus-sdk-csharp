using System.Diagnostics.CodeAnalysis;

namespace IO.Milvus.Param.Alias
{
    /// <summary>
    /// Parameters for <see cref="CreateAliasParam"/> interface.
    /// </summary>
    public class CreateAliasParam
    {
        public static CreateAliasParam Create(
            string collectionName,
            string alias)
        {
            var param = new CreateAliasParam()
            {
                CollectionName = collectionName,
                Alias = alias
            };
            param.Check();

            return param;
        }

        #region Properties
        public string Alias { get; set; }

        public string CollectionName { get; set; }
        #endregion

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, "Collection name");
            ParamUtils.CheckNullEmptyString(Alias, "Alias");
        }
    }
}
