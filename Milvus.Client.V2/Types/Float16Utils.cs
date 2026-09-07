using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

/// <summary>
/// Utilities for converting between <see cref="float" /> and the half-precision / bfloat16 bit patterns used by
/// <c>Float16Vector</c> and <c>BFloat16Vector</c> fields.
/// </summary>
/// <remarks>
/// On net8.0 the <c>Half</c> type and <c>BitConverter</c> helpers are used; on
/// netstandard2.0 / net462 (no native <c>Half</c>) the IEEE-754 half-precision conversion is
/// implemented in terms of <see cref="uint" /> bit patterns, so the behavior is identical everywhere.
/// </remarks>
public static class Float16Utils
{
    /// <summary>
    /// Converts a <see cref="float" /> to a float16 value represented as <see cref="ushort" /> bit pattern.
    /// </summary>
    public static ushort FloatToFp16(float value)
    {
#if NET8_0_OR_GREATER
        return BitConverter.HalfToUInt16Bits((Half)value);
#else
        return FloatToHalfBits(value);
#endif
    }

    /// <summary>
    /// Converts a float16 value (as a <see cref="ushort" /> bit pattern) to a <see cref="float" />.
    /// </summary>
    public static float Fp16ToFloat(ushort bits)
    {
#if NET8_0_OR_GREATER
        return (float)BitConverter.UInt16BitsToHalf(bits);
#else
        return HalfBitsToFloat(bits);
#endif
    }

    /// <summary>
    /// Converts a <see cref="float" /> to a bfloat16 value represented as <see cref="ushort" /> bit pattern
    /// (the top 16 bits of the 32-bit float, with round-to-nearest-even).
    /// </summary>
    public static ushort FloatToBf16(float value)
    {
        uint bits = SingleToUInt32Bits(value);
        uint lsb = (bits >> 16) & 1;
        bits += 0x7FFF + lsb;
        return (ushort)(bits >> 16);
    }

    /// <summary>
    /// Converts a bfloat16 value (as a <see cref="ushort" /> bit pattern) to a <see cref="float" />.
    /// </summary>
    public static float Bf16ToFloat(ushort bits)
        => UInt32BitsToSingle((uint)bits << 16);

    /// <summary>
    /// Converts a vector of <see cref="float" /> values to a float16 vector of <see cref="ushort" /> bit patterns.
    /// </summary>
    public static ushort[] F32VectorToFp16(IReadOnlyList<float> values)
    {
        Verify.NotNull(values);
        var result = new ushort[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            result[i] = FloatToFp16(values[i]);
        }
        return result;
    }

    /// <summary>
    /// Converts a float16 vector of <see cref="ushort" /> bit patterns to a <see cref="float" /> vector.
    /// </summary>
    public static float[] Fp16VectorToF32(IReadOnlyList<ushort> bits)
    {
        Verify.NotNull(bits);
        var result = new float[bits.Count];
        for (int i = 0; i < bits.Count; i++)
        {
            result[i] = Fp16ToFloat(bits[i]);
        }
        return result;
    }

    /// <summary>
    /// Converts a vector of <see cref="float" /> values to a bfloat16 vector of <see cref="ushort" /> bit patterns.
    /// </summary>
    public static ushort[] F32VectorToBf16(IReadOnlyList<float> values)
    {
        Verify.NotNull(values);
        var result = new ushort[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            result[i] = FloatToBf16(values[i]);
        }
        return result;
    }

    /// <summary>
    /// Converts a bfloat16 vector of <see cref="ushort" /> bit patterns to a <see cref="float" /> vector.
    /// </summary>
    public static float[] Bf16VectorToF32(IReadOnlyList<ushort> bits)
    {
        Verify.NotNull(bits);
        var result = new float[bits.Count];
        for (int i = 0; i < bits.Count; i++)
        {
            result[i] = Bf16ToFloat(bits[i]);
        }
        return result;
    }

    private static ushort FloatToHalfBits(float value)
    {
        uint bits = SingleToUInt32Bits(value);
        uint sign = (bits >> 16) & 0x8000;
        int exp = (int)((bits >> 23) & 0xFF) - 127 + 15;
        uint mantissa = bits & 0x7FFFFF;

        if (((bits >> 23) & 0xFF) == 0xFF)
        {
            // Inf / NaN
            return (ushort)(sign | 0x7C00u | (mantissa != 0 ? 0x200u : 0u));
        }

        if (exp >= 31)
        {
            // Overflow -> infinity
            return (ushort)(sign | 0x7C00);
        }

        if (exp <= 0)
        {
            // Subnormal or zero
            if (exp < -10)
            {
                return (ushort)sign;
            }

            mantissa |= 0x800000;
            uint shift = (uint)(14 - exp);
            uint halfMantissa = mantissa >> (int)shift;
            uint remainder = mantissa & ((1u << (int)shift) - 1);
            if (remainder > (1u << (int)(shift - 1)) ||
                (remainder == (1u << (int)(shift - 1)) && (halfMantissa & 1) != 0))
            {
                halfMantissa++;
            }
            return (ushort)(sign | halfMantissa);
        }

        ushort result = (ushort)(sign | ((uint)exp << 10) | (mantissa >> 13));
        uint roundBit = (mantissa >> 12) & 1;
        uint rest = mantissa & 0xFFF;
        if (rest > 0x800 || (rest == 0x800 && roundBit != 0))
        {
            result++;
        }
        return result;
    }

    private static float HalfBitsToFloat(ushort bits)
    {
        uint sign = (uint)(bits & 0x8000) << 16;
        uint exp = (uint)((bits >> 10) & 0x1F);
        uint mantissa = (uint)(bits & 0x3FF);

        if (exp == 0)
        {
            if (mantissa == 0)
            {
                return UInt32BitsToSingle(sign);
            }

            // Subnormal
            exp = 1;
            while ((mantissa & 0x400) == 0)
            {
                mantissa <<= 1;
                exp--;
            }
            mantissa &= 0x3FF;
            return UInt32BitsToSingle(sign | ((exp + 112) << 23) | (mantissa << 13));
        }

        if (exp == 0x1F)
        {
            // Inf / NaN
            return UInt32BitsToSingle(sign | 0x7F800000 | (mantissa << 13));
        }

        return UInt32BitsToSingle(sign | ((exp + 112) << 23) | (mantissa << 13));
    }

    private static uint SingleToUInt32Bits(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static float UInt32BitsToSingle(uint bits)
    {
        byte[] bytes = BitConverter.GetBytes(bits);
        return BitConverter.ToSingle(bytes, 0);
    }
}
