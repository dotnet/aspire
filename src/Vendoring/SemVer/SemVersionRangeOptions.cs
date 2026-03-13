using System;

namespace Semver;

/// <summary>
/// <para>Determines the parsing options and allowed styles of range and version strings passed
/// to the <see cref="SemVersionRange.Parse(string,SemVersionRangeOptions,int)"/> and
/// <see cref="SemVersionRange.TryParse(string,SemVersionRangeOptions,out SemVersionRange,int)"/>
/// methods. These styles only affect which strings are accepted when parsing. The
/// constructed ranges and version numbers are valid semantic version ranges without any of the
/// optional features in the original string.</para>
///
/// <para>Most options only allow additional version styles. However, the
/// <see cref="IncludeAllPrerelease"/> option modifies how version ranges are interpreted. With
/// this option, all prerelease versions within the range bounds will be considered to be in the
/// range. Without this option, only prerelease versions where one comparison with the same
/// major, minor, and patch versions is prerelease will satisfy the range. For more information,
/// see the <a href="https://semver-nuget.org/ranges/">range expressions documentation</a>.</para>
///
/// <para>This enumeration supports a bitwise combination of its member values (e.g.
/// <c>SemVersionRangeOptions.OptionalPatch | SemVersionRangeOptions.AllowV</c>).</para>
/// </summary>
[Flags]
public enum SemVersionRangeOptions
{
    #region Matching SemVersionStyles
    /// <summary>
    /// Accept versions strictly conforming to the SemVer 2.0 spec without metadata.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// <para>Allow leading zeros on major, minor, patch, and prerelease version numbers.</para>
    ///
    /// <para>Leading zeros will be removed from the constructed version number.</para>
    /// </summary>
    AllowLeadingZeros = 1,

    /// <summary>
    /// Allow a leading lowercase "v" on versions.
    /// </summary>
    AllowLowerV = 1 << 3,

    /// <summary>
    /// Allow a leading uppercase "V" on versions.
    /// </summary>
    AllowUpperV = 1 << 4,

    /// <summary>
    /// Allow a leading "v" or "V" on versions.
    /// </summary>
    AllowV = AllowLowerV | AllowUpperV,

    /// <summary>
    /// Patch version number is optional on versions.
    /// </summary>
    OptionalPatch = 1 << 5,

    /// <summary>
    /// Minor and patch version numbers are optional on versions.
    /// </summary>
    OptionalMinorPatch = 1 << 6 | OptionalPatch,
    #endregion

    #region Using values of SemVersionStyles that do not apply to ranges
    /// <summary>
    /// Include all prerelease versions in the range rather than just prerelease versions
    /// matching a prerelease identifier in the range.
    /// </summary>
    IncludeAllPrerelease = 1 << 1,

    /// <summary>
    /// Allow version numbers with build metadata in version range expressions. The metadata
    /// will be removed/ignored for the definition of the version range.
    /// </summary>
    AllowMetadata = 1 << 2,
    #endregion

    /// <summary>
    /// <para>Accept any version string format supported.</para>
    ///
    /// <para>The formats accepted by this style will change if/when more formats are supported.</para>
    /// </summary>
    Loose = AllowLeadingZeros | AllowV | OptionalMinorPatch | AllowMetadata,
}
