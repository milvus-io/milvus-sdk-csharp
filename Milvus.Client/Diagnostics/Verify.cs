using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Milvus.Client;

internal static class Verify
{
    internal static void NotNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument is null)
        {
            ThrowNullException(paramName);
        }

        [DoesNotReturn]
        static void ThrowNullException(string paramName) => throw new ArgumentNullException(paramName);
    }

    internal static void NotNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        NotNull(argument, paramName);

        if (string.IsNullOrWhiteSpace(argument))
        {
            ThrowWhiteSpaceException(paramName);
        }

        [DoesNotReturn]
        static void ThrowWhiteSpaceException(string paramName)
            => throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
    }

    internal static void NotNullOrEmpty<T>([NotNull] IList<T>? argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        NotNull(argument);

        if (argument.Count == 0)
        {
            ThrowEmptyException(paramName);
        }

        static void ThrowEmptyException(string paramName)
            => throw new ArgumentException("The collection cannot empty", paramName);
    }

    internal static void GreaterThan(long value, long other, [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value <= other)
        {
            ThrowLessThanOrEqual(other, paramName);
        }

        static void ThrowLessThanOrEqual(long other, string paramName) =>
            throw new ArgumentOutOfRangeException(paramName, $"The value must be greater than {other}.");
    }

    internal static void GreaterThanOrEqualTo(long value, long other, [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value < other)
        {
            ThrowLessThan(other, paramName);
        }

        static void ThrowLessThan(long other, string paramName) =>
            throw new ArgumentOutOfRangeException(paramName, $"The value must be greater than or equal to {other}.");
    }
}
