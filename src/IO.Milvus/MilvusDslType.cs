namespace IO.Milvus;

/// <summary>
/// Dsl type
/// </summary>
public enum MilvusDslType
{
    /// <summary>
    /// 
    /// </summary>
    Dsl = 0,

    /// <summary>
    /// A predicate expression outputs a boolean value.
    /// </summary>
    /// <remarks>
    /// <see href="https://milvus.io/docs/boolean.md"/>
    /// </remarks>
    BoolExprV1 = 1,
}
