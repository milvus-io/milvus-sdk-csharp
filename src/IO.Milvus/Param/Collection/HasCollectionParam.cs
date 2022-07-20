using System;
using System.Diagnostics.CodeAnalysis;

namespace IO.Milvus.Param.Collection
{
    /// <summary>
    /// Parameters for <code>hasCollection</code> interface.
    /// </summary>
    public class HasCollectionParam
    {
        public HasCollectionParam()
        {
        }

        public static HasCollectionParam Create(string name)
        {
            var param = new HasCollectionParam()
            {
                CollectionName = name
            };
            param.Check();

            return param;
        }

        public string CollectionName { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(CollectionName, $"{nameof(HasCollectionParam)}.{nameof(HasCollectionParam.CollectionName)}");
        }

        /// <summary>
        /// Constructs a <code>String</code> by <see cref="HasCollectionParam"/> instance.
        /// </summary>
        /// <returns><code>string</code></returns>
        public override string ToString()
        {
            return $"{nameof(HasCollectionParam)}{{{nameof(CollectionName)}=\'{CollectionName}\'}}";
        }
    }
}
