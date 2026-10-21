using System.Text.Json;

using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rls;

/// <summary>
/// Represents a request to incrementally upsert a non-empty mapping of principal tags for a collection.
/// </summary>
/// <remarks>
/// Values may be strings, finite numbers, or one-dimensional arrays of strings or numbers. Keys omitted from
/// this call retain their values.
/// </remarks>
public sealed class SetRLSPrincipalTagsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection the principal tags belong to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the principal whose tags are upserted.
    /// </summary>
    public string PrincipalName { get; set; } = "";

    /// <summary>
    /// The principal tags to upsert. Values may be strings, finite numbers, or one-dimensional arrays of
    /// strings or numbers.
    /// </summary>
    public IDictionary<string, object?> Tags { get; } = new Dictionary<string, object?>();

    internal Grpc.SetRLSPrincipalTagsRequest ToGrpcSetRLSPrincipalTagsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PrincipalName);

        ValidateTags();

        var request = new Grpc.SetRLSPrincipalTagsRequest
        {
            CollectionName = CollectionName,
            PrincipalName = PrincipalName,
            Tags = JsonSerializer.Serialize(Tags)
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }

    private void ValidateTags()
    {
        if (Tags.Count == 0)
        {
            throw new ArgumentException("RLS principal tags must be a non-empty mapping.", nameof(Tags));
        }

        foreach (KeyValuePair<string, object?> entry in Tags)
        {
            object? value = entry.Value;
            if (value is System.Collections.IEnumerable list and not string and not System.Collections.IDictionary)
            {
                foreach (object? item in list)
                {
                    if (!IsValidTagValue(item))
                    {
                        throw new ArgumentException(
                            "RLS principal tag values must be strings, finite numbers, or arrays of them.", nameof(Tags));
                    }
                }
            }
            else if (!IsValidTagValue(value))
            {
                throw new ArgumentException(
                    "RLS principal tag values must be strings, finite numbers, or arrays of them.", nameof(Tags));
            }
        }
    }

    private static bool IsValidTagValue(object? value)
        => value switch
        {
            string => true,
            byte or sbyte or short or ushort or int or uint or long or ulong or decimal => true,
            float f => !float.IsNaN(f) && !float.IsInfinity(f),
            double d => !double.IsNaN(d) && !double.IsInfinity(d),
            // BigInteger is deliberately excluded: System.Text.Json has no BigInteger converter on the SDK's
            // target frameworks (support arrived only in .NET 9), so accepting it here would pass validation
            // and then throw NotSupportedException during JSON serialization.
            _ => false
        };
}
