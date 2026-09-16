namespace Milvus.Client;

/// <summary>
/// A single token produced by running a text analyzer over an input string, via
/// <see cref="MilvusClient.RunAnalyzerAsync" />.
/// </summary>
public sealed class AnalyzerToken
{
    internal AnalyzerToken(string token, long startOffset, long endOffset, long position, long positionLength, uint? hash)
    {
        Token = token;
        StartOffset = startOffset;
        EndOffset = endOffset;
        Position = position;
        PositionLength = positionLength;
        Hash = hash;
    }

    /// <summary>
    /// The token text produced by the analyzer, e.g. a single word after tokenization and any configured
    /// filters (lowercasing, stemming, stop-word removal, ...).
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// The byte offset of the token's start in the original input string.
    /// </summary>
    public long StartOffset { get; }

    /// <summary>
    /// The byte offset of the token's end in the original input string.
    /// </summary>
    public long EndOffset { get; }

    /// <summary>
    /// The token's position (its index among tokens produced from the input), used for phrase matching.
    /// </summary>
    public long Position { get; }

    /// <summary>
    /// The number of positions this token spans. Normally 1; a token produced by some filters (e.g. a
    /// synonym or shingle expansion) can span more than one position.
    /// </summary>
    public long PositionLength { get; }

    /// <summary>
    /// The token's hash, if requested via <c>withHash</c>; <see langword="null" /> otherwise.
    /// </summary>
    public uint? Hash { get; }
}
