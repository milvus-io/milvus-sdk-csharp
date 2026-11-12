namespace Milvus.Client.V2.Utils;

/// <summary>
/// Helpers for struct sub-field names, mirroring pymilvus's <c>_strip_struct_sub_field_name</c> and the
/// Java SDK's <c>stripStructSubFieldName</c>.
/// </summary>
internal static class StructFieldNames
{
    // Strips the "{structName}[" prefix and trailing "]" from a struct sub-field wire name that was added via
    // the bracketed struct[sub] path (e.g. "clips[note]" -> "note"). Sub-fields declared at create time already
    // carry their plain names and are returned unchanged. Keeps the user-visible name consistent across
    // describe, insert matching and query/search decoding regardless of what the server echoes back.
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
}
