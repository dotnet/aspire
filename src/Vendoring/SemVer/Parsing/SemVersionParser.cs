using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;
using Semver.Utility;

namespace Semver.Parsing;

/// <summary>
/// Parsing for <see cref="SemVersion"/>
/// </summary>
/// <remarks>The new parsing code was complex enough that is made sense to break out into its
/// own class.</remarks>
internal static class SemVersionParser
{
    private const string LeadingWhitespaceMessage = "Version '{0}' has leading whitespace.";
    private const string TrailingWhitespaceMessage = "Version '{0}' has trailing whitespace.";
    private const string EmptyVersionMessage = "Empty string is not a valid version.";
    private const string TooLongVersionMessage = "Exceeded maximum length of {1} for '{0}'.";
    private const string AllWhitespaceVersionMessage = "Whitespace is not a valid version.";
    private const string LeadingLowerVMessage = "Leading 'v' in '{0}'.";
    private const string LeadingUpperVMessage = "Leading 'V' in '{0}'.";
    private const string LeadingZeroInMajorMinorOrPatchMessage = "{1} version has leading zero in '{0}'.";
    private const string EmptyMajorMinorOrPatchMessage = "{1} version missing in '{0}'.";
    private const string FourthVersionNumberMessage = "Fourth version number in '{0}'.";
    private const string PrereleasePrefixedByDotMessage = "The prerelease identfiers should be prefixed by '-' instead of '.' in '{0}'.";
    private const string MissingPrereleaseIdentifierMessage = "Missing prerelease identifier in '{0}'.";
    private const string LeadingZeroInPrereleaseMessage = "Leading zero in prerelease identifier in version '{0}'.";
    private const string InvalidCharacterInPrereleaseMessage = "Invalid character '{1}' in prerelease identifier in '{0}'.";
    private const string MissingMetadataIdentifierMessage = "Missing metadata identifier in '{0}'.";
    private const string InvalidCharacterInMajorMinorOrPatchMessage = "{1} version contains invalid character '{2}' in '{0}'.";
    private const string InvalidCharacterInMetadataMessage = "Invalid character '{1}' in metadata identifier in '{0}'.";
    private const string InvalidWildcardInMajorMinorOrPatchMessage = "{1} version is a wildcard and should contain only 1 character in '{0}'.";
    private const string MinorOrPatchMustBeWildcardVersionMessage = "{1} version should be a wildcard because the preceding version is a wildcard in '{0}'.";
    private const string InvalidWildcardInPrereleaseMessage = "Prerelease version is a wildcard and should contain only 1 character in '{0}'.";
    private const string PrereleaseWildcardMustBeLastMessage = "Prerelease identifier follows wildcard prerelease identifier in '{0}'.";
    private const string MajorMinorOrPatchParsingFailedMessage = "{1} version '{2}' failed to parse in '{0}'.";
    private const string PrereleaseParsingFailedMessage = "Prerelease identifier '{1}' failed to parse in '{0}'.";

    /// <summary>
    /// The internal method that all parsing is based on. Because this is called by both
    /// <see cref="SemVersion.Parse(string, SemVersionStyles, int)"/> and
    /// <see cref="SemVersion.TryParse(string, SemVersionStyles, out SemVersion, int)"/>
    /// it does not throw exceptions, but instead returns the exception that should be thrown
    /// by the parse method. For performance when used from try parse, all exception construction
    /// and message formatting can be avoided by passing in an exception which will be returned
    /// when parsing fails.
    /// </summary>
    /// <remarks>This does not validate the <paramref name="style"/> or <paramref name="maxLength"/>
    /// parameter values. That must be done in the calling method.</remarks>
    public static Exception? Parse(
        string? version,
        SemVersionStyles style,
        Exception? ex,
        int maxLength,
        out SemVersion? semver)
    {
        DebugChecks.IsValid(style, nameof(style));
        DebugChecks.IsValidMaxLength(maxLength, nameof(maxLength));

        if (version != null)
            return Parse(version, style, SemVersionParsingOptions.None, ex, maxLength, out semver, out _);
        semver = null;
        return ex ?? new ArgumentNullException(nameof(version));
    }

