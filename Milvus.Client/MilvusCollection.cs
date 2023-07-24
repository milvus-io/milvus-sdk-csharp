using System.Diagnostics;
using System.Text;

namespace Milvus.Client;

/// <summary>
/// Represents a Milvus collection, and is the starting point for all operations involving one.
/// </summary>
public partial class MilvusCollection
{
    private readonly MilvusClient _client;

    /// <summary>
    /// The name of the database in which this collection is located.
    /// <c>null</c> if the collection is in the default database,
    /// </summary>
    public string? DatabaseName { get; }

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string Name { get; private set; }

    internal MilvusCollection(MilvusClient client, string collectionName, string? databaseName)
        => (_client, Name, DatabaseName) = (client, collectionName, databaseName);

    #region Utilities

    private static async Task Poll<TProgress>(
        Func<Task<(bool, TProgress)>> pollingAction,
        string timeoutExceptionMessage,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        IProgress<TProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        waitingInterval ??= TimeSpan.FromMilliseconds(500);

        Stopwatch? stopWatch = timeout is null ? null : Stopwatch.StartNew();

        while (true)
        {
            (bool isComplete, TProgress currentProgress) = await pollingAction().ConfigureAwait(false);

            progress?.Report(currentProgress);

            if (isComplete)
            {
                return;
            }

            if (stopWatch is not null && stopWatch.Elapsed + waitingInterval.Value >= timeout)
            {
                throw new TimeoutException(timeoutExceptionMessage);
            }

            await Task.Delay(waitingInterval.Value, cancellationToken).ConfigureAwait(false);
        }
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
