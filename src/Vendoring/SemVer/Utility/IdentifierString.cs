using System;
using System.Runtime.CompilerServices;

namespace Semver.Utility;

/// <summary>
/// Methods for working with the strings that make up identifiers
/// </summary>
internal static class IdentifierString
{
    /// <summary>
    /// Compare two strings as they should be compared as identifiers.
    /// </summary>
    /// <remarks>This enforces ordinal comparison. It also fixes a technically
    /// correct but odd thing where the comparison result can be a number
    /// other than -1, 0, or 1.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(string left, string right)
        => Math.Sign(string.CompareOrdinal(left, right));
}
