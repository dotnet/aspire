using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Semver.Comparers;
using Semver.Parsing;
using Semver.Utility;

namespace Semver;

/// <summary>
/// A range of <see cref="SemVersion"/> values. A range can have gaps in it and may include only
/// some prerelease versions between included release versions. For a range that cannot have
/// gaps see the <see cref="UnbrokenSemVersionRange"/> class.
/// </summary>
public sealed class SemVersionRange : IReadOnlyList<UnbrokenSemVersionRange>, IEquatable<SemVersionRange>
{
    internal const int MaxRangeLength = 2048;
    internal const string InvalidOptionsMessage = "An invalid SemVersionRangeOptions value was used.";
    internal const string InvalidMaxLengthMessage = "Must not be negative.";

    /// <summary>
    /// The empty range that contains no versions.
    /// </summary>
    /// <value>The empty range that contains no versions.</value>
    public static SemVersionRange Empty { get; } = new SemVersionRange(ReadOnlyList<UnbrokenSemVersionRange>.Empty);

    /// <summary>
    /// The range that contains all release versions but no prerelease versions.
    /// </summary>
    /// <value>The range that contains all release versions but no prerelease versions.</value>
    public static SemVersionRange AllRelease { get; } = new SemVersionRange(UnbrokenSemVersionRange.AllRelease);

    /// <summary>
    /// The range that contains both all release and prerelease versions.
    /// </summary>
    /// <value>The range that contains both all release and prerelease versions.</value>
    public static SemVersionRange All { get; } = new SemVersionRange(UnbrokenSemVersionRange.All);

    /// <summary>
    /// Construct a range containing only a single version.
    /// </summary>
    /// <param name="version">The version the range should contain.</param>
    /// <returns>A range containing only the given version.</returns>
    public static SemVersionRange Equals(SemVersion version)
        => Create(UnbrokenSemVersionRange.Equals(version));

    /// <summary>
    /// Construct a range containing versions greater than the given version.
    /// </summary>
    /// <param name="version">The range will contain all versions greater than this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given version if it is prerelease.</param>
    /// <returns>A range containing versions greater than the given version.</returns>
    public static SemVersionRange GreaterThan(SemVersion version, bool includeAllPrerelease = false)
        => Create(UnbrokenSemVersionRange.GreaterThan(version, includeAllPrerelease));

    /// <summary>
    /// Construct a range containing versions equal to or greater than the given version.
    /// </summary>
    /// <param name="version">The range will contain all versions greater than or equal to this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given version if it is prerelease.</param>
    /// <returns>A range containing versions greater than or equal to the given version.</returns>
    public static SemVersionRange AtLeast(SemVersion version, bool includeAllPrerelease = false)
         => Create(UnbrokenSemVersionRange.AtLeast(version, includeAllPrerelease));

    /// <summary>
    /// Construct a range containing versions less than the given version.
    /// </summary>
    /// <param name="version">The range will contain all versions less than this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given version if it is prerelease.</param>
    /// <returns>A range containing versions less than the given version.</returns>
    public static SemVersionRange LessThan(SemVersion version, bool includeAllPrerelease = false)
         => Create(UnbrokenSemVersionRange.LessThan(version, includeAllPrerelease));

    /// <summary>
    /// Construct a range containing versions equal to or less than the given version.
    /// </summary>
    /// <param name="version">The range will contain all versions less than or equal to this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given version if it is prerelease.</param>
    /// <returns>A range containing versions less than or equal to the given version.</returns>
    public static SemVersionRange AtMost(SemVersion version, bool includeAllPrerelease = false)
        => Create(UnbrokenSemVersionRange.AtMost(version, includeAllPrerelease));

