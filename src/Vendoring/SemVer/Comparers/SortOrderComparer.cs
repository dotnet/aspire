using System;
using Semver.Utility;

namespace Semver.Comparers;

internal sealed class SortOrderComparer : SemVersionComparer
{
    #region Singleton
    public static readonly ISemVersionComparer Instance = new SortOrderComparer();

    private SortOrderComparer() { }
    #endregion

    public override bool Equals(SemVersion? x, SemVersion? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Major == y.Major && x.Minor == y.Minor && x.Patch == y.Patch
               && x.Prerelease == y.Prerelease
               && x.Metadata == y.Metadata;
    }

    public override int GetHashCode(SemVersion? v)
        => v is null ? 0 : HashCode.Combine(v.Major, v.Minor, v.Patch, v.Prerelease, v.Metadata);

    public override int Compare(SemVersion? x, SemVersion? y)
    {
        if (ReferenceEquals(x, y)) return 0; // covers both null case

        var comparison = PrecedenceComparer.Instance.Compare(x!, y!);
        if (comparison != 0) return comparison;

        DebugChecks.IsNotNull(x, nameof(x));
        DebugChecks.IsNotNull(y, nameof(y));

        var xMetadataIdentifiers = x.MetadataIdentifiers;
        var yMetadataIdentifiers = y.MetadataIdentifiers;
        var minLength = Math.Min(xMetadataIdentifiers.Count, yMetadataIdentifiers.Count);
        for (int i = 0; i < minLength; i++)
        {
            comparison = xMetadataIdentifiers[i].CompareTo(yMetadataIdentifiers[i]);
            if (comparison != 0) return comparison;
        }

        return xMetadataIdentifiers.Count.CompareTo(yMetadataIdentifiers.Count);
    }
}
