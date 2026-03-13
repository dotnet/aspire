using System;
using System.Runtime.CompilerServices;

namespace Semver.Parsing;

internal static class WildcardVersionExtensions
{
    /// <summary>
    /// The <see cref="Enum.HasFlag"/> method is surprisingly slow. This provides
    /// a fast alternative for the <see cref="WildcardVersion"/> enum.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasOption(this WildcardVersion wildcards, WildcardVersion flag)
        => (wildcards & flag) == flag;

    /// <summary>
    /// Remove a flag from a <see cref="WildcardVersion"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveOption(this ref WildcardVersion wildcards, WildcardVersion flag)
        => wildcards &= ~flag;
}
