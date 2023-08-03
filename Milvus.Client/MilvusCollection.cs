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
