namespace Milvus.Client;

/// <summary>
/// Milvus field state.
/// </summary>
public enum MilvusFieldState
{
    /// <summary>
    /// The field has been created.
    /// </summary>
    FieldCreated = FieldState.FieldCreated,

    /// <summary>
    /// The field is in the process of being created.
    /// </summary>
    FieldCreating = FieldState.FieldCreating,

    /// <summary>
    /// The field is in the process of being dropped.
    /// </summary>
    FieldDropping = FieldState.FieldDropping,

    /// <summary>
    /// The field has been dropped.
    /// </summary>
    FieldDropped = FieldState.FieldDropped,

    /// <summary>
    /// The field state is unknown or not yet created.
    /// </summary>
    Unknown = int.MaxValue
}
