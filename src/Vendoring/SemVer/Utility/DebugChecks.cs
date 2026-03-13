using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Primitives;
using Semver.Parsing;

namespace Semver.Utility;

/// <summary>
/// The <see cref="DebugChecks"/> class allows for the various conditional checks done only in
/// debug builds to not count against the code coverage metrics.
/// </summary>
/// <remarks>When using a preprocessor conditional block, the contained lines are not covered by
/// the unit tests (see example below). This is expected because the conditions should not be
/// reachable. But it makes it difficult to evaluate at a glance whether full code coverage has
/// been reached.
/// <code>
/// #if DEBUG
///     if (condition) throw new Exception("...");
/// #endif
/// </code>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class DebugChecks
{
    [Conditional("DEBUG")]
    public static void IsValid(SemVersionStyles style, string paramName)
    {
        if (!style.IsValid())
            throw new ArgumentException("DEBUG: " + SemVersion.InvalidSemVersionStylesMessage, paramName);
    }

    [Conditional("DEBUG")]
    public static void IsValid(SemVersionRangeOptions rangeOptions, string paramName)
    {
        if (!rangeOptions.IsValid())
            throw new ArgumentException("DEBUG: " + SemVersionRange.InvalidOptionsMessage, paramName);
    }

    [Conditional("DEBUG")]
    public static void IsValidMaxLength(int maxLength, string paramName)
    {
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(paramName, "DEBUG: " + SemVersionRange.InvalidMaxLengthMessage);
    }

    [Conditional("DEBUG")]
    public static void IsNotWildcardVersionWithPrerelease(WildcardVersion wildcardVersion, SemVersion semver)
    {
        if (wildcardVersion != WildcardVersion.None && semver.IsPrerelease)
            throw new InvalidOperationException("DEBUG: prerelease not allowed with wildcard");
    }

    [Conditional("DEBUG")]
    public static void IsNotEmpty(StringSegment segment, string paramName)
    {
        if (segment.IsEmpty())
            throw new ArgumentException("DEBUG: Cannot be empty", paramName);
    }

    [Conditional("DEBUG")]
    public static void IsNotNull<T>([NotNull] T? value, string paramName)
        where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName, "DEBUG: Value cannot be null.");
    }

    /// <summary>
    /// This check ensures that an exception hasn't been constructed, but rather something always
    /// returns <see cref="VersionParsing.FailedException"/>.
    /// </summary>
    [Conditional("DEBUG")]
    public static void IsNotFailedException(Exception? exception, string className, string methodName)
    {
        if (exception != null && exception != VersionParsing.FailedException)
            throw new InvalidOperationException($"DEBUG: {className}.{methodName} returned exception other than {nameof(VersionParsing.FailedException)}", exception);
    }

    [Conditional("DEBUG")]
    public static void NoMetadata(SemVersion? version, string paramName)
    {
        if (version?.MetadataIdentifiers.Any() ?? false)
            throw new ArgumentException("DEBUG: Cannot have metadata.", paramName);
    }

    [Conditional("DEBUG")]
    public static void IsValidVersionNumber(BigInteger versionNumber, string kind, string paramName)
    {
        if (versionNumber < 0)
            throw new ArgumentException($"DEBUG: {kind} version must be greater than or equal to zero.", paramName);
    }

    [Conditional("DEBUG")]
    public static void ContainsNoDefaultValues<T>(IEnumerable<T> values, string kind, string paramName)
        where T : struct
    {
        if (values.Any(i => EqualityComparer<T>.Default.Equals(i, default)))
            throw new ArgumentException($"DEBUG: {kind} identifier cannot be default/null.", paramName);
    }

    [Conditional("DEBUG")]
    public static void AreEqualWhenJoinedWithDots<T>(string value, string param1Name, IReadOnlyList<T> values, string param2Name)
    {
        if (value != string.Join(".", values))
            throw new ArgumentException($"DEBUG: must be equal to {param2Name} when joined with dots.", param1Name);
    }
}
