#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;
public sealed class FunctionSchema
{
    public FunctionSchema(string name, FunctionType type, IEnumerable<string> inputFieldNames, IEnumerable<string> outputFieldNames, string description = "")
    {
        Verify.NotNullOrWhiteSpace(name);
        Name = name;
        Type = type;
        _inputFieldNames.AddRange(inputFieldNames);
        _outputFieldNames.AddRange(outputFieldNames);
        Description = description;
    }

    public static FunctionSchema CreateBm25(string name, string inputFieldName, string outputFieldName, string description = "")
        => new(name, FunctionType.Bm25, new[] { inputFieldName }, new[] { outputFieldName }, description);

    private readonly List<string> _inputFieldNames = new();
    private readonly List<string> _outputFieldNames = new();

    public long Id { get; }
    public string Name { get; }
    public FunctionType Type { get; }
    public string Description { get; }
    public IList<string> InputFieldNames => _inputFieldNames;
    public IList<string> OutputFieldNames => _outputFieldNames;

    internal Grpc.FunctionSchema ToGrpcFunctionSchema()
    {
        var result = new Grpc.FunctionSchema
        {
            Name = Name,
            Type = (Grpc.FunctionType)(int)Type,
            Description = Description
        };
        result.InputFieldNames.AddRange(_inputFieldNames);
        result.OutputFieldNames.AddRange(_outputFieldNames);
        return result;
    }
}
