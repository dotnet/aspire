using System;

namespace Semver.Comparers;

internal sealed class PrecedenceComparer : SemVersionComparer
{
    #region Singleton
    public static readonly ISemVersionComparer Instance = new PrecedenceComparer();

    private PrecedenceComparer() { }
    #endregion

    public override bool Equals(SemVersion? x, SemVersion? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Major == y.Major && x.Minor == y.Minor && x.Patch == y.Patch
               && x.Prerelease == y.Prerelease;
    }

    public override int GetHashCode(SemVersion? v)
        => v is null ? 0 : HashCode.Combine(v.Major, v.Minor, v.Patch, v.Prerelease);

    public override int Compare(SemVersion? x, SemVersion? y)
    {
        if (ReferenceEquals(x, y)) return 0; // covers both null case
        if (x is null) return -1;
        if (y is null) return 1;

        var comparison = x.Major.CompareTo(y.Major);
        if (comparison != 0) return comparison;

        comparison = x.Minor.CompareTo(y.Minor);
        if (comparison != 0) return comparison;

        comparison = x.Patch.CompareTo(y.Patch);
        if (comparison != 0) return comparison;

        // Release are higher precedence than prerelease
        var xIsRelease = x.IsRelease;
        var yIsRelease = y.IsRelease;
        if (xIsRelease && yIsRelease) return 0;
        if (xIsRelease) return 1;
        if (yIsRelease) return -1;

        var xPrereleaseIdentifiers = x.PrereleaseIdentifiers;
        var yPrereleaseIdentifiers = y.PrereleaseIdentifiers;

        var minLength = Math.Min(xPrereleaseIdentifiers.Count, yPrereleaseIdentifiers.Count);
        for (int i = 0; i < minLength; i++)
        {
            comparison = xPrereleaseIdentifiers[i].CompareTo(yPrereleaseIdentifiers[i]);
            if (comparison != 0) return comparison;
        }

        return xPrereleaseIdentifiers.Count.CompareTo(yPrereleaseIdentifiers.Count);
    }
}
