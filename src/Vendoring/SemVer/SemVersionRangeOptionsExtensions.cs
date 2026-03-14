using System;
using System.Runtime.CompilerServices;
using static Semver.SemVersionRangeOptions;

namespace Semver;

internal static class SemVersionRangeOptionsExtensions
{
    private const SemVersionRangeOptions OptionsThatAreStyles
        = AllowLeadingZeros | AllowV | OptionalMinorPatch;
    private const SemVersionRangeOptions OptionalMinorWithoutPatch
        = OptionalMinorPatch & ~OptionalPatch;

    internal const SemVersionRangeOptions AllFlags
        = AllowLeadingZeros | AllowV | OptionalMinorPatch | IncludeAllPrerelease | AllowMetadata;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this SemVersionRangeOptions options)
        // It is some combination of the flags
        => (options & AllFlags) == options
           // Except for a flag for optional minor without optional patch
           && (options & OptionalMinorPatch) != OptionalMinorWithoutPatch;

    /// <summary>
    /// The <see cref="Enum.HasFlag"/> method is surprisingly slow. This provides
    /// a fast alternative for the <see cref="SemVersionRangeOptions"/> enum.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasOption(this SemVersionRangeOptions options, SemVersionRangeOptions flag)
        => (options & flag) == flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SemVersionStyles ToStyles(this SemVersionRangeOptions options)
        => (SemVersionStyles)(options & OptionsThatAreStyles);
}
