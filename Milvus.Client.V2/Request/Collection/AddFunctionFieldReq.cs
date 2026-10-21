using System.Globalization;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to add a function output field (together with its bound index) to an existing
/// collection.
/// </summary>
/// <remarks>
/// Only BM25 (with <see cref="Milvus.Client.V2.Types.DataType.SparseFloatVector" />) and MinHash (with
/// <see cref="Milvus.Client.V2.Types.DataType.BinaryVector" />) function fields can be added to an existing
/// collection, mirroring the Java/C++/Rust SDKs.
/// </remarks>
public sealed class AddFunctionFieldReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to add the function field to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The schema of the function's output field. The data type must match the output type required by the
    /// function's type.
    /// </summary>
    public FieldSchema Field { get; set; } = null!;

    /// <summary>
    /// The function to add, e.g. a BM25 function whose output field is <see cref="Field" />.
    /// </summary>
    public FunctionSchema Function { get; set; } = null!;

    /// <summary>
    /// The index to bind to the function's output field. An explicit, non-<see cref="IndexType.Invalid" />
    /// index type is required.
    /// </summary>
    public IndexParam IndexParam { get; set; } = null!;
    internal Grpc.AlterCollectionSchemaRequest ToGrpcAlterCollectionSchemaRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(Field);
        Verify.NotNull(Function);
        Verify.NotNull(IndexParam);

        if (string.IsNullOrWhiteSpace(Field.Name))
        {
            throw new ArgumentException("The function output field name must be specified.", nameof(Field));
        }

        if (Field.DataType == DataType.None)
        {
            throw new ArgumentException("The function output field data type must be specified.", nameof(Field));
        }

        if (string.IsNullOrWhiteSpace(Function.Name))
        {
            throw new ArgumentException("The function name must be specified.", nameof(Function));
        }

        // The function must produce the field being added; otherwise the server rejects the request with an
        // opaque error after the DDL is attempted. Fail fast like the sibling checks.
        if (!Function.OutputFieldNames.Contains(Field.Name, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "The function's output field names must include the field being added.", nameof(Function));
        }

        // Only BM25 (SparseFloatVector) and MinHash (BinaryVector) function fields can be added to an existing
        // collection, matching the Java/C++/Rust SDKs.
        DataType expectedOutputType = Function.Type switch
        {
            FunctionType.Bm25 => DataType.SparseFloatVector,
            FunctionType.MinHash => DataType.BinaryVector,
            _ => throw new ArgumentException(
                $"addFunctionField only supports {nameof(FunctionType.Bm25)} with " +
                $"{nameof(DataType.SparseFloatVector)} or {nameof(FunctionType.MinHash)} with " +
                $"{nameof(DataType.BinaryVector)}, got {Function.Type}.", nameof(Function))
        };
        if (Field.DataType != expectedOutputType)
        {
            throw new ArgumentException(
                $"addFunctionField requires a {expectedOutputType} output field for {Function.Type}, got " +
                $"{Field.DataType}.", nameof(Field));
        }

        // Binary vectors store one bit per dimension; the server requires the dimension to be a multiple of 8.
        // Fail fast instead of waiting for a create-time server error, matching the create-collection path.
        if (Field.DataType == DataType.BinaryVector
            && Field.Dimension is { } binaryDim
            && (binaryDim < 8 || binaryDim % 8 != 0))
        {
            throw new ArgumentOutOfRangeException(nameof(Field), binaryDim,
                $"Binary vector field '{Field.Name}' dimension {binaryDim} must be a multiple of 8 (and at least 8).");
        }

        if (IndexParam.IndexType is not { } indexType || indexType == IndexType.Invalid)
        {
            throw new ArgumentException(
                "The bound index must specify a non-Invalid index type.", nameof(IndexParam));
        }

        if (!string.IsNullOrWhiteSpace(IndexParam.FieldName)
            && !string.Equals(IndexParam.FieldName, Field.Name, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The bound index field must match the function output field being added.", nameof(IndexParam));
        }

        var fieldInfo = new Grpc.AlterCollectionSchemaRequest.Types.FieldInfo
        {
            FieldSchema = CreateCollectionReq.ConvertToGrpcFieldSchema(Field, isFunctionOutput: true),
            IndexName = IndexParam.IndexName ?? ""
        };

        fieldInfo.ExtraParams.Add(new Grpc.KeyValuePair
        {
            Key = Constants.IndexType,
            Value = indexType.ToWireString()
        });

        if (IndexParam.MetricType is { } metricType && metricType != MetricType.Invalid)
        {
            fieldInfo.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = Constants.MetricType,
                Value = metricType.ToWireString()
            });
        }

        foreach (KeyValuePair<string, object> parameter in IndexParam.ExtraParams)
        {
            // The typed entries above take precedence: index_type is always emitted (it is mandatory), and
            // metric_type is skipped only when the typed MetricType was emitted. A raw "metric_type" supplied
            // only via ExtraParams is honored when the typed metric is unset, mirroring the Java SDK's
            // addFunctionField and the CreateIndexReq path in this repo.
            if (parameter.Key == Constants.IndexType
                || (parameter.Key == Constants.MetricType
                    && IndexParam.MetricType is { } metric && metric != MetricType.Invalid))
            {
                continue;
            }

            fieldInfo.ExtraParams.Add(new Grpc.KeyValuePair
            {
                Key = parameter.Key,
                // Mirror Java's String.valueOf for arbitrary object values (bool renders lowercase, matching
                // the CreateIndexReq path).
                Value = parameter.Value is bool b
                    ? b ? "true" : "false"
                    : Convert.ToString(parameter.Value, CultureInfo.InvariantCulture) ?? ""
            });
        }

        var addRequest = new Grpc.AlterCollectionSchemaRequest.Types.AddRequest();
        addRequest.FieldInfos.Add(fieldInfo);
        addRequest.FuncSchema.Add(Function.ToGrpcFunctionSchema());

        var request = new Grpc.AlterCollectionSchemaRequest
        {
            CollectionName = CollectionName,
            Action = new Grpc.AlterCollectionSchemaRequest.Types.Action
            {
                AddRequest = addRequest
            }
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
