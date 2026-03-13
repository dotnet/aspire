using System;

namespace Semver;

/// <summary>
/// <para>Determines the styles that are allowed in version strings passed to the
/// <see cref="SemVersion.Parse(string,SemVersionStyles,int)"/> and
/// <see cref="SemVersion.TryParse(string,SemVersionStyles,out SemVersion,int)"/>
/// methods. These styles only affect which strings are accepted when parsing. The
/// constructed version numbers are valid semantic versions without any of the
/// optional features in the original string.</para>
///
/// <para>This enumeration supports a bitwise combination of its member values (e.g.
/// <c>SemVersionStyles.AllowWhitespace | SemVersionStyles.AllowV</c>).</para>
/// </summary>
[Flags]
public enum SemVersionStyles
{
    /// <summary>
    /// Accept version strings strictly conforming to the SemVer 2.0 spec.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// <para>Allow leading zeros on major, minor, patch, and prerelease version numbers.</para>
    ///
    /// <para>Leading zeros will be removed from the constructed version number.</para>
    /// </summary>
    AllowLeadingZeros = 1,

    /// <summary>
    /// Allow leading whitespace. When combined with leading "v", the whitespace
    /// must come before the "v".
    /// </summary>
    AllowLeadingWhitespace = 1 << 1,

    /// <summary>
    /// Allow trailing whitespace.
    /// </summary>
    AllowTrailingWhitespace = 1 << 2,

    /// <summary>
    /// Allow leading and/or trailing whitespace. When combined with leading "v",
    /// the leading whitespace must come before the "v".
    /// </summary>
    AllowWhitespace = AllowLeadingWhitespace | AllowTrailingWhitespace,

    /// <summary>
    /// Allow a leading lowercase "v".
    /// </summary>
    AllowLowerV = 1 << 3,

    /// <summary>
    /// Allow a leading uppercase "V".
    /// </summary>
    AllowUpperV = 1 << 4,

    /// <summary>
    /// Allow a leading "v" or "V".
    /// </summary>
    AllowV = AllowLowerV | AllowUpperV,

    /// <summary>
    /// Patch version number is optional.
    /// </summary>
    OptionalPatch = 1 << 5,

    /// <summary>
    /// Minor and patch version numbers are optional.
    /// </summary>
    OptionalMinorPatch = 1 << 6 | OptionalPatch,

    /// <summary>
    /// <para>Accept any version string format supported.</para>
    ///
    /// <para>The formats accepted by this style will change if/when more formats are supported.</para>
    /// </summary>
    Any = unchecked((int)0xFFFF_FFFF),
}
