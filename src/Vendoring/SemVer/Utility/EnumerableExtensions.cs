using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Semver.Utility;

internal static class EnumerableExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> values)
        => values.ToList().AsReadOnly();
}
