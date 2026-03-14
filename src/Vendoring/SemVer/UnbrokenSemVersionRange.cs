using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Semver.Comparers;
using Semver.Ranges;

namespace Semver;

/// <summary>
/// A range of <see cref="SemVersion"/> values with no gaps. The more general and flexible range
/// class <see cref="SemVersionRange"/> is typically used instead. It combines multiple
/// <see cref="UnbrokenSemVersionRange"/>s. <see cref="UnbrokenSemVersionRange"/> can be used
/// directly if it is important to reflect that something must be a range with no gaps in it.
/// </summary>
/// <remarks>An <see cref="UnbrokenSemVersionRange"/> is represented as an interval between two
/// versions, the <see cref="Start"/> and <see cref="End"/>. For each, that version may or may
/// not be included.</remarks>
internal sealed class UnbrokenSemVersionRange : IEquatable<UnbrokenSemVersionRange>
{
    /// <summary>
    /// A standard representation for the empty range that contains no versions.
    /// </summary>
    /// <value>A standard representation for the empty range that contains no versions.</value>
    /// <remarks><para>There are an infinite number of ways to represent the empty range. Any range
    /// where the start is greater than the end or where start equals end but one is not
    /// inclusive would be empty.
    /// See https://en.wikipedia.org/wiki/Interval_(mathematics)#Classification_of_intervals</para>
    ///
    /// <para>Since there is no maximum version the only unique empty range is <c>&lt;0.0.0-0</c>.</para>
    /// </remarks>
    public static UnbrokenSemVersionRange Empty { get; }
        = new UnbrokenSemVersionRange(new LeftBoundedRange(null, false),
            new RightBoundedRange(SemVersion.Min, false), false, false);

    /// <summary>
    /// The range that contains all release versions but no prerelease versions.
    /// </summary>
    /// <value>The range that contains all release versions but no prerelease versions.</value>
    public static UnbrokenSemVersionRange AllRelease { get; } = Create(null, false, null, false, false);

    /// <summary>
    /// The range that contains both all release and prerelease versions.
    /// </summary>
    /// <value>The range that contains both all release and prerelease versions.</value>
    public static UnbrokenSemVersionRange All { get; } = Create(null, false, null, false, true);

    #region Static Factory Methods
    /// <summary>
    /// Construct a range containing only a single version.
    /// </summary>
    /// <param name="version">The version the range should contain.</param>
    /// <returns>A range containing only the given version.</returns>
    public static UnbrokenSemVersionRange Equals(SemVersion version)
        => Create(Validate(version, nameof(version)), true, version, true, false);

    /// <summary>
    /// Construct a range containing versions greater than the given version.
    /// </summary>
    /// <param name="version">The range will contain all versions greater than this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given version if it is prerelease.</param>
    /// <returns>A range containing versions greater than the given version.</returns>
    public static UnbrokenSemVersionRange GreaterThan(SemVersion version, bool includeAllPrerelease = false)
        => Create(Validate(version, nameof(version)), false, null, false, includeAllPrerelease);

    /// <summary>
    /// Construct a range containing versions equal to or greater than the given version.
    /// </summary>
    /// <param name="version">The range will contain all versions greater than or equal to this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given version if it is prerelease.</param>
    /// <returns>A range containing versions greater than or equal to the given version.</returns>
    public static UnbrokenSemVersionRange AtLeast(SemVersion version, bool includeAllPrerelease = false)
        => Create(Validate(version, nameof(version)), true, null, false, includeAllPrerelease);

    /// <summary>
    /// Construct a range containing versions less than the given version.
    /// </summary>
    /// <param name="version">The range will contain all versions less than this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given version if it is prerelease.</param>
    /// <returns>A range containing versions less than the given version.</returns>
    public static UnbrokenSemVersionRange LessThan(SemVersion version, bool includeAllPrerelease = false)
        => Create(null, false, Validate(version, nameof(version)), false, includeAllPrerelease);

    /// <summary>
    /// Construct a range containing versions equal to or less than the given version.
    /// </summary>
    /// <param name="version">The range will contain all versions less than or equal to this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given version if it is prerelease.</param>
    /// <returns>A range containing versions less than or equal to the given version.</returns>
    public static UnbrokenSemVersionRange AtMost(SemVersion version, bool includeAllPrerelease = false)
        => Create(null, false, Validate(version, nameof(version)), true, includeAllPrerelease);

