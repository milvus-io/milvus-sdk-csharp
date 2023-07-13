using System.Collections.Generic;

namespace IO.Milvus;

/// <summary>
/// Collection Schema
/// </summary>
public sealed class CollectionSchema
{
    /// <summary>
    /// Auto id
    /// </summary>
    /// <remarks>
    /// deprecated later, keep compatible with c++ part now
    /// </remarks>
    public bool AutoId { get; set; }

    /// <summary>
    /// Collection description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fields
    /// </summary>
    /// <remarks>
    /// Array of <see cref="FieldType"/>
    /// </remarks>
    public required IList<FieldType> Fields { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Enable dynamic field.
    /// </summary>
    /// <remarks>
    /// <see href="https://milvus.io/docs/dynamic_schema.md#JSON-a-new-data-type"/>
    /// </remarks>
    public bool EnableDynamicField { get; set; }

    /// <summary>
    /// Return string value of <see cref="CollectionSchema"/>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
        => $"CollectionSchema: {{{nameof(AutoId)}: {AutoId}, {nameof(Description)}, {Description}, {nameof(Fields)}: {Fields?.Count}}}";
}