    /// <summary>
    /// An internal method that is used when parsing versions from ranges. Because this is
    /// called by both
    /// <see cref="SemVersionRange.Parse(string,SemVersionRangeOptions,int)"/> and
    /// <see cref="SemVersionRange.TryParse(string,SemVersionRangeOptions,out SemVersionRange,int)"/>
    /// it does not throw exceptions, but instead returns the exception that should be thrown
    /// by the parse method. For performance when used from try parse, all exception construction
    /// and message formatting can be avoided by passing in an exception which will be returned
    /// when parsing fails.
    /// </summary>
    /// <remarks>This does not validate the <paramref name="style"/> or <paramref name="maxLength"/>
    /// parameter values. That must be done in the calling method.</remarks>
    public static Exception? Parse(
        StringSegment version,
        SemVersionStyles style,
        SemVersionParsingOptions options,
        Exception? ex,
        int maxLength,
        out SemVersion? semver,
        out WildcardVersion wildcardVersion)
    {
        DebugChecks.IsValid(style, nameof(style));
        DebugChecks.IsValidMaxLength(maxLength, nameof(maxLength));

        // Assign once so it doesn't have to be done any time parse fails
        semver = null;
        wildcardVersion = WildcardVersion.None;

        // Note: this method relies on the fact that the null coalescing operator `??`
        // is short-circuiting to avoid constructing exceptions and exception messages
        // when a non-null exception is passed in.

        if (version.Length == 0) return ex ?? new FormatException(EmptyVersionMessage);

        if (version.Length > maxLength)
            return ex ?? NewFormatException(TooLongVersionMessage, version.ToStringLimitLength(), maxLength);

        // This code does two things to help provide good error messages:
        // 1. It breaks the version number into segments and then parses those segments
        // 2. It parses an element first, then checks the flags for whether it should be allowed

        var mainSegment = version;

        var parseEx = ParseLeadingWhitespace(version, ref mainSegment, style, ex);
        if (parseEx != null) return parseEx;

        // Take of trailing whitespace and remember that there was trailing whitespace
        var lengthWithTrailingWhitespace = mainSegment.Length;
        mainSegment = mainSegment.TrimEnd();
        var hasTrailingWhitespace = lengthWithTrailingWhitespace > mainSegment.Length;

        // Now break the version number down into segments.
        mainSegment.SplitBeforeFirst('+', out mainSegment, out var metadataSegment);
        mainSegment.SplitBeforeFirst('-', out var majorMinorPatchSegment, out var prereleaseSegment);

        parseEx = ParseLeadingV(version, ref majorMinorPatchSegment, style, ex);
        if (parseEx != null) return parseEx;

        // Are leading zeros allowed
        var allowLeadingZeros = style.HasStyle(SemVersionStyles.AllowLeadingZeros);

        BigInteger major, minor, patch;
        using (var versionNumbers = majorMinorPatchSegment.Split('.').GetEnumerator())
        {
            const bool majorIsOptional = false;
            const bool majorIsWildcardRequired = false;
            parseEx = ParseVersionNumber("Major", version, versionNumbers, allowLeadingZeros,
                majorIsOptional, majorIsWildcardRequired, options, ex, out major, out var majorIsWildcard);
            if (parseEx != null) return parseEx;
            if (majorIsWildcard) wildcardVersion |= WildcardVersion.MajorMinorPatchWildcard;

            var minorIsOptional = style.HasStyle(SemVersionStyles.OptionalMinorPatch) || majorIsWildcard;
            parseEx = ParseVersionNumber("Minor", version, versionNumbers, allowLeadingZeros,
                minorIsOptional, majorIsWildcard, options, ex, out minor, out var minorIsWildcard);
            if (parseEx != null) return parseEx;
            if (minorIsWildcard) wildcardVersion |= WildcardVersion.MinorPatchWildcard;

            var patchIsOptional = style.HasStyle(SemVersionStyles.OptionalPatch) || majorIsWildcard || minorIsWildcard;
            parseEx = ParseVersionNumber("Patch", version, versionNumbers, allowLeadingZeros,
                patchIsOptional, minorIsWildcard, options, ex, out patch, out var patchIsWildcard);
            if (parseEx != null) return parseEx;
            if (patchIsWildcard) wildcardVersion |= WildcardVersion.PatchWildcard;

            // Handle fourth version number
            if (versionNumbers.MoveNext())
            {
                var fourthSegment = versionNumbers.Current;
                // If it is ".\d" then we'll assume they were trying to have a fourth version number
                if (fourthSegment.Length > 0 && fourthSegment[0].IsDigit())
                    return ex ?? NewFormatException(FourthVersionNumberMessage, version.ToStringLimitLength());

                // Otherwise, assume they used "." instead of "-" to start the prerelease
                return ex ?? NewFormatException(PrereleasePrefixedByDotMessage, version.ToStringLimitLength());
            }
        }

        // Parse prerelease version
        string? prerelease;
        IReadOnlyList<PrereleaseIdentifier> prereleaseIdentifiers;
        if (prereleaseSegment.Length > 0)
        {
            prereleaseSegment = prereleaseSegment.Subsegment(1);
            parseEx = ParsePrerelease(version, prereleaseSegment, allowLeadingZeros, options, ex,
                        out prerelease, out prereleaseIdentifiers, out var prereleaseIsWildcard);
            if (parseEx != null) return parseEx;
            DebugChecks.IsNotNull(prerelease, nameof(prerelease));
            if (prereleaseIsWildcard) wildcardVersion |= WildcardVersion.PrereleaseWildcard;
        }
        else
        {
            prerelease = "";
            prereleaseIdentifiers = ReadOnlyList<PrereleaseIdentifier>.Empty;
        }

        // Parse metadata
        string metadata;
        IReadOnlyList<MetadataIdentifier> metadataIdentifiers;
        if (metadataSegment.Length > 0)
        {
            metadataSegment = metadataSegment.Subsegment(1);
            parseEx = ParseMetadata(version, metadataSegment, ex, out metadata, out metadataIdentifiers);
            if (parseEx != null) return parseEx;
        }
        else
        {
            metadata = "";
            metadataIdentifiers = ReadOnlyList<MetadataIdentifier>.Empty;
        }

        // Error if trailing whitespace not allowed
        if (hasTrailingWhitespace && !style.HasStyle(SemVersionStyles.AllowTrailingWhitespace))
            return ex ?? NewFormatException(TrailingWhitespaceMessage, version.ToStringLimitLength());

        semver = new SemVersion(major, minor, patch,
            prerelease, prereleaseIdentifiers, metadata, metadataIdentifiers);
        return null;
    }

