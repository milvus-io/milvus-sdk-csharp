namespace IO.Milvus;

/// <summary>
/// Milvus field state.
/// </summary>
public enum MilvusFieldState
{
    /// <summary>
    /// FieldCreated.
    /// </summary>
    FieldCreated = 0,

    /// <summary>
    /// FieldCreating.
    /// </summary>
    FieldCreating = 1,

    /// <summary>
    /// FieldDropping.
    /// </summary>
    FieldDropping = 2,

    /// <summary>
    /// FieldDropped
    /// </summary>
    FieldDropped = 3,
}