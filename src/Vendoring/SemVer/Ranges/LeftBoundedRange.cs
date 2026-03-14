using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Semver.Comparers;
using Semver.Utility;

namespace Semver.Ranges;

/// <summary>
/// A range of versions that is bounded only on the left. That is a range defined by some version
/// <c>x</c> such that <c>x &lt; v</c> or <c>x &lt;= v</c> depending on whether it is inclusive.
/// A left-bounded range forms the lower limit for a version range.
/// </summary>
/// <remarks>An "unbounded" left-bounded range is represented by a lower bound of
/// <see langword="null"/> since <see langword="null"/> compares as less than all versions.
/// However, it does not allow such ranges to be inclusive because a range cannot contain null.
/// The <see cref="SemVersion.Min"/> (i.e. <c>0.0.0-0</c>) cannot be used instead
/// because it would be inclusive of prerelease.</remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LeftBoundedRange : IEquatable<LeftBoundedRange>
{
    public static readonly LeftBoundedRange Unbounded = new LeftBoundedRange(null, false);

    public LeftBoundedRange(SemVersion? version, bool inclusive)
    {
#if DEBUG
        // dotcover disable
        if (version is null && inclusive)
            throw new ArgumentException("DEBUG: Cannot be inclusive of start without start value.", nameof(inclusive));
        // dotcover enable
#endif
        DebugChecks.NoMetadata(version, nameof(version));

        Version = version;
        Inclusive = inclusive;
    }

    public SemVersion? Version { get; }

    [MemberNotNullWhen(true, "Version")]
    public bool Inclusive { get; }

    public bool IncludesPrerelease => Version?.IsPrerelease == true;

    public bool Contains(SemVersion version)
    {
        DebugChecks.IsNotNull(version, nameof(version));
        var comparison = SemVersion.ComparePrecedence(Version, version);
        return Inclusive ? comparison <= 0 : comparison < 0;
    }

    public LeftBoundedRange Min(LeftBoundedRange other)
        => CompareTo(other) <= 0 ? this : other;

    public LeftBoundedRange Max(LeftBoundedRange other)
        => CompareTo(other) >= 0 ? this : other;

    #region Equality
    public bool Equals(LeftBoundedRange other)
        => Equals(Version, other.Version) && Inclusive == other.Inclusive;

    public override bool Equals(object? obj)
        => obj is LeftBoundedRange other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Version, Inclusive);

    public static bool operator ==(LeftBoundedRange left, LeftBoundedRange right)
        => left.Equals(right);

    public static bool operator !=(LeftBoundedRange left, LeftBoundedRange right)
        => !left.Equals(right);
    #endregion

    public int CompareTo(RightBoundedRange other)
    {
        if(other.Version is null) return -1;
        var comparison = PrecedenceComparer.Instance.Compare(Version!, other.Version);
        if (comparison != 0) return comparison;
        return Inclusive && other.Inclusive ? 0 : 1;
    }

    public int CompareTo(LeftBoundedRange other)
    {
        var comparison = PrecedenceComparer.Instance.Compare(Version!, other.Version!);
        if (comparison != 0) return comparison;
        return -Inclusive.CompareTo(other.Inclusive);
    }

    public override string ToString() => (Inclusive ? ">=" : ">") + (Version?.ToString() ?? "null");
}
