namespace Milvus.Client.V2.Utils;

/// <summary>
/// Enum helpers that avoid the reflection-based <c>Enum.GetValues(Type)</c>, which the NativeAOT/trimmed
/// analyzers flag with IL3050 (RequiresDynamicCode). The generic overload exists only on .NET 5+; older
/// targets fall back to the non-generic form, which is never AOT-published.
/// </summary>
internal static class EnumUtil
{
    internal static TEnum[] GetValues<TEnum>()
        where TEnum : struct, Enum
    {
#if NET5_0_OR_GREATER
        return Enum.GetValues<TEnum>();
#else
        return (TEnum[])Enum.GetValues(typeof(TEnum));
#endif
    }
}
