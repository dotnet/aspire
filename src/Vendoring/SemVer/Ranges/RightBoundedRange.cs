using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Semver.Comparers;
using Semver.Utility;

namespace Semver.Ranges;

/// <summary>
/// A range of versions that is bounded only on the right. That is a range defined by some
/// version <c>x</c> such that <c>v &lt; x</c> or <c>v &lt;= x</c> depending on whether it is
/// inclusive. A right-bounded range forms the upper limit for a version range.
/// </summary>
/// <remarks>An "unbounded" right-bounded range is represented by an inclusive upper bound of
/// <see langword="null"/> since there is no maximum <see cref="SemVersion"/>. This requires
/// special checking for comparison.</remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct RightBoundedRange : IEquatable<RightBoundedRange>
{
    public static readonly RightBoundedRange Unbounded
        = new RightBoundedRange(null, false);

    public RightBoundedRange(SemVersion? version, bool inclusive)
    {
#if DEBUG
        // dotcover disable
        if (version is null && inclusive)
            throw new ArgumentException("DEBUG: Cannot be inclusive of end without end value.",
                nameof(inclusive));
        // dotcover enable
#endif
        DebugChecks.NoMetadata(version, nameof(version));

        Version = version;
        Inclusive = inclusive;
    }

    public SemVersion? Version { get; }

    [MemberNotNullWhen(true, "Version")]
    public bool Inclusive { get; }

    /// <summary>
    /// Whether this bound actually includes any prerelease versions.
    /// </summary>
    /// <remarks>
    /// A non-inclusive bound of X.Y.Z-0 doesn't actually include any prerelease versions.
    /// </remarks>
    public bool IncludesPrerelease
        => Version?.IsPrerelease == true && !(!Inclusive && Version.PrereleaseIsZero);

    public bool Contains(SemVersion version)
    {
        DebugChecks.IsNotNull(version, nameof(version));
        if (Version is null) return true;
        var comparison = SemVersion.ComparePrecedence(version, Version);
        return Inclusive ? comparison <= 0 : comparison < 0;
    }

    public RightBoundedRange Min(RightBoundedRange other)
        => CompareTo(other) <= 0 ? this : other;

    public RightBoundedRange Max(RightBoundedRange other)
        => CompareTo(other) >= 0 ? this : other;

    #region Equality
    public bool Equals(RightBoundedRange other)
        => Equals(Version, other.Version) && Inclusive == other.Inclusive;

    public override bool Equals(object? obj)
        => obj is RightBoundedRange other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Version, Inclusive);

    public static bool operator ==(RightBoundedRange left, RightBoundedRange right)
        => left.Equals(right);

    public static bool operator !=(RightBoundedRange left, RightBoundedRange right)
        => !left.Equals(right);
    #endregion

    public int CompareTo(RightBoundedRange other)
    {
        switch (Version, other.Version)
        {
            case (null,null): return 0;
            case (null, _): return 1;
            case (_, null): return -1;
            default:
                var comparison = PrecedenceComparer.Instance.Compare(Version!, other.Version!);
                if (comparison != 0) return comparison;
                return Inclusive.CompareTo(other.Inclusive);
        }
    }

    public override string ToString() => (Inclusive ? "<=" : "<") + (Version?.ToString() ?? "null");
}
