using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Primitives;
using Semver.Ranges;
using Semver.Utility;

namespace Semver.Parsing;

internal static class StandardRangeParser
{
    public static Exception? Parse(
        string? range,
        SemVersionRangeOptions rangeOptions,
        Exception? ex,
        int maxLength,
        out SemVersionRange? semverRange)
    {
        DebugChecks.IsValid(rangeOptions, nameof(rangeOptions));
        DebugChecks.IsValidMaxLength(maxLength, nameof(maxLength));

        // Assign null once, so it doesn't have to be done any time parse fails
        semverRange = null;

        // Note: this method relies on the fact that the null coalescing operator `??`
        // is short-circuiting to avoid constructing exceptions and exception messages
        // when a non-null exception is passed in.

        if (range is null) return ex ?? new ArgumentNullException(nameof(range));
        if (range.Length > maxLength) return ex ?? RangeError.TooLong(range, maxLength);

        var unbrokenRanges = new List<UnbrokenSemVersionRange>(GeneralRangeParser.CountSplitOnOrOperator(range));
        foreach (var segment in GeneralRangeParser.SplitOnOrOperator(range))
        {
            var exception = ParseUnbrokenRange(segment, rangeOptions, ex, maxLength, out var unbrokenRange);
            if (exception is not null) return exception;
            DebugChecks.IsNotNull(unbrokenRange, nameof(unbrokenRange));

            unbrokenRanges.Add(unbrokenRange);
        }

        semverRange = SemVersionRange.Create(unbrokenRanges);
        return null;
    }

    private static Exception? ParseUnbrokenRange(
        StringSegment segment,
        SemVersionRangeOptions rangeOptions,
        Exception? ex,
        int maxLength,
        out UnbrokenSemVersionRange? unbrokenRange)
    {
        // Assign null once, so it doesn't have to be done any time parse fails
        unbrokenRange = null;

        // Parse off leading whitespace
        var exception = GeneralRangeParser.ParseOptionalSpaces(ref segment, ex);
        if (exception != null) return exception;

        // Reject empty string ranges
        if (segment.IsEmpty()) return ex ?? RangeError.MissingComparison(segment.Offset, segment.Buffer!);

        var start = LeftBoundedRange.Unbounded;
        var end = RightBoundedRange.Unbounded;
        var includeAllPrerelease = rangeOptions.HasOption(SemVersionRangeOptions.IncludeAllPrerelease);
        while (!segment.IsEmpty())
        {
            exception = ParseComparison(ref segment, rangeOptions, ref includeAllPrerelease, ex, maxLength, ref start, ref end);
            if (exception != null) return exception;
        }

        unbrokenRange = UnbrokenSemVersionRange.Create(start, end, includeAllPrerelease);
        return null;
    }

    /// <summary>
    /// Parse a comparison from the beginning of the segment.
    /// </summary>
    /// <remarks>
    /// Must have leading whitespace removed. Will consume trailing whitespace.
    /// </remarks>
    private static Exception? ParseComparison(
        ref StringSegment segment,
        SemVersionRangeOptions rangeOptions,
        ref bool includeAllPrerelease,
        Exception? ex,
        int maxLength,
        ref LeftBoundedRange leftBound,
        ref RightBoundedRange rightBound)
    {
        DebugChecks.IsNotEmpty(segment, nameof(segment));

        var exception = ParseOperator(ref segment, ex, out var @operator);
        if (exception != null) return exception;

        exception = GeneralRangeParser.ParseOptionalSpaces(ref segment, ex);
        if (exception != null) return exception;

        exception = GeneralRangeParser.ParseVersion(ref segment, rangeOptions, ParsingOptions, ex, maxLength,
                        out var semver, out var wildcardVersion);
        if (exception != null) return exception;
        DebugChecks.IsNotNull(semver, nameof(semver));

        if (@operator != StandardOperator.None && wildcardVersion != WildcardVersion.None)
            return ex ?? RangeError.WildcardNotSupportedWithOperator(segment.Buffer!);

        exception = GeneralRangeParser.ParseOptionalSpaces(ref segment, ex);
        if (exception != null) return exception;

        switch (@operator)
        {
            case StandardOperator.Equals:
                leftBound = leftBound.Max(new LeftBoundedRange(semver, true));
                rightBound = rightBound.Min(new RightBoundedRange(semver, true));
                return null;
            case StandardOperator.GreaterThan:
                leftBound = leftBound.Max(new LeftBoundedRange(semver, false));
                return null;
            case StandardOperator.GreaterThanOrEqual:
                leftBound = leftBound.Max(new LeftBoundedRange(semver, true));
                return null;
            case StandardOperator.LessThan:
                rightBound = rightBound.Min(new RightBoundedRange(semver, false));
                return null;
            case StandardOperator.LessThanOrEqual:
                rightBound = rightBound.Min(new RightBoundedRange(semver, true));
                return null;
            case StandardOperator.Caret:
                leftBound = leftBound.Max(new LeftBoundedRange(semver, true));
                BigInteger major = BigInteger.Zero, minor = BigInteger.Zero, patch = BigInteger.Zero;
                if (semver.Major != 0)
                    major = semver.Major + BigInteger.One;
                else if (semver.Minor != 0)
                    minor = semver.Minor + BigInteger.One;
                else
                    patch = semver.Patch + BigInteger.One;

                rightBound = rightBound.Min(new RightBoundedRange(new SemVersion(
                                major, minor, patch,
                                "0", PrereleaseIdentifiers.Zero,
                                "", ReadOnlyList<MetadataIdentifier>.Empty), false));
                return null;
            case StandardOperator.Tilde:
                leftBound = leftBound.Max(new LeftBoundedRange(semver, true));
                rightBound = rightBound.Min(new RightBoundedRange(
                    semver.With(minor: semver.Minor + BigInteger.One, patch: BigInteger.Zero, prerelease: PrereleaseIdentifiers.Zero),
                    false));
                return null;
            case StandardOperator.None: // implied = (supports wildcard *)
                var prereleaseWildcard = wildcardVersion.HasOption(WildcardVersion.PrereleaseWildcard);
                includeAllPrerelease |= prereleaseWildcard;
                wildcardVersion.RemoveOption(WildcardVersion.PrereleaseWildcard);
                if (wildcardVersion != WildcardVersion.None && semver.IsPrerelease)
                    return ex ?? RangeError.PrereleaseNotSupportedWithWildcardVersion(segment.Buffer!);
                switch (wildcardVersion)
                {
                    case WildcardVersion.None:
                        leftBound = leftBound.Max(WildcardLowerBound(semver, prereleaseWildcard));
                        PrereleaseWildcardUpperBound(ref rightBound, semver, prereleaseWildcard);
                        return null;
                    case WildcardVersion.MajorMinorPatchWildcard:
                        // No further bound is places on the left and right bounds
                        return null;
                    case WildcardVersion.MinorPatchWildcard:
                        leftBound = leftBound.Max(WildcardLowerBound(semver, prereleaseWildcard));
                        rightBound = rightBound.Min(new RightBoundedRange(
                            new SemVersion(semver.Major + BigInteger.One, BigInteger.Zero, BigInteger.Zero,
                                "0", PrereleaseIdentifiers.Zero,
                                "", ReadOnlyList<MetadataIdentifier>.Empty), false));
                        return null;
                    case WildcardVersion.PatchWildcard:
                        leftBound = leftBound.Max(WildcardLowerBound(semver, prereleaseWildcard));
                        rightBound = rightBound.Min(new RightBoundedRange(
                            new SemVersion(semver.Major, semver.Minor + BigInteger.One, BigInteger.Zero,
                                "0", PrereleaseIdentifiers.Zero,
                                "", ReadOnlyList<MetadataIdentifier>.Empty), false));
                        return null;
                    default:
                        // dotcover disable next line
                        throw Unreachable.InvalidEnum(wildcardVersion);
                }
            default:
                // dotcover disable next line
                throw Unreachable.InvalidEnum(@operator);
        }
    }

