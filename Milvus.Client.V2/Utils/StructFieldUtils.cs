using System.Text.RegularExpressions;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// Shared helpers for struct fields: sub-field name normalization and PATH_REPLACE path validation.
/// </summary>
internal static class StructFieldUtils
{
    // Strips the "{structName}[" prefix and trailing "]" from a struct sub-field wire name that was added via
    // the bracketed struct[sub] path (e.g. "clips[note]" -> "note"). Sub-fields declared at create time already
    // carry their plain names and are returned unchanged. Keeps the user-visible name consistent across
    // describe, insert matching and query/search decoding regardless of what the server echoes back. Mirrors
    // pymilvus's _strip_struct_sub_field_name and the Java SDK's stripStructSubFieldName.
    internal static string StripStructSubFieldName(string structName, string fieldName)
    {
        string prefix = structName + "[";
        if (fieldName.StartsWith(prefix, StringComparison.Ordinal)
            && fieldName[fieldName.Length - 1] == ']')
        {
            return fieldName.Substring(prefix.Length, fieldName.Length - prefix.Length - 1);
        }

        return fieldName;
    }

    // Strictly matches a PATH_REPLACE path: [index] or [index][child], aligned with pymilvus. A typo must not
    // silently fall back to the whole-element interpretation.
    internal static readonly Regex StructPathReplaceRegex =
        new(@"\[(?:0|[1-9][0-9]*)\](?:\[([^\[\]\s]+)\])?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Returns whether the given path is a well-formed <c>[index]</c> or <c>[index][child]</c> path.
    /// </summary>
    internal static bool IsValidPath(string path)
    {
        Match match = StructPathReplaceRegex.Match(path);
        return match.Success && match.Length == path.Length;
    }
}
