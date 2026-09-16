using System.Text.Json;

namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Runs a text analyzer over one or more input strings and returns the resulting tokens, without
    /// requiring any data to be inserted. Useful for previewing how a given analyzer configuration (or an
    /// existing field's configured analyzer) will tokenize text before committing to it in a schema.
    /// Available since Milvus v2.5.
    /// </summary>
    /// <param name="texts">The input strings to analyze.</param>
    /// <param name="analyzerParams">
    /// The analyzer configuration to test, e.g. <c>new Dictionary&lt;string, object&gt; { ["type"] = "english" }</c>.
    /// Mutually exclusive with <paramref name="collectionName" />/<paramref name="fieldName" />: to test a
    /// field's own already-configured analyzer instead of an ad hoc one, pass those instead and leave this
    /// <see langword="null" />.
    /// </param>
    /// <param name="collectionName">
    /// Together with <paramref name="fieldName" />, runs that field's own configured analyzer instead of
    /// <paramref name="analyzerParams" />. The collection must be loaded, and -- verified against Milvus
    /// 2.6.4 -- the field must be the input field of a BM25 function; any other field, even one with
    /// <see cref="FieldSchema.EnableAnalyzer" /> set, is rejected with "now only support run analyzer by
    /// field if field was bm25 input field".
    /// </param>
    /// <param name="fieldName">See <paramref name="collectionName" />.</param>
    /// <param name="withDetail">
    /// Whether to include position and offset detail for each token. When <see langword="false" /> (the
    /// default), <see cref="AnalyzerToken.StartOffset" />, <see cref="AnalyzerToken.EndOffset" /> and
    /// <see cref="AnalyzerToken.Position" /> all come back as 0 rather than being omitted.
    /// </param>
    /// <param name="withHash">Whether to include each token's hash.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    /// One list of <see cref="AnalyzerToken" /> per input string, in the same order as
    /// <paramref name="texts" />.
    /// </returns>
    public async Task<IReadOnlyList<IReadOnlyList<AnalyzerToken>>> RunAnalyzerAsync(
        IReadOnlyList<string> texts,
        IReadOnlyDictionary<string, object>? analyzerParams = null,
        string? collectionName = null,
        string? fieldName = null,
        bool withDetail = false,
        bool withHash = false,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(texts);

        bool hasFieldRef = collectionName is not null || fieldName is not null;

        if (analyzerParams is not null && hasFieldRef)
        {
            throw new ArgumentException(
                $"{nameof(analyzerParams)} is mutually exclusive with {nameof(collectionName)}/{nameof(fieldName)} -- " +
                "pass one or the other, not both.");
        }

        if (hasFieldRef && (collectionName is null || fieldName is null))
        {
            throw new ArgumentException(
                $"{nameof(collectionName)} and {nameof(fieldName)} must be supplied together to run a field's " +
                "own configured analyzer.");
        }

        RunAnalyzerRequest request = new()
        {
            WithDetail = withDetail,
            WithHash = withHash,
        };

        request.Placeholder.AddRange(texts.Select(ByteString.CopyFromUtf8));

        if (analyzerParams is not null)
        {
            request.AnalyzerParams = JsonSerializer.Serialize(analyzerParams);
        }

        if (collectionName is not null)
        {
            request.CollectionName = collectionName;
        }

        if (fieldName is not null)
        {
            request.FieldName = fieldName;
        }

        RunAnalyzerResponse response = await InvokeAsync(
                GrpcClient.RunAnalyzerAsync, request, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        List<IReadOnlyList<AnalyzerToken>> results = new(response.Results.Count);
        foreach (AnalyzerResult result in response.Results)
        {
            List<AnalyzerToken> tokens = new(result.Tokens.Count);
            foreach (Grpc.AnalyzerToken token in result.Tokens)
            {
                tokens.Add(new AnalyzerToken(
                    token.Token, token.StartOffset, token.EndOffset, token.Position, token.PositionLength,
                    withHash ? token.Hash : null));
            }

            results.Add(tokens);
        }

        return results;
    }
}
