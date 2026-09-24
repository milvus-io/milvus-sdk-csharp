namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// A single token produced by a text analyzer, with its offsets and position information.
/// </summary>
public sealed class AnalyzerToken
{
    internal AnalyzerToken(string token, long startOffset, long endOffset, long position, long positionLength, uint hash)
    {
        Token = token;
        StartOffset = startOffset;
        EndOffset = endOffset;
        Position = position;
        PositionLength = positionLength;
        Hash = hash;
    }
    internal static AnalyzerToken FromGrpc(Grpc.AnalyzerToken token)
        => new(token.Token, token.StartOffset, token.EndOffset, token.Position, token.PositionLength, token.Hash);

    /// <summary>
    /// The token text.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// The zero-based start offset of the token in the analyzed text.
    /// </summary>
    public long StartOffset { get; }

    /// <summary>
    /// The zero-based end offset of the token in the analyzed text.
    /// </summary>
    public long EndOffset { get; }

    /// <summary>
    /// The position of the token within the analyzed text.
    /// </summary>
    public long Position { get; }

    /// <summary>
    /// The length of the token's position range.
    /// </summary>
    public long PositionLength { get; }

    /// <summary>
    /// The hash of the token, populated when the analyzer was run with hash output requested.
    /// </summary>
    public uint Hash { get; }
}