    /// <summary>
    /// Construct a range containing all versions between the given versions including those versions.
    /// </summary>
    /// <param name="start">The range will contain only versions greater than or equal to this.</param>
    /// <param name="end">The range will contain only versions less than or equal to this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given versions if they are prerelease.</param>
    /// <returns>A range containing versions between the given versions including those versions.</returns>
    public static UnbrokenSemVersionRange Inclusive(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
        => Create(Validate(start, nameof(start)), true,
            Validate(end, nameof(end)), true, includeAllPrerelease);

    /// <summary>
    /// Construct a range containing all versions between the given versions including the start
    /// but not the end.
    /// </summary>
    /// <param name="start">The range will contain only versions greater than or equal to this.</param>
    /// <param name="end">The range will contain only versions less than this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given versions if they are prerelease.</param>
    /// <returns>A range containing versions between the given versions including the start but
    /// not the end.</returns>
    public static UnbrokenSemVersionRange InclusiveOfStart(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
        => Create(Validate(start, nameof(start)), true,
            Validate(end, nameof(end)), false, includeAllPrerelease);

    /// <summary>
    /// Construct a range containing all versions between the given versions including the end but
    /// not the start.
    /// </summary>
    /// <param name="start">The range will contain only versions greater than this.</param>
    /// <param name="end">The range will contain only versions less than or equal to this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given versions if they are prerelease.</param>
    /// <returns>A range containing versions between the given versions including the end but
    /// not the start.</returns>
    public static UnbrokenSemVersionRange InclusiveOfEnd(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
        => Create(Validate(start, nameof(start)), false,
            Validate(end, nameof(end)), true, includeAllPrerelease);

    /// <summary>
    /// Construct a range containing all versions between the given versions excluding those versions.
    /// </summary>
    /// <param name="start">The range will contain only versions greater than this.</param>
    /// <param name="end">The range will contain only versions less than this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given versions if they are prerelease.</param>
    /// <returns>A range containing versions between the given versions including the end but
    /// not the start.</returns>
    public static UnbrokenSemVersionRange Exclusive(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
        => Create(Validate(start, nameof(start)), false,
            Validate(end, nameof(end)), false, includeAllPrerelease);
    #endregion

    // TODO support parsing unbroken ranges?

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static UnbrokenSemVersionRange Create(
        SemVersion? startVersion,
        bool startInclusive,
        SemVersion? endVersion,
        bool endInclusive,
        bool includeAllPrerelease)
    {
        var start = new LeftBoundedRange(startVersion, startInclusive);
        var end = new RightBoundedRange(endVersion, endInclusive);
        return Create(start, end, includeAllPrerelease);
    }

    internal static UnbrokenSemVersionRange Create(
        LeftBoundedRange start,
        RightBoundedRange end,
        bool includeAllPrerelease)
    {
        // Always return the same empty range
        if (IsEmpty(start, end, includeAllPrerelease)) return Empty;

        var allPrereleaseCoveredByEnds = false;

        if (start.Version is not null && end.Version is not null)
        {
            // Equals ranges include all prerelease if they are prerelease
            if (start.Version == end.Version)
                allPrereleaseCoveredByEnds = includeAllPrerelease = start.Version.IsPrerelease;
            // Some ranges have all the prerelease versions in them covered by the bounds
            else if (start.IncludesPrerelease || end.IncludesPrerelease)
            {
                if (start.Version.MajorMinorPatchEquals(end.Version))
                    allPrereleaseCoveredByEnds = true;
                else if ((end.IncludesPrerelease || end.Version.PrereleaseIsZero)
                         && start.Version.Major == end.Version.Major && start.Version.Minor == end.Version.Minor
                         && start.Version.Patch + BigInteger.One == end.Version.Patch)
                    allPrereleaseCoveredByEnds = true;
            }
        }

        return new UnbrokenSemVersionRange(start, end, includeAllPrerelease, allPrereleaseCoveredByEnds);
    }

    private UnbrokenSemVersionRange(
        LeftBoundedRange leftBound,
        RightBoundedRange rightBound,
        bool includeAllPrerelease,
        bool allPrereleaseCoveredByEnds)
    {
        LeftBound = leftBound;
        RightBound = rightBound;
        IncludeAllPrerelease = includeAllPrerelease | allPrereleaseCoveredByEnds;
        this.allPrereleaseCoveredByEnds = allPrereleaseCoveredByEnds;
    }

    internal readonly LeftBoundedRange LeftBound;
    internal readonly RightBoundedRange RightBound;
    /// <summary>
    /// If this <see cref="IncludeAllPrerelease"/> and those prerelease versions are entirely
    /// covered by the left and right bounds so that effectively, it doesn't need to include all
    /// prerelease.
    /// </summary>
    private readonly bool allPrereleaseCoveredByEnds;
    private string? toStringCache;

    /// <summary>
    /// The start, left limit, or minimum of this range. Can be <see langword="null"/>.
    /// </summary>
    /// <value>The start or left end of this range. Can be <see langword="null"/>.</value>
    /// <remarks>Ranges with no lower bound have a <see cref="Start"/> value
    /// of <see langword="null"/>. This ensures that they do not unintentionally include any
    /// prerelease versions.</remarks>
    public SemVersion? Start => LeftBound.Version;

    /// <summary>
    /// Whether this range includes the <see cref="Start"/> value.
    /// </summary>
    /// <value>Whether this range includes the <see cref="Start"/> value.</value>
    /// <remarks>When <see cref="Start"/> is <see langword="null"/>, <see cref="StartInclusive"/>
    /// will always be <see langword="false"/>.</remarks>
    [MemberNotNullWhen(true, "Start")]
    public bool StartInclusive => LeftBound.Inclusive;

    /// <summary>
    /// The end, right limit, or maximum of this range. Can be <see langword="null"/>.
    /// </summary>
    /// <value>The end, right limit, or maximum of this range. Can be <see langword="null"/>.</value>
    /// <remarks>Ranges with no upper bound have an <see cref="End"/> value
    /// of <see langword="null"/>.</remarks>
    public SemVersion? End => RightBound.Version;

    /// <summary>
    /// Whether this range includes the <see cref="End"/> value.
    /// </summary>
    /// <value>Whether this range includes the <see cref="End"/> value.</value>
    public bool EndInclusive => RightBound.Inclusive;

    /// <summary>
    /// Whether this range includes all prerelease versions between <see cref="Start"/> and
    /// <see cref="End"/>. If <see cref="IncludeAllPrerelease"/> is <see langword="false"/> then
    /// prerelease versions matching the major, minor, and patch version of the <see cref="Start"/>
    /// or <see cref="End"/> will be included only if that end is a prerelease version.
    /// </summary>
    public bool IncludeAllPrerelease { get; }

    /// <summary>
    /// Determine whether this range contains the given version.
    /// </summary>
    /// <param name="version">The version to test against the range.</param>
    /// <returns><see langword="true"/> if the version is contained in the range,
    /// otherwise <see langword="false"/>.</returns>
    public bool Contains(SemVersion? version)
    {
        if (version is null) throw new ArgumentNullException(nameof(version));

        if (!LeftBound.Contains(version) || !RightBound.Contains(version)) return false;

        if (IncludeAllPrerelease || !version.IsPrerelease) return true;

        // Prerelease versions must match either the start or end
        return Start?.IsPrerelease == true && version.MajorMinorPatchEquals(Start)
               || End?.IsPrerelease == true && version.MajorMinorPatchEquals(End);
    }

    /// <summary>
    /// Convert this range into a predicate function indicating whether a version is contained
    /// in the range.
    /// </summary>
    /// <param name="range">The range to convert into a predicate function.</param>
    /// <returns>A predicate that indicates whether a given version is contained in this range.</returns>
    public static implicit operator Predicate<SemVersion>(UnbrokenSemVersionRange range)
        => range.Contains;

    #region Equality
    /// <summary>
    /// Determines whether two version ranges are equal.
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="other"/> is equal to this range;
    /// otherwise <see langword="false"/>.</returns>
    public bool Equals(UnbrokenSemVersionRange? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return LeftBound.Equals(other.LeftBound)
               && RightBound.Equals(other.RightBound)
               && IncludeAllPrerelease == other.IncludeAllPrerelease;
    }

    /// <summary>
    /// Determines whether the given object is equal to this range.
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is equal to this range;
    /// otherwise <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
        => obj is UnbrokenSemVersionRange other && Equals(other);

    /// <summary>
    /// Gets a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms
    /// and data structures like a hash table.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(LeftBound, RightBound, IncludeAllPrerelease);

    /// <summary>
    /// Determines whether two version ranges are equal.
    /// </summary>
    /// <returns><see langword="true"/> if the two values are equal, otherwise <see langword="false"/>.</returns>
    public static bool operator ==(UnbrokenSemVersionRange? left, UnbrokenSemVersionRange? right)
        => Equals(left, right);

    /// <summary>
    /// Determines whether two version ranges are <em>not</em> equal. Due to the complexity of
    /// ranges, it may be possible for two ranges to match the same set of versions but be
    /// expressed in different ways and so not be equal.
    /// </summary>
    /// <returns><see langword="true"/> if the two ranges are <em>not</em> equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(UnbrokenSemVersionRange? left, UnbrokenSemVersionRange? right)
        => !Equals(left, right);
    #endregion

    /// <summary>
    /// Converts this version range to an equivalent string value in
    /// <a href="https://semver-nuget.org/ranges/">standard range syntax</a>.
    /// </summary>
    /// <returns>
    /// The <see cref="string" /> equivalent of this version in
    /// <a href="https://semver-nuget.org/ranges/">standard range syntax</a>.
    /// </returns>
    /// <remarks>Ranges including all prerelease versions are indicated with the idiom of prefixing
    /// with "<c>*-*</c>". This includes all prerelease versions because it matches all prerelease
    /// versions.</remarks>
    public override string ToString()
        => toStringCache ??= ToStringInternal();

    private string ToStringInternal()
    {
        if (this == Empty)
            // Must combine with including prerelease and still be empty
            return "<0.0.0-0";

        // Simple Equals ranges
        if (StartInclusive && RightBound.Inclusive && SemVersion.Equals(Start, End))
            return Start.ToString();

        var includesPrereleaseNotCoveredByEnds = IncludeAllPrerelease && !allPrereleaseCoveredByEnds;

        // All versions ranges
        var leftUnbounded = LeftBound == LeftBoundedRange.Unbounded;
        var rightUnbounded = RightBound == RightBoundedRange.Unbounded;
        if (leftUnbounded && rightUnbounded)
            return includesPrereleaseNotCoveredByEnds ? "*-*" : "*";

        if (TryToSpecialString(includesPrereleaseNotCoveredByEnds, out var result))
            return result;

        string range;
        if (leftUnbounded)
            range = RightBound.ToString();
        else if (rightUnbounded)
            range = LeftBound.ToString();
        else
            range = $"{LeftBound} {RightBound}";

        return includesPrereleaseNotCoveredByEnds ? "*-* " + range : range;
    }

    private bool TryToSpecialString(bool includesPrereleaseNotCoveredByEnds, [NotNullWhen(true)] out string? result)
    {
        // Most special ranges follow the pattern '>=X.Y.Z <P.Q.R-0'
        if (StartInclusive && !RightBound.Inclusive && End?.PrereleaseIsZero == true)
        {
            // Wildcard Ranges like 2.*, 2.*-*, 2.3.*, and 2.3.*-*
            if (Start.Patch == 0 && End.Patch == 0 && (!Start.IsPrerelease || Start.PrereleaseIsZero))
            {
                if (Start.Major == End.Major && Start.Minor + BigInteger.One == End.Minor)
                    // Wildcard patch
                    result = $"{Start.Major}.{Start.Minor}.*";
                else if (Start.Major + BigInteger.One == End.Major && Start.Minor == 0 && End.Minor == 0)
                    // Wildcard minor
                    result = $"{Start.Major}.*";
                else
                    goto tilde;

                if (!includesPrereleaseNotCoveredByEnds)
                    return true;

                result = Start.PrereleaseIsZero ? result + "-*" : "*-* " + result;
                return true;
            }

            // Wildcard ranges like 2.1.4-* follow the pattern '>=X.Y.Z-0 <X.Y.(Z+1)-0'
            if (Start.PrereleaseIsZero
                && Start.Major == End.Major && Start.Minor == End.Minor
                && Start.Patch + BigInteger.One == End.Patch)
            {
                result = $"{Start.Major}.{Start.Minor}.{Start.Patch}-*";
                return true;
            }

        tilde:
            // Tilde ranges like ~1.2.3, and ~1.2.3-rc
            if (Start.Major == End.Major
                && Start.Minor + BigInteger.One == End.Minor && End.Patch == 0)
            {
                result = (includesPrereleaseNotCoveredByEnds ? "*-* ~" : "~") + Start;
                return true;
            }

            // Note: caret ranges like ^0.1.2 and ^0.2.3-rc are converted to tilde ranges

            if (Start.Major != 0)
            {
                // Caret ranges like ^1.2.3 and ^1.2.3-rc
                if (Start.Major + 1 == End.Major && End.Minor == 0 && End.Patch == 0)
                {
                    result = (includesPrereleaseNotCoveredByEnds ? "*-* ^" : "^") + Start;
                    return true;
                }
            }
            else if (End.Major == 0
                     && Start.Minor == 0 && End.Minor == 0
                     && Start.Patch + BigInteger.One == End.Patch)
            {
                // Caret ranges like ^0.0.2 and ^0.0.2-rc
                result = (includesPrereleaseNotCoveredByEnds ? "*-* ^" : "^") + Start;
                return true;
            }
        }

        // Assign null once
        result = null;

        // Wildcards with prerelease follow the pattern >=X.Y.Z-φ.α.0 <X.Y.Z-φ.β
        if (StartInclusive && !RightBound.Inclusive
            && LeftBound.Version?.MajorMinorPatchEquals(RightBound.Version) == true
            && LeftBound.Version.IsPrerelease && RightBound.Version.IsPrerelease)
        {
            var leftPrerelease = LeftBound.Version.PrereleaseIdentifiers;
            var rightPrerelease = RightBound.Version.PrereleaseIdentifiers;
            if (leftPrerelease.Count < 2
                || leftPrerelease[^1] != PrereleaseIdentifier.Zero
                || leftPrerelease.Count - 1 != rightPrerelease.Count)
                return false;

            // But they must be equal in prerelease up to the correct point
            for (int i = 0; i < leftPrerelease.Count - 2; i++)
                if (leftPrerelease[i] != rightPrerelease[i])
                    return false;

            // And the prerelease identifiers must have the correct relationship
            if (leftPrerelease[^2].NextIdentifier()
                != rightPrerelease[^1])
                return false;

            var originalPrerelease = string.Join(".", leftPrerelease.Take(leftPrerelease.Count - 1));
            result = $"{Start.Major}.{Start.Minor}.{Start.Patch}-{originalPrerelease}.*";
            return true;
        }

        return false;
    }

    internal bool Overlaps(UnbrokenSemVersionRange other)
    {
        // The empty range doesn't overlap anything, but passes the test below in some cases
        if (Empty.Equals(this) || Empty.Equals(other)) return false;

        // see https://stackoverflow.com/a/3269471/268898
        return LeftBound.CompareTo(other.RightBound) <= 0
               && other.LeftBound.CompareTo(RightBound) <= 0;
    }

    internal bool OverlapsOrAbuts(UnbrokenSemVersionRange other)
    {
        if (Overlaps(other)) return true;

        // The empty range is never considered to abut anything even though its "ends" might
        // actually abut things.
        if (Empty.Equals(this) || Empty.Equals(other)) return false;

        // To check abutting, we just need to put them in the right order and check the gap between them
        var isLessThanOrEqual = UnbrokenSemVersionRangeComparer.Instance.Compare(this, other) <= 0;
        var leftRangeEnd = (isLessThanOrEqual ? this : other).RightBound;
        var rightRangeStart = (isLessThanOrEqual ? other : this).LeftBound;

        // Note: the case where a major, minor, or patch version is at max value and so is just
        // less than the next prerelease version is being ignored.

        // If one of the ends is inclusive then it is sufficient for them to be the same version.
        if ((leftRangeEnd.Inclusive || rightRangeStart.Inclusive)
            && leftRangeEnd.Version?.Equals(rightRangeStart.Version) == true)
            return true;

        // But they could also abut if the prerelease versions between them are being excluded.
        if (IncludeAllPrerelease || other.IncludeAllPrerelease
            || leftRangeEnd.IncludesPrerelease || rightRangeStart.IncludesPrerelease)
            return false;

        return rightRangeStart.Inclusive
               && leftRangeEnd.Version?.MajorMinorPatchEquals(rightRangeStart.Version) == true;
    }

    /// <summary>
    /// Whether this range contains the other. For this to be the case, it must contain all the
    /// versions accounting for which prerelease versions are in each range.
    /// </summary>
    internal bool Contains(UnbrokenSemVersionRange other)
    {
        // The empty set is a subset of every other set, even itself
        if (other == Empty) return true;

        // It contains prerelease we don't
        if (other.IncludeAllPrerelease && !IncludeAllPrerelease) return false;

        // If our bounds don't contain the other bounds, there is no containment
        if (LeftBound.CompareTo(other.LeftBound) > 0
            || other.RightBound.CompareTo(RightBound) > 0) return false;

        // Our bounds contain the other bounds, but that doesn't mean it contains if there
        // are prerelease versions that are being missed.

        // If we contain all prerelease versions, it is safe
        if (IncludeAllPrerelease) return true;

        // Make sure we include prerelease at the start
        if (other.LeftBound.IncludesPrerelease)
        {
            if (!(Start?.IsPrerelease ?? false)
                || !Start.MajorMinorPatchEquals(other.Start)) return false;
        }

        // Make sure we include prerelease at the end
        return !other.RightBound.IncludesPrerelease
               || (End?.IsPrerelease == true && End.MajorMinorPatchEquals(other.End));
    }

    /// <summary>
    /// Try to union this range with the other. This is a complex operation because it must
    /// account for prerelease versions that may be accepted at the endpoints of the ranges.
    /// </summary>
    internal bool TryUnion(UnbrokenSemVersionRange other, [NotNullWhen(true)] out UnbrokenSemVersionRange? union)
    {
        // First deal with simple containment. This handles cases where the containing range
        // includes all prerelease that aren't handled with the union below. It also handles
        // containment of empty ranges.
        if (Contains(other))
        {
            union = this;
            return true;
        }

        if (other.Contains(this))
        {
            union = other;
            return true;
        }

        // Assign null once, so it doesn't need to be assigned in every return case
        union = null;

        // Can't union ranges with different prerelease coverage
        if (IncludeAllPrerelease != other.IncludeAllPrerelease) return false;

        // No overlap means no union
        if (!OverlapsOrAbuts(other)) return false;

        var leftBound = LeftBound.Min(other.LeftBound);
        var rightBound = RightBound.Max(other.RightBound);
        var includeAllPrerelease = IncludeAllPrerelease; // note that other.IncludeAllPrerelease is equal

        // Create the union early to use it for containment checks
        var possibleUnion = Create(leftBound, rightBound, includeAllPrerelease);

        // If all prerelease is included, then the prerelease versions from the dropped ends
        // will be covered.
        if (!includeAllPrerelease)
        {
            var otherLeftBound = LeftBound.Max(other.LeftBound);
            if (otherLeftBound.IncludesPrerelease
                && !possibleUnion.Contains(otherLeftBound.Version))
                return false;

            var otherRightBound = RightBound.Min(other.RightBound);
            if (otherRightBound.IncludesPrerelease
                && !possibleUnion.Contains(otherRightBound.Version))
                return false;
        }

        union = possibleUnion;
        return true;
    }

    private static bool IsEmpty(LeftBoundedRange start, RightBoundedRange end, bool includeAllPrerelease)
    {
        // Ranges with unbounded ends aren't empty
        if (end.Version is null) return false;

        var comparison = SemVersion.ComparePrecedence(start.Version, end.Version);
        if (comparison > 0) return true;
        if (comparison == 0) return !(start.Inclusive && end.Inclusive);

        // else start < end

        if (start.Version is null)
        {
            if (end.Inclusive) return false;
            // A range like "<0.0.0" is empty if prerelease isn't allowed and
            // "<0.0.0-0" is empty even it if isn't
            return end.Version == SemVersion.Min
                   || (!includeAllPrerelease && end.Version == SemVersion.MinRelease);
        }

        // A range like ">1.0.0 <1.0.1" is still empty if prerelease isn't allowed.
        // If prerelease is allowed, there is always an infinite number of versions in the range
        // (e.g. ">1.0.0-0 <1.0.1-0" contains "1.0.0-0.between").
        if (start.Inclusive || end.Inclusive
            || includeAllPrerelease || start.Version.IsPrerelease || end.Version.IsPrerelease)
            return false;

        return start.Version.Major == end.Version.Major
               && start.Version.Minor == end.Version.Minor
               && start.Version.Patch + BigInteger.One == end.Version.Patch;
    }

    private static SemVersion Validate(SemVersion version, string paramName)
    {
        if (version is null) throw new ArgumentNullException(paramName);
        if (version.MetadataIdentifiers.Count > 0) throw new ArgumentException(InvalidMetadataMessage, paramName);
        return version;
    }

    private const string InvalidMetadataMessage = "Cannot have metadata.";
}