    private static LeftBoundedRange WildcardLowerBound(SemVersion semver, bool prereleaseWildcard)
    {
        if (prereleaseWildcard)
            semver = semver.IsPrerelease
                ? semver.WithPrerelease(semver.PrereleaseIdentifiers.Concat(PrereleaseIdentifiers.Zero))
                : semver.WithPrerelease(PrereleaseIdentifier.Zero);
        return new LeftBoundedRange(semver, true);
    }

    private static void PrereleaseWildcardUpperBound(
        ref RightBoundedRange rightBound,
        SemVersion semver,
        bool prereleaseWildcard)
    {
        var inclusive = false;
        if (prereleaseWildcard)
        {
            if (semver.IsPrerelease)
                semver = new SemVersion(semver.Major, semver.Minor, semver.Patch,
                    PrereleaseWildcardUpperBoundPrereleaseIdentifiers(semver.PrereleaseIdentifiers));
            else
            {
                semver = new SemVersion(semver.Major, semver.Minor, semver.Patch + BigInteger.One,
                    "0", PrereleaseIdentifiers.Zero, "", ReadOnlyList<MetadataIdentifier>.Empty);
            }
        }
        else
            inclusive = true;

        rightBound = rightBound.Min(new RightBoundedRange(semver, inclusive));
    }

    private static IEnumerable<PrereleaseIdentifier> PrereleaseWildcardUpperBoundPrereleaseIdentifiers(
        IReadOnlyList<PrereleaseIdentifier> identifiers)
    {
        for (int i = 0; i < identifiers.Count - 1; i++)
            yield return identifiers[i];

        var lastIdentifier = identifiers[^1];

        yield return lastIdentifier.NextIdentifier();
    }

    private static Exception? ParseOperator(
        ref StringSegment segment, Exception? ex, out StandardOperator @operator)
    {
        var end = 0;
        while (end < segment.Length && GeneralRangeParser.IsPossibleOperatorChar(segment[end], SemVersionRangeOptions.Strict)) end++;
        var opSegment = segment.Subsegment(0, end);
        segment = segment.Subsegment(end);

        if (opSegment.Length == 0)
        {
            @operator = StandardOperator.None;
            return null;
        }

        // Assign invalid once, so it doesn't have to be done any time parse fails
        @operator = 0;
        if (opSegment.Length > 2
            || (opSegment.Length == 2 && opSegment[1] != '='))
            return ex ?? RangeError.InvalidOperator(opSegment);

        var firstChar = opSegment[0];
        var isOrEqual = opSegment.Length == 2; // Already checked for second char != '='
        switch (firstChar)
        {
            case '=' when !isOrEqual:
                @operator = StandardOperator.Equals;
                return null;
            case '<' when isOrEqual:
                @operator = StandardOperator.LessThanOrEqual;
                return null;
            case '<':
                @operator = StandardOperator.LessThan;
                return null;
            case '>' when isOrEqual:
                @operator = StandardOperator.GreaterThanOrEqual;
                return null;
            case '>':
                @operator = StandardOperator.GreaterThan;
                return null;
            case '~' when !isOrEqual:
                @operator = StandardOperator.Tilde;
                return null;
            case '^' when !isOrEqual:
                @operator = StandardOperator.Caret;
                return null;
            default:
                return ex ?? RangeError.InvalidOperator(opSegment);
        }
    }

    private static readonly SemVersionParsingOptions ParsingOptions
        = new(true, true, false, c => c == '*');
}