    /// <summary>
    /// Construct a range containing all versions between the given versions including those versions.
    /// </summary>
    /// <param name="start">The range will contain only versions greater than or equal to this.</param>
    /// <param name="end">The range will contain only versions less than or equal to this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given versions if they are prerelease.</param>
    /// <returns>A range containing versions between the given versions including those versions.</returns>
    public static SemVersionRange Inclusive(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
        => Create(UnbrokenSemVersionRange.Inclusive(start, end, includeAllPrerelease));

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
    public static SemVersionRange InclusiveOfStart(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
        => Create(UnbrokenSemVersionRange.InclusiveOfStart(start, end, includeAllPrerelease));

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
    public static SemVersionRange InclusiveOfEnd(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
        => Create(UnbrokenSemVersionRange.InclusiveOfEnd(start, end, includeAllPrerelease));

    /// <summary>
    /// Construct a range containing all versions between the given versions excluding those versions.
    /// </summary>
    /// <param name="start">The range will contain only versions greater than this.</param>
    /// <param name="end">The range will contain only versions less than this.</param>
    /// <param name="includeAllPrerelease">Include all prerelease versions in the range rather
    /// than just those matching the given versions if they are prerelease.</param>
    /// <returns>A range containing versions between the given versions including the end but
    /// not the start.</returns>
    public static SemVersionRange Exclusive(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
        => Create(UnbrokenSemVersionRange.Exclusive(start, end, includeAllPrerelease));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SemVersionRange Create(UnbrokenSemVersionRange range)
        => UnbrokenSemVersionRange.Empty.Equals(range) ? Empty : new SemVersionRange(range);

    /// <remarks>Ownership of the <paramref name="ranges"/> list must be given to this method.
    /// The list will be mutated and used as the basis of an immutable list. It must not still
    /// be referenced by the caller.</remarks>
    internal static SemVersionRange Create(List<UnbrokenSemVersionRange> ranges)
    {
        DebugChecks.IsNotNull(ranges, nameof(ranges));

        // Remove empty ranges and see if the result is empty
        ranges.RemoveAll(range => UnbrokenSemVersionRange.Empty.Equals(range));
        if (ranges.Count == 0) return Empty;

        // Sort and merge ranges
        ranges.Sort(UnbrokenSemVersionRangeComparer.Instance);
        for (var i = 0; i < ranges.Count - 1; i++)
            for (var j = ranges.Count - 1; j > i; j--)
                if (ranges[i].TryUnion(ranges[j], out var union))
                {
                    ranges.RemoveAt(j);
                    ranges[i] = union;
                }

        return new SemVersionRange(ranges.AsReadOnly());
    }

    /// <summary>
    /// Construct a range that joins the given <see cref="UnbrokenSemVersionRange"/>s. It will
    /// contain all versions contained by any of the given unbroken ranges.
    /// </summary>
    /// <param name="ranges">The unbroken ranges to join into a single range.</param>
    /// <returns>A range that joins the given <see cref="UnbrokenSemVersionRange"/>s. It will
    /// contain all versions contained by any of the given unbroken ranges.</returns>
    /// <remarks>The unbroken ranges are simplified and sorted. So, the resulting
    /// <see cref="SemVersionRange"/> may not contain the same number of
    /// <see cref="UnbrokenSemVersionRange"/>s in the same order as passed to this method.</remarks>
    public static SemVersionRange Create(IEnumerable<UnbrokenSemVersionRange> ranges)
        => Create((ranges ?? throw new ArgumentNullException(nameof(ranges))).ToList());

    /// <summary>
    /// Construct a range that joins the given <see cref="UnbrokenSemVersionRange"/>s. It will
    /// contain all versions contained by any of the given unbroken ranges.
    /// </summary>
    /// <param name="ranges">The unbroken ranges to join into a single range.</param>
    /// <returns>A range that joins the given <see cref="UnbrokenSemVersionRange"/>s. It will
    /// contain all versions contained by any of the given unbroken ranges.</returns>
    /// <remarks>The unbroken ranges are simplified and sorted. So, the resulting
    /// <see cref="SemVersionRange"/> may not contain the same number of
    /// <see cref="UnbrokenSemVersionRange"/>s in the same order as passed to this method.</remarks>
    public static SemVersionRange Create(params UnbrokenSemVersionRange[] ranges)
        => Create((ranges ?? throw new ArgumentNullException(nameof(ranges))).ToList());

    private readonly IReadOnlyList<UnbrokenSemVersionRange> ranges;

    /// <remarks>Parameter validation is not performed. The unbroken range must not be empty.</remarks>
    private SemVersionRange(UnbrokenSemVersionRange range)
        : this(new List<UnbrokenSemVersionRange>(1) { range }.AsReadOnly())
    {
    }

    /// <remarks>Parameter validation is not performed. The <paramref name="ranges"/> must be
    /// an immutable list of properly ordered and combined ranges.</remarks>
    private SemVersionRange(IReadOnlyList<UnbrokenSemVersionRange> ranges)
    {
        this.ranges = ranges;
    }

    /// <summary>
    /// Determine whether this range contains the given version.
    /// </summary>
    /// <param name="version">The version to test against the range.</param>
    /// <returns><see langword="true"/> if the version is contained in the range,
    /// otherwise <see langword="false"/>.</returns>
    public bool Contains(SemVersion? version)
    {
        // Using `for` loop for better performance
        for (var i = 0; i < ranges.Count; i++)
            if (ranges[i].Contains(version))
                return true;

        return false;
    }

    /// <summary>
    /// Convert this range into a predicate function indicating whether a version is contained
    /// in the range.
    /// </summary>
    /// <param name="range">The range to convert into a predicate function.</param>
    /// <returns>A predicate that indicates whether a given version is contained in this range.</returns>
    public static implicit operator Predicate<SemVersion>(SemVersionRange range)
        => range.Contains;

    #region Standard Parsing
    /// <summary>
    /// Convert a version range string in the <a href="https://semver-nuget.org/ranges/">standard
    /// range syntax</a> into a <see cref="SemVersionRange"/> using the given options.
    /// </summary>
    /// <param name="range">The version range string to parse. Accepts the
    /// <a href="https://semver-nuget.org/ranges/">standard range syntax</a>.</param>
    /// <param name="options">A bitwise combination of enumeration values that indicates the style
    /// elements that can be present in <paramref name="range"/> and the options to use when
    /// constructing the range.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long version range strings.</param>
    /// <exception cref="ArgumentException"><paramref name="options"/> is not a valid
    /// <see cref="SemVersionRangeOptions"/> value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The <paramref name="range"/> is invalid or not in a
    /// format compliant with <paramref name="options"/>.</exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static SemVersionRange Parse(
        string range,
        SemVersionRangeOptions options,
        int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        if (!options.IsValid())
            throw new ArgumentException(InvalidOptionsMessage, nameof(options));
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, InvalidMaxLengthMessage);

        var ex = StandardRangeParser.Parse(range, options, null, maxLength, out var semverRange);
        if (ex != null) throw ex;
        DebugChecks.IsNotNull(semverRange, nameof(semverRange));
        return semverRange;
    }

    /// <summary>
    /// Convert a version range string in the <a href="https://semver-nuget.org/ranges/">standard
    /// range syntax</a> into a <see cref="SemVersionRange"/> using the
    /// <see cref="SemVersionRangeOptions.Strict"/> option.
    /// </summary>
    /// <param name="range">The version range string to parse. Accepts the
    /// <a href="https://semver-nuget.org/ranges/">standard range syntax</a>.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long version range strings.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The <paramref name="range"/> is invalid or not in a
    /// format compliant with the <see cref="SemVersionRangeOptions.Strict"/> option.</exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static SemVersionRange Parse(
        string range,
        int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        => Parse(range, SemVersionRangeOptions.Strict, maxLength);

    /// <summary>
    /// Convert a version range string in the <a href="https://semver-nuget.org/ranges/">standard
    /// range syntax</a> into a <see cref="SemVersionRange"/> using the given options.
    /// </summary>
    /// <param name="range">The version range string to parse. Accepts the
    /// <a href="https://semver-nuget.org/ranges/">standard range syntax</a>.</param>
    /// <param name="options">A bitwise combination of enumeration values that indicates the style
    /// elements that can be present in <paramref name="range"/> and the options to use when
    /// constructing the range.</param>
    /// <param name="semverRange">The converted <see cref="SemVersionRange"/>.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long version range strings.</param>
    /// <returns><see langword="false"/> when an invalid version range string is passed,
    /// otherwise <see langword="true"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="options"/> is not a valid
    /// <see cref="SemVersionRangeOptions"/> value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static bool TryParse(
        string? range,
        SemVersionRangeOptions options,
        [NotNullWhen(true)]
        out SemVersionRange? semverRange,
        int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        if (!options.IsValid()) throw new ArgumentException(InvalidOptionsMessage, nameof(options));
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, InvalidMaxLengthMessage);

        var exception = StandardRangeParser.Parse(range, options, VersionParsing.FailedException, maxLength, out semverRange);

        DebugChecks.IsNotFailedException(exception, nameof(SemVersionParser), nameof(SemVersionParser.Parse));

        return exception is null;
    }

