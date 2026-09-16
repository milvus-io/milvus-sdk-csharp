using System.Text.RegularExpressions;
using Testcontainers.Milvus;
using Xunit;

namespace Milvus.Client.Tests;

/// <summary>
/// A dedicated Milvus container, entirely separate from <see cref="MilvusFixture" />'s assembly-wide
/// shared one. For tests whose failure mode can crash the server process itself (not just fail the
/// one request) -- those must never run against the container every other test also depends on.
/// Pair with <c>IClassFixture&lt;IsolatedMilvusFixture&gt;</c> on the one test class that needs it; the
/// cost is a fresh container start (tens of seconds) for that class alone, not shared with anything else.
/// </summary>
/// <remarks>
/// If the caller's test only applies from some minimum Milvus version onward (the common pattern
/// elsewhere in this suite: check <c>GetParsedMilvusVersion()</c> and return early), check
/// <see cref="IsAvailable" /> for the same purpose <em>before</em> calling <see cref="CreateClient" /> --
/// it's parsed from the <c>MILVUS_IMAGE</c> tag alone, with no container involved, so a version-gated
/// test on an older CI image never pays this fixture's container-start cost just to immediately return.
/// </remarks>
public sealed class IsolatedMilvusFixture : IAsyncLifetime
{
    private const string DefaultMilvusImage = "milvusdb/milvus:v2.6.4";

    private readonly MilvusContainer? _container;

    public IsolatedMilvusFixture()
    {
        string image = Environment.GetEnvironmentVariable("MILVUS_IMAGE") ?? DefaultMilvusImage;
        ParsedImageVersion = ParseImageVersion(image);

        // Only build (not start -- that's still InitializeAsync's job) the container when the image
        // actually looks new enough to be worth it. An unparseable tag is treated as available rather
        // than silently skipped, so a genuinely new/unexpected image format still gets exercised.
        if (ParsedImageVersion is null || ParsedImageVersion >= MinimumVersion)
        {
            _container = new MilvusBuilder(image)
                .WithEnvironment("QUOTA_AND_LIMITS_FLUSH_RATE_COLLECTION_MAX", "-1")
                .Build();
        }
    }

    /// <summary>
    /// The minimum Milvus version this fixture bothers starting a container for. Tests using this
    /// fixture are all about behavior introduced in the 2.6 line so far; bump this if that changes.
    /// </summary>
    private static readonly Version MinimumVersion = new(2, 6);

    /// <summary>
    /// The Milvus version parsed from the <c>MILVUS_IMAGE</c> tag (e.g. <c>v2.6.4</c> -&gt;
    /// <c>2.6.4</c>), or <see langword="null" /> if the tag doesn't look like a version at all.
    /// </summary>
    public Version? ParsedImageVersion { get; }

    /// <summary>
    /// Whether this fixture actually started a container. False when <see cref="ParsedImageVersion" /> is
    /// parsed and below the version this fixture requires -- check this (or compare
    /// <see cref="ParsedImageVersion" /> directly) before <see cref="CreateClient" /> instead of starting
    /// a client only to immediately discard it.
    /// </summary>
    public bool IsAvailable => _container is not null;

    public MilvusClient CreateClient()
        => _container is not null
            ? new MilvusClient(_container.Hostname, "root", "Milvus", _container.GetMappedPublicPort(MilvusBuilder.MilvusGrpcPort), ssl: false)
            : throw new InvalidOperationException(
                $"No container was started for this fixture (MILVUS_IMAGE parsed as {ParsedImageVersion?.ToString() ?? "unparseable"}, " +
                $"below the {MinimumVersion} this fixture requires) -- check {nameof(IsAvailable)} first.");

    public ValueTask InitializeAsync() => _container is null ? default : new ValueTask(_container.StartAsync());
    public ValueTask DisposeAsync() => _container is null ? default : _container.DisposeAsync();

    private static Version? ParseImageVersion(string image)
    {
        // e.g. "milvusdb/milvus:v2.6.4" -> "2.6.4"
        Match match = Regex.Match(image, @":v?(?<version>\d+(\.\d+){1,3})$");
        return match.Success && System.Version.TryParse(match.Groups["version"].Value, out Version? version)
            ? version
            : null;
    }
}
