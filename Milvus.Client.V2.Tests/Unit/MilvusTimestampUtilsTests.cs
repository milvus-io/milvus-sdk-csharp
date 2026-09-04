using Xunit;

using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit;

[Trait("Category", "Unit")]
public class MilvusTimestampUtilsTests
{
    [Fact]
    public void FromDateTime_requires_utc()
    {
        Assert.Throws<ArgumentException>(() => MilvusTimestampUtils.FromDateTime(DateTime.Now));
    }

    [Fact]
    public void RoundTrip_preserves_millisecond_precision()
    {
        var dateTime = new DateTime(2026, 8, 31, 12, 34, 56, 789, DateTimeKind.Utc);

        ulong timestamp = MilvusTimestampUtils.FromDateTime(dateTime);
        DateTime roundTripped = MilvusTimestampUtils.ToDateTime(timestamp);

        Assert.Equal(DateTimeKind.Utc, roundTripped.Kind);
        Assert.Equal(dateTime, roundTripped);
    }

    [Fact]
    public void ToDateTime_zeroes_logical_bits()
    {
        // A pure millisecond-since-epoch value (logical bits zeroed) must map back to the same instant.
        ulong epochMs = (ulong)new DateTimeOffset(dateTime: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), offset: TimeSpan.Zero)
            .ToUnixTimeMilliseconds();

        DateTime converted = MilvusTimestampUtils.ToDateTime(epochMs << 18);

        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), converted);
    }
}
