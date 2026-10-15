using Xunit;

using Milvus.Client.V2;

namespace Milvus.Client.V2.Tests.Unit;

[Trait("Category", "Unit")]
public class Float16UtilsTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(-1f)]
    [InlineData(0.5f)]
    [InlineData(3.1415927f)]
    [InlineData(100.5f)]
    [InlineData(-123.456f)]
    [InlineData(65504f)]      // max finite half
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Fp16_roundtrip_approximates(float value)
    {
        ushort bits = Float16Utils.FloatToFp16(value);
        float back = Float16Utils.Fp16ToFloat(bits);

        if (float.IsInfinity(value))
        {
            Assert.Equal(float.IsPositiveInfinity(value), float.IsPositiveInfinity(back));
            return;
        }

        // FP16 has a 10-bit mantissa (~3 significant digits); allow ~0.1% relative error.
        AssertWithinRelative(value, back, tolerance: 0.001f);
    }

    [Fact]
    public void Bf16_roundtrip_approximates()
    {
        foreach (float value in new[] { 0f, 1f, -1f, 0.5f, 3.1415927f, 100.5f, -123.456f })
        {
            ushort bits = Float16Utils.FloatToBf16(value);
            float back = Float16Utils.Bf16ToFloat(bits);

            // BF16 has a 7-bit mantissa (~2 significant digits); allow ~0.5% relative error.
            AssertWithinRelative(value, back, tolerance: 0.005f);
        }
    }

    [Fact]
    public void Bf16_preserves_infinity_and_nan()
    {
        // NaN bit patterns with the low 16 bits set used to wrap to +0.0 / -0.0 via the rounding add.
        foreach (float value in new[] { float.PositiveInfinity, float.NegativeInfinity, BitConverter.Int32BitsToSingle(unchecked((int)0x7FFFFFFF)) })
        {
            ushort bits = Float16Utils.FloatToBf16(value);
            float back = Float16Utils.Bf16ToFloat(bits);

            if (float.IsNaN(value))
            {
                Assert.True(float.IsNaN(back), $"NaN value {value} should stay NaN, got {back}");
            }
            else
            {
                Assert.Equal(float.IsPositiveInfinity(value), float.IsPositiveInfinity(back));
            }
        }
    }

    [Fact]
    public void Bf16_preserves_signaling_nan_payload()
    {
        // A signaling NaN whose payload lives only in the low 16 bits (0x7F800001) would truncate to +Inf
        // without preserving a payload bit in the bf16 mantissa.
        float sNaN = BitConverter.Int32BitsToSingle(unchecked((int)0x7F800001));

        ushort bits = Float16Utils.FloatToBf16(sNaN);
        float back = Float16Utils.Bf16ToFloat(bits);

        Assert.True(float.IsNaN(back), $"signaling NaN {sNaN} should stay NaN, got {back}");
    }

    [Fact]
    public void Vector_conversions_match_scalar()
    {
        float[] values = { 0f, 1.5f, -2.5f, 3.75f, 100f };
        ushort[] fp16 = Float16Utils.F32VectorToFp16(values);
        float[] back = Float16Utils.Fp16VectorToF32(fp16);
        Assert.Equal(values.Length, back.Length);

        ushort[] bf16 = Float16Utils.F32VectorToBf16(values);
        float[] bf16Back = Float16Utils.Bf16VectorToF32(bf16);
        Assert.Equal(values.Length, bf16Back.Length);
    }

    [Theory]
    [InlineData(1.1f, 0x3C66)]      // tie-rounding of the 13-bit remainder
    [InlineData(65516f, 0x7BFF)]    // rounds down to max finite half (65504)
    [InlineData(65520f, 0x7C00)]    // rounds up to +Inf
    [InlineData(-65516f, 0xFBFF)]
    [InlineData(65504f, 0x7BFF)]    // max finite half
    public void Fp16_rounds_to_nearest_even(float value, ushort expectedBits)
    {
        Assert.Equal(expectedBits, Float16Utils.FloatToFp16(value));
    }

    private static void AssertWithinRelative(float expected, float actual, float tolerance)
    {
        float scale = Math.Max(1f, Math.Abs(expected));
        Assert.True(Math.Abs(expected - actual) <= scale * tolerance,
            $"Expected {expected}, got {actual} (relative error {Math.Abs(expected - actual) / scale:E2})");
    }
}
