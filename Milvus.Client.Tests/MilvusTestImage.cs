using System.Text.RegularExpressions;

namespace Milvus.Client.Tests;

/// <summary>
/// The Milvus Docker image the test containers run, taken from the <c>MILVUS_IMAGE</c> environment variable
/// (set per Milvus version by CI), falling back to a default.
/// </summary>
public static class MilvusTestImage
{
    private const string DefaultImage = "milvusdb/milvus:v2.6.4";

    public static string Name { get; } = Environment.GetEnvironmentVariable("MILVUS_IMAGE") ?? DefaultImage;

    /// <summary>
    /// The Milvus version parsed from the image tag (e.g. <c>v2.6.4</c> -&gt; <c>2.6.4</c>), or
    /// <see langword="null" /> if the tag doesn't look like a version at all.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Utils.GetParsedMilvusVersion" />, this is known without a running server, so it can be
    /// used where no container exists yet (e.g. test discovery).
    /// </remarks>
    public static Version? ParsedVersion { get; } = ParseVersion(Name);

    /// <summary>
    /// Whether the image is at least <paramref name="minimumVersion" />. An unparseable tag counts as new enough,
    /// so a genuinely new/unexpected image format still gets exercised rather than silently skipped.
    /// </summary>
    public static bool IsAtLeast(Version minimumVersion)
        => IsAtLeast(ParsedVersion, minimumVersion);

    /// <summary>The rule behind <see cref="IsAtLeast(Version)" />, over an explicit version so it can be tested.</summary>
    internal static bool IsAtLeast(Version? parsedVersion, Version minimumVersion)
        => parsedVersion is null || parsedVersion >= minimumVersion;

    internal static Version? ParseVersion(string image)
    {
        // e.g. "milvusdb/milvus:v2.6.4" -> "2.6.4"
        Match match = Regex.Match(image, @":v?(?<version>\d+(\.\d+){1,3})$");
        return match.Success && Version.TryParse(match.Groups["version"].Value, out Version? version)
            ? version
            : null;
    }
}
