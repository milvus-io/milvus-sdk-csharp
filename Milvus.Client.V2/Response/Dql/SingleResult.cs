using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Dql;

/// <summary>
/// The search results for a single query vector: its top-K hits with scores, primary keys and output fields.
/// Mirrors the C++ SDK's <c>SingleResult</c> (and pymilvus's <c>Hits</c>); a <see cref="SearchResp" />
/// carries one <see cref="SingleResult" /> per query vector, sliced out of the flat N*topk response by
/// <see cref="SearchResp.Limits" />.
/// </summary>
public sealed class SingleResult
{
    internal SingleResult(
        string primaryKeyName, Ids ids, IReadOnlyList<float> scores,
        IReadOnlyList<FieldData> outputFields)
    {
        PrimaryKeyName = primaryKeyName;
        Ids = ids;
        Scores = scores;
        OutputFields = outputFields;
    }

    /// <summary>
    /// The primary key field name of the searched collection (reported by the server), so callers do not need
    /// to describe the collection again to know the pk column.
    /// </summary>
    public string PrimaryKeyName { get; }

    /// <summary>
    /// The primary keys of the top-K hits, in score order.
    /// </summary>
    public Ids Ids { get; }

    /// <summary>
    /// The scores (distances) of the top-K hits, in descending similarity order.
    /// </summary>
    public IReadOnlyList<float> Scores { get; }

    /// <summary>
    /// The output fields of this query's hits, one column per requested output field (column-oriented, one
    /// element per hit). Combined with <see cref="Ids" /> and <see cref="Scores" /> to form each hit.
    /// </summary>
    public IReadOnlyList<FieldData> OutputFields { get; }

    /// <summary>
    /// Returns a new <see cref="SingleResult" /> containing only the rows at the given
    /// <paramref name="indices" /> (in the given order), preserving the primary-key name. Used by the search
    /// iterator's client-side page filter.
    /// </summary>
    internal SingleResult SelectRows(IReadOnlyList<int> indices)
        => indices.Count == 0
            ? new SingleResult(PrimaryKeyName, default, Array.Empty<float>(), Array.Empty<FieldData>())
            : new SingleResult(
                PrimaryKeyName,
                Ids.SelectRows(indices),
                indices.Select(i => Scores[i]).ToList(),
                OutputFields.Count == 0 ? OutputFields : OutputFields.Select(f => f.SelectRows(indices)).ToList());

    /// <summary>
    /// Merges multiple pages (e.g. filtered pages accumulated by the search iterator's external page filter)
    /// into a single <see cref="SingleResult" />, concatenating the primary-key lists, scores and output
    /// columns in order. All pages must share the same primary-key name and output-field set.
    /// </summary>
    internal static SingleResult Merge(IReadOnlyList<SingleResult> pages)
    {
        if (pages.Count == 0)
        {
            return new SingleResult("", default, Array.Empty<float>(), Array.Empty<FieldData>());
        }

        if (pages.Count == 1)
        {
            return pages[0];
        }

        IReadOnlyList<FieldData>? outputFields = null;
        foreach (SingleResult page in pages)
        {
            if (outputFields is null)
            {
                outputFields = page.OutputFields;
                continue;
            }

            // The same search request drives every page, so the columns arrive in the same order with the
            // same names; merge positionally and defensively fall back to name-matching.
            if (outputFields.Count == page.OutputFields.Count)
            {
                outputFields = outputFields.Zip(page.OutputFields, static (l, r) => l.Concat(r)).ToList();
            }
            else
            {
                outputFields = outputFields.Select(l =>
                    page.OutputFields.FirstOrDefault(r => r.FieldName == l.FieldName) is { } matched
                        ? l.Concat(matched)
                        : l).ToList();
            }
        }

        Ids ids = pages[0].Ids;
        var scores = new List<float>(pages.Sum(p => p.Scores.Count));
        for (int i = 1; i < pages.Count; i++)
        {
            ids = ids.Concat(pages[i].Ids);
        }

        foreach (SingleResult page in pages)
        {
            scores.AddRange(page.Scores);
        }

        return new SingleResult(pages[0].PrimaryKeyName, ids, scores, outputFields ?? Array.Empty<FieldData>());
    }

    /// <summary>
    /// Materializes the hit at row <paramref name="rowIndex" /> into a row dictionary mapping field name to
    /// value (plus the <c>"score"</c> entry), the same shape the row-based insert/upsert accepts.
    /// </summary>
    /// <param name="rowIndex">The zero-based hit index within this query's results.</param>
    /// <returns>
    /// A dictionary mapping each output field name to its value for the hit, plus <c>"score"</c>. The primary
    /// key is included when it is among the output fields.
    /// </returns>
    public IReadOnlyDictionary<string, object?> GetRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Scores.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex), rowIndex,
                $"Must be in [0, {Scores.Count}).");
        }

        var row = new Dictionary<string, object?>(OutputFields.Count + 1, StringComparer.Ordinal);
        foreach (FieldData field in OutputFields)
        {
            row[field.FieldName] = field.GetValueAsObject(rowIndex);
        }

        // Only inject the distance when no output field is literally named "score", so a user's scalar column
        // is not silently overwritten.
        if (!row.ContainsKey("score"))
        {
            row["score"] = Scores[rowIndex];
        }

        return row;
    }
}