    /// <summary>
    /// Convert a version range string in the <a href="https://semver-nuget.org/ranges/">standard
    /// range syntax</a> into a <see cref="SemVersionRange"/> using the
    /// <see cref="SemVersionRangeOptions.Strict"/> option.
    /// </summary>
    /// <param name="range">The version range string to parse. Accepts the
    /// <a href="https://semver-nuget.org/ranges/">standard range syntax</a>.</param>
    /// <param name="semverRange">The converted <see cref="SemVersionRange"/>.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long version range strings.</param>
    /// <returns><see langword="false"/> when an invalid version range string is passed,
    /// otherwise <see langword="true"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static bool TryParse(
        string? range,
        [NotNullWhen(true)]
        out SemVersionRange? semverRange,
        int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        => TryParse(range, SemVersionRangeOptions.Strict, out semverRange, maxLength);
    #endregion

    #region npm Parsing
    /// <summary>
    /// Convert a version range string in the <a href="https://github.com/npm/node-semver/#ranges">
    /// npm syntax</a> into a <see cref="SemVersionRange"/>.
    /// </summary>
    /// <param name="range">The version range string to parse. Accepts the
    /// <a href="https://github.com/npm/node-semver/#ranges">npm syntax</a>.</param>
    /// <param name="includeAllPrerelease">Whether to include all prerelease versions in the
    /// range rather than just prerelease versions matching a prerelease identifier in the range.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long version range strings.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The <paramref name="range"/> is invalid.</exception>
    /// <remarks>The npm "loose" option is not supported. The
    /// <see cref="Parse(string,SemVersionRangeOptions,int)"/> method provides more control over
    /// parsing. However, it does not accept all the npm syntax.</remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static SemVersionRange ParseNpm(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        string range,
        bool includeAllPrerelease,
        int maxLength = MaxRangeLength)
    {
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, InvalidMaxLengthMessage);

        var ex = NpmRangeParser.Parse(range, includeAllPrerelease, null, maxLength, out var semverRange);
        if (ex != null) throw ex;
        DebugChecks.IsNotNull(semverRange, nameof(semverRange));
        return semverRange;
    }

    /// <summary>
    /// Convert a version range string in the <a href="https://github.com/npm/node-semver/#ranges">
    /// npm syntax</a> into a <see cref="SemVersionRange"/>.
    /// </summary>
    /// <param name="range">The version range string to parse. Accepts the
    /// <a href="https://github.com/npm/node-semver/#ranges">npm syntax</a>.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long version range strings.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The <paramref name="range"/> is invalid.</exception>
    /// <remarks>The npm "loose" option is not supported. The
    /// <see cref="Parse(string,SemVersionRangeOptions,int)"/> method provides more control over
    /// parsing. However, it does not accept all the npm syntax.</remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static SemVersionRange ParseNpm(string range, int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        => ParseNpm(range, false, maxLength);

    /// <summary>
    /// Convert a version range string in the <a href="https://github.com/npm/node-semver/#ranges">
    /// npm syntax</a> into a <see cref="SemVersionRange"/>.
    /// </summary>
    /// <param name="range">The version range string to parse. Accepts the
    /// <a href="https://github.com/npm/node-semver/#ranges">npm syntax</a>.</param>
    /// <param name="includeAllPrerelease">Whether to include all prerelease versions in the
    /// range rather than just prerelease versions matching a prerelease identifier in the range.</param>
    /// <param name="semverRange">The converted <see cref="SemVersionRange"/>.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long version range strings.</param>
    /// <returns><see langword="false"/> when an invalid version range string is passed,
    /// otherwise <see langword="true"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
    /// <remarks>The npm "loose" option is not supported. The
    /// <see cref="TryParse(string,SemVersionRangeOptions,out SemVersionRange,int)"/> method
    /// provides more control over parsing. However, it does not accept all the npm syntax.</remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static bool TryParseNpm(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        string? range,
        bool includeAllPrerelease,
        [NotNullWhen(true)]
        out SemVersionRange? semverRange,
        int maxLength = MaxRangeLength)
    {
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, InvalidMaxLengthMessage);

        var exception = NpmRangeParser.Parse(range, includeAllPrerelease, VersionParsing.FailedException, maxLength, out semverRange);

        DebugChecks.IsNotFailedException(exception, nameof(NpmRangeParser), nameof(NpmRangeParser.Parse));

        return exception is null;
    }

    /// <summary>
    /// Convert a version range string in the <a href="https://github.com/npm/node-semver/#ranges">
    /// npm syntax</a> into a <see cref="SemVersionRange"/>.
    /// </summary>
    /// <param name="range">The version range string to parse. Accepts the
    /// <a href="https://github.com/npm/node-semver/#ranges">npm syntax</a>.</param>
    /// <param name="semverRange">The converted <see cref="SemVersionRange"/>.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long version range strings.</param>
    /// <returns><see langword="false"/> when an invalid version range string is passed,
    /// otherwise <see langword="true"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
    /// <remarks>The npm "loose" option is not supported. The
    /// <see cref="TryParse(string,SemVersionRangeOptions,out SemVersionRange,int)"/> method
    /// provides more control over parsing. However, it does not accept all the npm syntax.</remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static bool TryParseNpm(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        string? range,
        [NotNullWhen(true)]
        out SemVersionRange? semverRange,
        int maxLength = MaxRangeLength)
        => TryParseNpm(range, false, out semverRange, maxLength);
    #endregion

    #region IReadOnlyList<UnbrokenSemVersionRange>
    /// <summary>
    /// The number of <see cref="UnbrokenSemVersionRange"/>s that make up this range.
    /// </summary>
    /// <value>The number of <see cref="UnbrokenSemVersionRange"/>s that make up this range.</value>
    public int Count => ranges.Count;

    /// <summary>
    /// Get the <see cref="UnbrokenSemVersionRange"/> making up this range at the given index.
    /// </summary>
    /// <param name="index">The zero-based index of the <see cref="UnbrokenSemVersionRange"/> to
    /// get.</param>
    /// <returns>The <see cref="UnbrokenSemVersionRange"/> making up this range at the given
    /// index.</returns>
    public UnbrokenSemVersionRange this[int index] => ranges[index];

    /// <summary>
    /// Get an enumerator that iterates through the <see cref="UnbrokenSemVersionRange"/>s
    /// making up this range.
    /// </summary>
    /// <returns>An enumerator that iterates through the <see cref="UnbrokenSemVersionRange"/>s
    /// making up this range.</returns>
    public IEnumerator<UnbrokenSemVersionRange> GetEnumerator() => ranges.GetEnumerator();

    /// <summary>
    /// Get an enumerator that iterates through the <see cref="UnbrokenSemVersionRange"/>s
    /// making up this range.
    /// </summary>
    /// <returns>An enumerator that iterates through the <see cref="UnbrokenSemVersionRange"/>s
    /// making up this range.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    #region Equality
    /// <summary>
    /// Determines whether two version ranges are equal. Due to the complexity of ranges, it may
    /// be possible for two ranges to match the same set of versions but be expressed in
    /// different ways and so not be equal.
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="other"/> is equal to this range;
    /// otherwise <see langword="false"/>.</returns>
    public bool Equals(SemVersionRange? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ranges.SequenceEqual(other.ranges);
    }

    /// <summary>
    /// Determines whether the given object is equal to this range. Due to the complexity of
    /// ranges, it may be possible for two ranges to match the same set of versions but be
    /// expressed in different ways and so not be equal.
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is equal to this range;
    /// otherwise <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
        => obj is SemVersionRange other && Equals(other);

    /// <summary>
    /// Gets a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms
    /// and data structures like a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        var hashcode = new HashCode();
        foreach (var range in ranges) hashcode.Add(range);
        return hashcode.ToHashCode();
    }

    /// <summary>
    /// Determines whether two version ranges are equal. Due to the complexity of ranges, it may
    /// be possible for two ranges to match the same set of versions but be expressed in
    /// different ways and so not be equal.
    /// </summary>
    /// <returns><see langword="true"/> if the two values are equal, otherwise <see langword="false"/>.</returns>
    public static bool operator ==(SemVersionRange? left, SemVersionRange? right)
        => Equals(left, right);

    /// <summary>
    /// Determines whether two version ranges are <em>not</em> equal. Due to the complexity of
    /// ranges, it may be possible for two ranges to match the same set of versions but be
    /// expressed in different ways and so not be equal.
    /// </summary>
    /// <returns><see langword="true"/> if the two ranges are <em>not</em> equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(SemVersionRange? left, SemVersionRange? right)
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
    /// <remarks>Because version ranges are simplified and sorted, this method may return
    /// a different, but equivalent, range string from the originally parsed one. Ranges
    /// including all prerelease versions are indicated with the idiom of prefixing each
    /// comparison group with "<c>*-*</c>". This includes all prerelease versions because it
    /// matches all prerelease versions.</remarks>
    public override string ToString() => string.Join(" || ", this);
}
