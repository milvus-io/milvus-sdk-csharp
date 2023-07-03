using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace IO.Milvus.Diagnostics;

internal static class Verify
{
    internal static void NotNull([NotNull] object argument, [CallerArgumentExpression(nameof(argument))] string paramName = null)
    {
        if (argument is null)
        {
            ThrowNullException(paramName);
        }

        static void ThrowNullException(string paramName) =>
            throw new ArgumentNullException(paramName);
    }

    internal static void NotNullOrWhiteSpace([NotNull] string argument, [CallerArgumentExpression(nameof(argument))] string paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            ThrowNullOrWhiteSpaceException(argument, paramName);
        }

        static void ThrowNullOrWhiteSpaceException(string argument, string paramName)
        {
            NotNull(argument, paramName);
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
        }
    }

    internal static void NotNullOrEmpty<T>(IList<T> argument, [CallerArgumentExpression(nameof(argument))] string paramName = null)
    {
        if (argument is null || argument.Count == 0)
        {
            ThrowNullOrEmptyException(argument, paramName);
        }

        static void ThrowNullOrEmptyException(object argument, string paramName)
        {
            NotNull(argument, paramName);
            throw new ArgumentException("The collection cannot empty", paramName);
        }
    }

    internal static void GreaterThan(long value, long other, [CallerArgumentExpression(nameof(value))] string paramName = null)
    {
        if (value <= other)
        {
            ThrowLessThanOrEqual(other, paramName);
        }

        static void ThrowLessThanOrEqual(long other, string paramName) =>
            throw new ArgumentOutOfRangeException(paramName, $"The value must be greater than {other}.");
    }

    internal static void GreaterThanOrEqualTo(long value, long other, [CallerArgumentExpression(nameof(value))] string paramName = null)
    {
        if (value < other)
        {
            ThrowLessThan(other, paramName);
        }

        static void ThrowLessThan(long other, string paramName) =>
            throw new ArgumentOutOfRangeException(paramName, $"The value must be greater than or equal to {other}.");
    }

    public static void ValidUrl(string name, string url, bool requireHttps, bool allowReservedIp, bool allowQuery)
    {
        static bool IsReservedIpAddress(string host) =>
            host.StartsWith("0.", StringComparison.Ordinal) ||
            host.StartsWith("10.", StringComparison.Ordinal) ||
            host.StartsWith("127.", StringComparison.Ordinal) ||
            host.StartsWith("169.254.", StringComparison.Ordinal) ||
            host.StartsWith("192.0.0.", StringComparison.Ordinal) ||
            host.StartsWith("192.88.99.", StringComparison.Ordinal) ||
            host.StartsWith("192.168.", StringComparison.Ordinal) ||
            host.StartsWith("255.255.255.255", StringComparison.Ordinal);

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException($"The {name} is empty", name);
        }

        if (requireHttps && url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The {name} `{url}` is not safe, it must start with https://", name);
        }

        if (requireHttps && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The {name} `{url}` is incomplete, enter a valid URL starting with 'https://", name);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || string.IsNullOrEmpty(uri.Host))
        {
            throw new ArgumentException($"The {name} `{url}` is not valid", name);
        }

        if (requireHttps && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException($"The {name} `{url}` is not safe, it must start with https://", name);
        }

        if (!allowReservedIp && (uri.IsLoopback || IsReservedIpAddress(uri.Host)))
        {
            throw new ArgumentException($"The {name} `{url}` is not safe, it cannot point to a reserved network address", name);
        }

        if (!allowQuery && !string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException($"The {name} `{url}` is not valid, it cannot contain query parameters", name);
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException($"The {name} `{url}` is not valid, it cannot contain URL fragments", name);
        }
    }
}
