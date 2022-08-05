using IO.Milvus.Exception;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Milvus.Param
{
    /// <summary>
    /// Defined single search for query node listener send heartbeat.
    /// </summary>
    public class QueryNodeSingleSearch<TVector>
    {
        #region Ctor
        private QueryNodeSingleSearch(Builder builder)
        {
            CollectionName = builder.collectionName;
            MetricType = builder.metricType;
            VectorFieldName = builder.vectorFieldName;
            Vectors = builder.vectors;
            Params = builder.@params;
        }
        #endregion

        #region Properties
        public string CollectionName { get; }

        public MetricType MetricType { get; }

        public string VectorFieldName { get; }

        public List<TVector> Vectors { get; }

        public string Params { get; }
        #endregion

        public static Builder NewBuilder()
        {
            return new Builder();
        }

        /// <summary>
        /// Builder for <see cref="QueryNodeSingleSearch"/>
        /// </summary>
        public class Builder
        {
            internal string collectionName;
            internal MetricType metricType = MetricType.L2;
            internal string vectorFieldName;
            internal List<TVector> vectors;
            internal string @params = "{}";

            internal Builder() { }

            /// <summary>
            /// Sets the collection name. Collection name cannot be empty or null.
            /// </summary>
            /// <param name="collectionName">collection name</param>
            /// <returns><code>Builder</code></returns>
            public Builder WithCollectionName(string collectionName)
            {
                this.collectionName = collectionName;
                return this;
            }

            /// <summary>
            /// Sets metric type of ANN searching.
            /// </summary>
            /// <param name="metricType">metric type</param>
            /// <returns><code>Builder</code></returns>
            public Builder WithMetricType(MetricType metricType)
            {
                this.metricType = metricType;
                return this;
            }

            /// <summary>
            /// Sets target vector field by name. Field name cannot be empty or null.
            /// </summary>
            /// <param name="vectorFieldName">vector field name</param>
            /// <returns></returns>
            public Builder WithVectorFieldName(string vectorFieldName)
            {
                this.vectorFieldName = vectorFieldName;
                return this;
            }

            /// <summary>
            /// Sets the target vectors.
            ///
            /// @param vectors list of target vectors:
            ///                if vector type is FloatVector, vectors is List&lt;List&lt;Float&gt;&gt;;
            ///                if vector type is BinaryVector, vectors is List&lt;ByteBuffer&gt;;
            /// </summary>
            /// <param name="vectors"></param>
            /// <returns><code>Builder</code></returns>
            public Builder WithVectors(List<TVector> vectors)
            {
                this.vectors = vectors;
                return this;
            }

            /// <summary>
            ///  Sets the search parameters specific to the index type.
            ///
            /// For example: IVF index, the search parameters can be "{\"nprobe\":10}"
            /// For more information: @see<a href="https://milvus.io/docs/v2.0.0/index_selection.md"> Index Selection</a>
            ///
            /// </summary>
            /// <param name="params">extra parameters in json format</param>
            /// <returns></returns>
            public Builder WithParams(string @params)
            {
                this.@params = @params;
                return this;
            }

            /// <summary>
            /// Verifies parameters and creates a new <see cref="QueryNodeSingleSearch{TVector}"/> instance.
            /// </summary>
            /// <returns></returns>
            /// <exception cref="ParamException"></exception>
            public QueryNodeSingleSearch<TVector> Build()
            {
                ParamUtils.CheckNullEmptyString(collectionName, "Collection name");
                ParamUtils.CheckNullEmptyString(vectorFieldName, "Target field name");

                if (metricType == MetricType.INVALID)
                {
                    throw new ParamException("Metric type is illegal");
                }

                if (vectors == null || vectors.Count == 0)
                {
                    throw new ParamException("Target vectors can not be empty");
                }

                if (typeof(TVector) ==  typeof(List<float>))
                {
                    int dim = (vectors.First() as List<float>).Count;
                    for (int i = 1; i < vectors.Count; ++i)
                    {
                        List<float> temp = vectors[i] as List<float>;
                        if (dim != temp.Count)
                        {
                            throw new ParamException("Target vector dimension must be equal");
                        }
                    }
                }
                else if (typeof(TVector) == typeof(MemoryStream))
                {
                    // binary vectors
                    MemoryStream first = vectors[0] as MemoryStream;
                    var dim = first.Position;
                    for (int i = 1; i < vectors.Count; ++i)
                    {
                        MemoryStream temp = vectors[i] as MemoryStream;
                        if (dim != temp.Position)
                        {
                            throw new ParamException("Target vector dimension must be equal");
                        }
                    }
                }
                else
                {
                    throw new ParamException("Target vector type must be List<Float> or ByteBuffer");
                }

                return new QueryNodeSingleSearch<TVector>(this);
            }
        }
    }
}
