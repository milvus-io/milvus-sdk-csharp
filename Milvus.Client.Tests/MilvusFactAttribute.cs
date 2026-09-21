using System.Runtime.CompilerServices;
using Xunit;

namespace Milvus.Client.Tests;

/// <summary>
/// A <see cref="FactAttribute" /> for tests that only apply from some Milvus version onward. On an older image the
/// test is reported as skipped, rather than returning early and showing up as passed.
/// </summary>
/// <remarks>
/// The version comes from the image tag (see <see cref="MilvusTestImage" />), since xunit evaluates this during
/// discovery, before any container has been started.
/// </remarks>
public sealed class MilvusFactAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1)
    : FactAttribute(sourceFilePath, sourceLineNumber)
{
    private string? _minimumVersion;

    /// <summary>The minimum Milvus version the test requires, e.g. <c>"2.5"</c>.</summary>
    public string? MinimumVersion
    {
        get => _minimumVersion;
        set
        {
            _minimumVersion = value;
            Skip ??= MilvusVersionRequirement.GetSkipReason(value);
        }
    }
}

/// <summary>
/// A <see cref="TheoryAttribute" /> for tests that only apply from some Milvus version onward. See
/// <see cref="MilvusFactAttribute" />.
/// </summary>
public sealed class MilvusTheoryAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1)
    : TheoryAttribute(sourceFilePath, sourceLineNumber)
{
    private string? _minimumVersion;

    /// <summary>The minimum Milvus version the test requires, e.g. <c>"2.5"</c>.</summary>
    public string? MinimumVersion
    {
        get => _minimumVersion;
        set
        {
            _minimumVersion = value;
            Skip ??= MilvusVersionRequirement.GetSkipReason(value);
        }
    }
}

internal static class MilvusVersionRequirement
{
    public static string? GetSkipReason(string? minimumVersion)
    {
        if (minimumVersion is null)
        {
            return null;
        }

        if (!Version.TryParse(minimumVersion, out Version? parsedMinimumVersion))
        {
            throw new ArgumentException(
                $"'{minimumVersion}' is not a valid MinimumVersion; expected a version such as \"2.5\".",
                nameof(minimumVersion));
        }

        return MilvusTestImage.IsAtLeast(parsedMinimumVersion)
            ? null
            : $"Requires Milvus {minimumVersion} or later, but the tests run against {MilvusTestImage.Name}.";
    }
}
