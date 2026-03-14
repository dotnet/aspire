using System;
using System.Collections;

namespace Semver.Comparers;

internal abstract class SemVersionComparer : ISemVersionComparer
{
    bool IEqualityComparer.Equals(object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;
        if (x is SemVersion v1 && y is SemVersion v2) return Equals(v1, v2);
        throw new ArgumentException(InvalidTypeMessage);
    }

    public abstract bool Equals(SemVersion? x, SemVersion? y);

    int IEqualityComparer.GetHashCode(object? obj)
    {
        if (obj is null) return 0;
        if (obj is SemVersion v) return GetHashCode(v);
        throw new ArgumentException(InvalidTypeMessage);
    }

    public abstract int GetHashCode(SemVersion? v);

    int IComparer.Compare(object? x, object? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;
        if (x is SemVersion v1 && y is SemVersion v2) return Compare(v1, v2);
        throw new ArgumentException(InvalidTypeMessage);
    }

    public abstract int Compare(SemVersion? x, SemVersion? y);

    private const string InvalidTypeMessage = $"Type of argument is not {nameof(SemVersion)}.";
}