    private static Exception? ParseLeadingWhitespace(
        StringSegment version,
        ref StringSegment segment,
        SemVersionStyles style,
        Exception? ex)
    {
        var oldLength = segment.Length;

        // Skip leading whitespace
        segment = segment.TrimStart();

        // Error if all whitespace
        if (segment.Length == 0)
            return ex ?? new FormatException(AllWhitespaceVersionMessage);

        // Error if leading whitespace not allowed
        if (oldLength > segment.Length && !style.HasStyle(SemVersionStyles.AllowLeadingWhitespace))
            return ex ?? NewFormatException(LeadingWhitespaceMessage, version.ToStringLimitLength());

        return null;
    }

    private static Exception? ParseLeadingV(
        StringSegment version,
        ref StringSegment segment,
        SemVersionStyles style,
        Exception? ex)
    {
        if (segment.IsEmpty()) return null;

        var leadChar = segment[0];
        switch (leadChar)
        {
            case 'v' when style.HasStyle(SemVersionStyles.AllowLowerV):
                segment = segment.Subsegment(1);
                break;
            case 'v':
                return ex ?? NewFormatException(LeadingLowerVMessage, version.ToStringLimitLength());
            case 'V' when style.HasStyle(SemVersionStyles.AllowUpperV):
                segment = segment.Subsegment(1);
                break;
            case 'V':
                return ex ?? NewFormatException(LeadingUpperVMessage, version.ToStringLimitLength());
        }

        return null;
    }

