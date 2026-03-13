using System.Runtime.CompilerServices;

namespace Semver.Utility;

internal static class CharExtensions
{
    /// <summary>
    /// Is this character an ASCII digit '0' through '9'
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigit(this char c) => c is >= '0' and <= '9';

    /// <summary>
    /// Is this character and ASCII alphabetic character or hyphen [A-Za-z-]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAlphaOrHyphen(this char c)
        => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '-';
}
