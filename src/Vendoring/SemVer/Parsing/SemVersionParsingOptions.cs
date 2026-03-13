using System;

namespace Semver.Parsing;

/// <summary>
/// Options beyond the <see cref="SemVersionStyles"/> for version parsing used by range parsing.
/// </summary>
internal class SemVersionParsingOptions
{
    /// <summary>
    /// No special parsing options. Used when parsing versions outside of ranges.
    /// </summary>
    public static SemVersionParsingOptions None = new(false, false, false, _ => false);

    public SemVersionParsingOptions(
        bool allowWildcardMajorMinorPatch,
        bool allowWildcardPrerelease,
        bool missingVersionsAreWildcards,
        Predicate<char> isWildcard)
    {
        AllowWildcardMajorMinorPatch = allowWildcardMajorMinorPatch;
        AllowWildcardPrerelease = allowWildcardPrerelease;
        MissingVersionsAreWildcards = missingVersionsAreWildcards;
        IsWildcard = isWildcard;
    }

    /// <summary>
    /// Allow wildcards as defined by <see cref="IsWildcard"/> in the major, minor, and patch
    /// version numbers.
    /// </summary>
    public bool AllowWildcardMajorMinorPatch { get; }

    /// <summary>
    /// Allow a wildcard as defined by <see cref="IsWildcard"/> as the final prerelease identifier.
    /// </summary>
    public bool AllowWildcardPrerelease { get; }

    /// <summary>
    /// Whether missing minor and patch version numbers allowed by the optional minor and patch
    /// options count as being wildcard version numbers.
    /// </summary>
    public bool MissingVersionsAreWildcards { get; }

    /// <summary>
    /// Determines whether any given character is a wildcard character.
    /// </summary>
    public Predicate<char> IsWildcard { get; }
}