    private static Exception? ParseVersionNumber(
        string kind, // i.e. Major, Minor, or Patch
        StringSegment version,
        IEnumerator<StringSegment> versionNumbers,
        bool allowLeadingZeros,
        bool optional,
        bool wildcardRequired,
        SemVersionParsingOptions options,
        Exception? ex,
        out BigInteger number,
        out bool isWildcard)
    {
        if (versionNumbers.MoveNext())
            return ParseVersionNumber(kind, version, versionNumbers.Current, allowLeadingZeros,
                wildcardRequired, options, ex, out number, out isWildcard);

        number = 0;
        isWildcard = options.MissingVersionsAreWildcards;
        if (!optional)
            return ex ?? NewFormatException(EmptyMajorMinorOrPatchMessage, version.ToStringLimitLength(), kind);

        return null;
    }

    private static Exception? ParseVersionNumber(
        string kind, // i.e. Major, Minor, or Patch
        StringSegment version,
        StringSegment segment,
        bool allowLeadingZeros,
        bool wildcardRequired,
        SemVersionParsingOptions options,
        Exception? ex,
        out BigInteger number,
        out bool isWildcard)
    {
        // Assign once so it doesn't have to be done any time parse fails
        number = BigInteger.Zero;

        if (segment.Length == 0)
        {
            isWildcard = false;
            return ex ?? NewFormatException(EmptyMajorMinorOrPatchMessage, version.ToStringLimitLength(), kind);
        }

        if (options.AllowWildcardMajorMinorPatch && segment.Length > 0 && options.IsWildcard(segment[0]))
        {
            isWildcard = true;
            if (segment.Length > 1)
                return ex ?? NewFormatException(InvalidWildcardInMajorMinorOrPatchMessage,
                    version.ToStringLimitLength(), kind);

            return null;
        }

        isWildcard = false;

        if (wildcardRequired)
            return ex ?? NewFormatException(MinorOrPatchMustBeWildcardVersionMessage,
                version.ToStringLimitLength(), kind);

        var lengthWithLeadingZeros = segment.Length;

        // Skip leading zeros
        segment = segment.TrimLeadingZeros();

        // Scan for digits
        var i = 0;
        while (i < segment.Length && segment[i].IsDigit()) i += 1;

        // If there are unprocessed characters, then it is an invalid char for this segment
        if (i < segment.Length)
            return ex ?? NewFormatException(InvalidCharacterInMajorMinorOrPatchMessage,
                version.ToStringLimitLength(),
                kind, segment[i]);

        if (!allowLeadingZeros && lengthWithLeadingZeros > segment.Length)
            return ex ?? NewFormatException(LeadingZeroInMajorMinorOrPatchMessage,
                version.ToStringLimitLength(), kind);

        var numberString = segment.ToString();
        if (!BigInteger.TryParse(numberString, NumberStyles.None, CultureInfo.InvariantCulture, out number))
            // Parsing validated this as a string of digits possibly proceeded by zero so this
            // failure shouldn't be possible.
            return ex ?? new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                MajorMinorOrPatchParsingFailedMessage, version.ToStringLimitLength(), kind, numberString));

        return null;
    }

    private static Exception? ParsePrerelease(
        StringSegment version,
        StringSegment segment,
        bool allowLeadingZero,
        SemVersionParsingOptions options,
        Exception? ex,
        out string? prerelease,
        out IReadOnlyList<PrereleaseIdentifier> prereleaseIdentifiers,
        out bool isWildcard)
    {
        prerelease = null;
        var identifiers = new List<PrereleaseIdentifier>(segment.SplitCount('.'));
        prereleaseIdentifiers = identifiers.AsReadOnly();
        isWildcard = false;

        bool hasLeadingZeros = false;
        foreach (var identifier in segment.Split('.'))
        {
            // Identifier after wildcard
            if (isWildcard)
                return ex ?? NewFormatException(PrereleaseWildcardMustBeLastMessage, version.ToStringLimitLength());

            // Empty identifiers not allowed
            if (identifier.Length == 0)
                return ex ?? NewFormatException(MissingPrereleaseIdentifierMessage, version.ToStringLimitLength());

            var isNumeric = true;

            for (int i = 0; i < identifier.Length; i++)
            {
                var c = identifier[i];
                if (c.IsAlphaOrHyphen())
                    isNumeric = false;
                else if (options.AllowWildcardPrerelease && options.IsWildcard(c))
                    isWildcard = true;
                else if (!c.IsDigit())
                    return ex ?? NewFormatException(InvalidCharacterInPrereleaseMessage,
                        version.ToStringLimitLength(), c);
            }

            if (isWildcard)
            {
                if (identifier.Length > 1) return ex ?? NewFormatException(InvalidWildcardInPrereleaseMessage, version.ToStringLimitLength());
                isWildcard = true;
                continue; // continue to make sure there aren't more identifiers
            }

            if (!isNumeric)
                identifiers.Add(PrereleaseIdentifier.CreateUnsafe(identifier.ToString(), null));
            else
            {
                string identifierString;
                if (identifier[0] == '0' && identifier.Length > 1)
                {
                    if (!allowLeadingZero)
                        return ex ?? NewFormatException(LeadingZeroInPrereleaseMessage,
                            version.ToStringLimitLength());
                    hasLeadingZeros = true;
                    identifierString = identifier.TrimLeadingZeros().ToString();
                }
                else
                    identifierString = identifier.ToString();

                if (!BigInteger.TryParse(identifierString, NumberStyles.None, null, out var numericValue))
                    // Parsing validated this as a string of digits possibly proceeded by zero
                    // so this failure shouldn't be possible.
                    return ex ?? new FormatException(string.Format(CultureInfo.InvariantCulture,
                        PrereleaseParsingFailedMessage, version.ToStringLimitLength(), identifier));

                identifiers.Add(PrereleaseIdentifier.CreateUnsafe(identifierString, numericValue));
            }
        }

        // If there are leading zeros or a wildcard, reconstruct the string from the identifiers,
        // otherwise just take a substring.
        prerelease = hasLeadingZeros || isWildcard
                        ? string.Join(".", identifiers) : segment.ToString();

        return null;
    }

    private static Exception? ParseMetadata(
        StringSegment version,
        StringSegment segment,
        Exception? ex,
        out string metadata,
        out IReadOnlyList<MetadataIdentifier> metadataIdentifiers)
    {
        metadata = segment.ToString();
        var identifiers = new List<MetadataIdentifier>(segment.SplitCount('.'));
        metadataIdentifiers = identifiers.AsReadOnly();
        foreach (var identifier in segment.Split('.'))
        {
            // Empty identifiers not allowed
            if (identifier.Length == 0)
                return ex ?? NewFormatException(MissingMetadataIdentifierMessage, version.ToStringLimitLength());

            for (int i = 0; i < identifier.Length; i++)
            {
                var c = identifier[i];
                if (!c.IsAlphaOrHyphen() && !c.IsDigit())
                    return ex ?? NewFormatException(InvalidCharacterInMetadataMessage,
                        version.ToStringLimitLength(), c);
            }

            identifiers.Add(MetadataIdentifier.CreateUnsafe(identifier.ToString()));
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FormatException NewFormatException(string messageTemplate, params object[] args)
        => new(string.Format(CultureInfo.InvariantCulture, messageTemplate, args));
}
