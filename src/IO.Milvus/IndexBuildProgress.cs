using System;

namespace IO.Milvus;

/// <summary>
/// Indicate build progress of an index.
/// </summary>
public readonly struct IndexBuildProgress : IEquatable<IndexBuildProgress>
{
    /// <summary>
    /// Construct a index progress struct.
    /// </summary>
    /// <param name="indexedRows">Indexed rows.</param>
    /// <param name="totalRows">Total rows.</param>
    public IndexBuildProgress(long indexedRows, long totalRows)
    {
        IndexedRows = indexedRows;
        TotalRows = totalRows;
    }

    /// <summary>
    /// Indexed rows.
    /// </summary>
    public long IndexedRows { get; }

    /// <summary>
    /// Total rows.
    /// </summary>
    public long TotalRows { get; }

    /// <summary>
    /// Whether the index has been fully built.
    /// </summary>
    public bool IsComplete => IndexedRows == TotalRows;

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is IndexBuildProgress other && Equals(other);

    /// <inheritdoc />
    public bool Equals(IndexBuildProgress other)
        => IndexedRows == other.IndexedRows && TotalRows == other.TotalRows;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(IndexedRows, TotalRows);

    /// <inheritdoc />
    public override string ToString() => $"Progress: {IndexedRows}/{TotalRows}";

    /// <summary>
    /// Compares two <see cref="IndexBuildProgress" /> instances for equality.
    /// </summary>
    public static bool operator ==(IndexBuildProgress left, IndexBuildProgress right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="IndexBuildProgress" /> instances for inequality.
    /// </summary>
    public static bool operator !=(IndexBuildProgress left, IndexBuildProgress right) => !(left == right);
}
