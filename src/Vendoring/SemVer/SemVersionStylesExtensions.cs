using System;
using System.Runtime.CompilerServices;
using static Semver.SemVersionStyles;

namespace Semver;

internal static class SemVersionStylesExtensions
{
    internal const SemVersionStyles AllowAll = AllowLeadingZeros
                                             | AllowLeadingWhitespace
                                             | AllowTrailingWhitespace
                                             | AllowWhitespace
                                             | AllowLowerV
                                             | AllowUpperV
                                             | AllowV
                                             | OptionalPatch
                                             | OptionalMinorPatch;
    private const SemVersionStyles OptionalMinorWithoutPatch = OptionalMinorPatch & ~OptionalPatch;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this SemVersionStyles styles)
    {
        // Either it is the Any style
        if (styles == Any) return true;

        // Or it is some combination of the flags
        return (styles & AllowAll) == styles
            // Except for a flag for optional minor without optional patch
            && (styles & OptionalMinorPatch) != OptionalMinorWithoutPatch;
    }

    /// <summary>
    /// The <see cref="Enum.HasFlag"/> method is surprisingly slow. This provides
    /// a fast alternative for the <see cref="SemVersionStyles"/> enum.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasStyle(this SemVersionStyles styles, SemVersionStyles flag)
        => (styles & flag) == flag;
}
