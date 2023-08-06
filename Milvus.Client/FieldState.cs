namespace Milvus.Client;

/// <summary>
/// Milvus field state.
/// </summary>
public enum FieldState
{
    /// <summary>
    /// The field has been created.
    /// </summary>
    FieldCreated = Grpc.FieldState.FieldCreated,

    /// <summary>
    /// The field is in the process of being created.
    /// </summary>
    FieldCreating = Grpc.FieldState.FieldCreating,

    /// <summary>
    /// The field is in the process of being dropped.
    /// </summary>
    FieldDropping = Grpc.FieldState.FieldDropping,

    /// <summary>
    /// The field has been dropped.
    /// </summary>
    FieldDropped = Grpc.FieldState.FieldDropped,

    /// <summary>
    /// The field state is unknown or not yet created.
    /// </summary>
    Unknown = int.MaxValue
}
