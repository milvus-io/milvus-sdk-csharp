using System.Globalization;

namespace Milvus.Client;

/// <summary>
/// A brain floating point (bfloat16) value: the upper 16 bits of an IEEE-754 single-precision float.
/// Available since Milvus v2.4.
/// </summary>
/// <remarks>
/// <para>
/// bfloat16 keeps the full 8-bit exponent of <see cref="float" /> but truncates the mantissa to 7 bits.
/// It therefore covers the same range as <see cref="float" /> with less precision, which is why it is
/// commonly used for embeddings.
/// </para>
/// <para>
/// .NET has no built-in bfloat16 type — <c>System.Half</c> is IEEE-754 binary16, a different format
/// with a 5-bit exponent — so this struct provides the representation. Conversion from
/// <see cref="float" /> is lossy and rounds to nearest, ties to even; conversion back is exact.
/// </para>
/// </remarks>
public readonly struct BFloat16 : IEquatable<BFloat16>, IComparable<BFloat16>
{
    private readonly ushort _bits;

    private BFloat16(ushort bits) => _bits = bits;

    /// <summary>
    /// Creates a value from its raw 16-bit representation.
    /// </summary>
    /// <param name="bits">The raw bfloat16 bit pattern.</param>
    public static BFloat16 FromBits(ushort bits) => new(bits);

    /// <summary>
    /// Returns the raw 16-bit representation of this value.
    /// </summary>
    public ushort ToBits() => _bits;

    /// <summary>
    /// Converts a <see cref="float" /> to bfloat16, rounding to nearest with ties to even.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static BFloat16 FromSingle(float value)
    {
        if (float.IsNaN(value))
        {
            // Canonical quiet NaN; the generic rounding below could otherwise turn a NaN into an
            // infinity by carrying into the exponent.
            return new BFloat16(0x7FC0);
        }

        uint bits = SingleToUInt32Bits(value);

        // Round to nearest, ties to even, on the bit being shifted out.
        uint lsb = (bits >> 16) & 1;
        bits += 0x7FFF + lsb;

        return new BFloat16((ushort)(bits >> 16));
    }

    /// <summary>
    /// Converts this value to a <see cref="float" />. The conversion is exact.
    /// </summary>
    public float ToSingle() => UInt32BitsToSingle((uint)_bits << 16);

    /// <summary>
    /// Converts a <see cref="float" /> to bfloat16. Lossy; see <see cref="FromSingle" />.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator BFloat16(float value) => FromSingle(value);

    /// <summary>
    /// Converts a bfloat16 to <see cref="float" />. Exact.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator float(BFloat16 value) => value.ToSingle();

    /// <inheritdoc />
    public bool Equals(BFloat16 other) => _bits == other._bits;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BFloat16 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _bits.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(BFloat16 other) => ToSingle().CompareTo(other.ToSingle());

    /// <summary>
    /// Determines whether two values have the same bit pattern.
    /// </summary>
    public static bool operator ==(BFloat16 left, BFloat16 right) => left.Equals(right);

    /// <summary>
    /// Determines whether two values have different bit patterns.
    /// </summary>
    public static bool operator !=(BFloat16 left, BFloat16 right) => !left.Equals(right);

    /// <summary>
    /// Determines whether one value is less than another.
    /// </summary>
    public static bool operator <(BFloat16 left, BFloat16 right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one value is less than or equal to another.
    /// </summary>
    public static bool operator <=(BFloat16 left, BFloat16 right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one value is greater than another.
    /// </summary>
    public static bool operator >(BFloat16 left, BFloat16 right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one value is greater than or equal to another.
    /// </summary>
    public static bool operator >=(BFloat16 left, BFloat16 right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => ToSingle().ToString(CultureInfo.InvariantCulture);

    // BitConverter.SingleToUInt32Bits is not available on netstandard2.0/net462, so reinterpret
    // directly. AllowUnsafeBlocks is already enabled for this project.
    private static unsafe uint SingleToUInt32Bits(float value) => *(uint*)&value;

    private static unsafe float UInt32BitsToSingle(uint value) => *(float*)&value;
}
