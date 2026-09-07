#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class AnalyzerToken
{
    internal AnalyzerToken(string token, long startOffset, long endOffset, long position)
    {
        Token = token;
        StartOffset = startOffset;
        EndOffset = endOffset;
        Position = position;
    }
    internal static AnalyzerToken FromGrpc(Grpc.AnalyzerToken token)
        => new(token.Token, token.StartOffset, token.EndOffset, token.Position);
    public string Token { get; }
    public long StartOffset { get; }
    public long EndOffset { get; }
    public long Position { get; }
}
