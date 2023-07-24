using Xunit;

namespace Milvus.Client.Tests;

public class TimestampUtilsTests
{
    [Fact]
    public void ToDateTime_returns_utc_DateTime()
        => Assert.Equal(DateTimeKind.Utc, MilvusTimestampUtils.ToDateTime(0).Kind);

    [Fact]
    public void FromDateTime_with_non_utc_DateTime_throws()
        => Assert.Throws<ArgumentException>(() =>
            MilvusTimestampUtils.FromDateTime(new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Local)));

    [Fact]
    public void Can_roundtrip_down_to_milliseconds()
    {
        var dateTimeWithMilliseconds = new DateTime(2020, 1, 1, 12, 0, 0, 123, DateTimeKind.Utc);
        var dateTimeWithMicroseconds = new DateTime(2020, 1, 1, 12, 0, 0, 123, 456, DateTimeKind.Utc);

        ulong milvusTimestamp = MilvusTimestampUtils.FromDateTime(dateTimeWithMicroseconds);
        Assert.Equal(dateTimeWithMilliseconds, MilvusTimestampUtils.ToDateTime(milvusTimestamp));
    }
}
