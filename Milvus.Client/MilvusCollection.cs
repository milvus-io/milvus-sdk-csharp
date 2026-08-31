using System.Globalization;
using System.Text;

namespace Milvus.Client;

/// <summary>
/// Represents a Milvus collection, and is the starting point for all operations involving one.
/// </summary>
#pragma warning disable CA1711
public partial class MilvusCollection
#pragma warning restore CA1711
{
    private readonly MilvusClient _client;

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string Name { get; private set; }

    internal MilvusCollection(MilvusClient client, string collectionName)
        => (_client, Name) = (client, collectionName);

    #region Utilities

    /// <summary>
    /// Builds the search <c>params</c> JSON, folding the strongly-typed range-search boundaries in
    /// alongside <see cref="SearchParameters.ExtraParameters" />. The typed properties win if the same
    /// key is also present in the dictionary.
    /// </summary>
    private static string CombineSearchParams(SearchParameters parameters)
    {
        if (parameters.Radius is null && parameters.RangeFilter is null)
        {
            return Combine(parameters.ExtraParameters);
        }

        Dictionary<string, string> combined = new(parameters.ExtraParameters);

        if (parameters.Radius is not null)
        {
            combined[Constants.Radius] = parameters.Radius.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (parameters.RangeFilter is not null)
        {
            combined[Constants.RangeFilter] = parameters.RangeFilter.Value.ToString(CultureInfo.InvariantCulture);
        }

        return Combine(combined);
    }

    private static string Combine(IDictionary<string, string> parameters)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append('{');

        int index = 0;
        foreach (KeyValuePair<string, string> parameter in parameters)
        {
            stringBuilder
                .Append('"')
                .Append(parameter.Key)
                .Append("\":")
                .Append(parameter.Value);

            if (index++ != parameters.Count - 1)
            {
                stringBuilder.Append(", ");
            }
        }

        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }

    #endregion Utilities
}
