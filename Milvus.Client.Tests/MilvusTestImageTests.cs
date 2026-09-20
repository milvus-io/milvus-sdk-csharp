using Xunit;

namespace Milvus.Client.Tests;

/// <summary>
/// Pins the rule that decides which tests run: every <c>[MilvusFact(MinimumVersion = "x.y")]</c> resolves through
/// <see cref="MilvusTestImage.IsAtLeast(Version)" />, so a change to the tag parsing silently changes which
/// versions get exercised. Needs no server.
/// </summary>
public class MilvusTestImageTests
{
    [Theory]
    [InlineData("milvusdb/milvus:v2.6.4", "2.6.4")]
    [InlineData("milvusdb/milvus:2.6.4", "2.6.4")]
    [InlineData("milvusdb/milvus:v2.3.22", "2.3.22")]
    [InlineData("milvusdb/milvus:v2.4", "2.4")]
    [InlineData("ghcr.io/milvus-io/milvus:v2.5.20", "2.5.20")]
    public void ParseVersion_reads_the_version_from_the_tag(string image, string expected)
        => Assert.Equal(Version.Parse(expected), MilvusTestImage.ParseVersion(image));

    [Theory]
    [InlineData("milvusdb/milvus:latest")]
    [InlineData("milvusdb/milvus")]
    [InlineData("milvusdb/milvus@sha256:1234567890abcdef")]
    [InlineData("milvusdb/milvus:v2.6.4-rc1")]
    [InlineData("milvusdb/milvus:master-20260920-abcdef")]
    public void ParseVersion_returns_null_for_tags_that_are_not_versions(string image)
        => Assert.Null(MilvusTestImage.ParseVersion(image));

    [Theory]
    [InlineData("2.6.4", "2.4", true)]
    [InlineData("2.4", "2.4", true)]
    [InlineData("2.4.23", "2.4", true)]
    [InlineData("2.3.22", "2.4", false)]
    [InlineData("2.5.20", "2.6", false)]
    public void IsAtLeast_compares_the_parsed_version(string parsed, string minimum, bool expected)
        => Assert.Equal(expected, MilvusTestImage.IsAtLeast(Version.Parse(parsed), Version.Parse(minimum)));

    [Fact]
    public void IsAtLeast_treats_an_unparseable_tag_as_new_enough()
        => Assert.True(MilvusTestImage.IsAtLeast(parsedVersion: null, new Version(2, 6)));
}

public class MilvusVersionRequirementTests
{
    [Fact]
    public void No_minimum_version_never_skips()
        => Assert.Null(MilvusVersionRequirement.GetSkipReason(null));

    [Theory]
    [InlineData("2,5")]
    [InlineData("2.5.0.0.0")]
    [InlineData("two.five")]
    public void A_malformed_minimum_version_says_which_value_is_wrong(string minimumVersion)
        => Assert.Contains(
            minimumVersion,
            Assert.Throws<ArgumentException>(() => MilvusVersionRequirement.GetSkipReason(minimumVersion)).Message);
}
