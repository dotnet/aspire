namespace Semver.Utility;

/// <summary>
/// Struct used as a marker to differentiate constructor overloads that would
/// otherwise be the same as safe overloads.
/// </summary>
internal readonly struct UnsafeOverload
{
    public static readonly UnsafeOverload Marker = default;
}
