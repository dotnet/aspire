using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using Semver.Comparers;
using Semver.Parsing;
using Semver.Utility;

namespace Semver;

/// <summary>
/// A semantic version number. Conforms with v2.0.0 of semantic versioning
/// (<a href="https://semver.org">semver.org</a>).
/// </summary>
[Serializable]
internal sealed class SemVersion : IEquatable<SemVersion>, ISerializable
{
    internal static readonly SemVersion Min = new SemVersion(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, new[] { PrereleaseIdentifier.Zero });
    internal static readonly SemVersion MinRelease = new SemVersion(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero);

    internal const string InvalidSemVersionStylesMessage = "An invalid SemVersionStyles value was used.";
    private const string InvalidMajorVersionMessage = "Major version must be greater than or equal to zero.";
    private const string InvalidMinorVersionMessage = "Minor version must be greater than or equal to zero.";
    private const string InvalidPatchVersionMessage = "Patch version must be greater than or equal to zero.";
    private const string PrereleaseIdentifierIsDefaultMessage = "Prerelease identifier cannot be default/null.";
    private const string MetadataIdentifierIsDefaultMessage = "Metadata identifier cannot be default/null.";
    private const string InvalidMaxLengthMessage = "Must not be negative.";
    private const string MajorMinorOrPatchVersionToLargeToConvertMessage =
        "Version with {0} version of {1} can't be converted to System.Version because it is greater than Int32.MaxValue.";
    internal const int MaxVersionLength = 1024;

    /// <summary>
    /// Deserialize a <see cref="SemVersion"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
    private SemVersion(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        var semVersion = Parse(info.GetString("SemVersion")!, SemVersionStyles.Strict);
        Major = semVersion.Major;
        Minor = semVersion.Minor;
        Patch = semVersion.Patch;
        Prerelease = semVersion.Prerelease;
        PrereleaseIdentifiers = semVersion.PrereleaseIdentifiers;
        Metadata = semVersion.Metadata;
        MetadataIdentifiers = semVersion.MetadataIdentifiers;
    }

    /// <summary>
    /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
    /// <param name="context">The destination (see <see cref="SerializationInfo"/>) for this serialization.</param>
#if !NET5_0_OR_GREATER
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        info.AddValue("SemVersion", ToString());
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="SemVersion" /> class.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="major"/> version
    /// number is negative.</exception>
    // Constructor needed to resolve ambiguity between other overloads with default parameters.
    public SemVersion(BigInteger major)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        Major = major;
        Minor = BigInteger.Zero;
        Patch = BigInteger.Zero;
        Prerelease = "";
        PrereleaseIdentifiers = ReadOnlyList<PrereleaseIdentifier>.Empty;
        Metadata = "";
        MetadataIdentifiers = ReadOnlyList<MetadataIdentifier>.Empty;
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="SemVersion" /> class.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="major"/> or
    /// <paramref name="minor"/> version number is negative.</exception>
    // Constructor needed to resolve ambiguity between other overloads with default parameters.
    public SemVersion(BigInteger major, BigInteger minor)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), InvalidMinorVersionMessage);
        Major = major;
        Minor = minor;
        Patch = BigInteger.Zero;
        Prerelease = "";
        PrereleaseIdentifiers = ReadOnlyList<PrereleaseIdentifier>.Empty;
        Metadata = "";
        MetadataIdentifiers = ReadOnlyList<MetadataIdentifier>.Empty;
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="SemVersion" /> class.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="patch">The patch version number.</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="major"/>,
    /// <paramref name="minor"/>, or <paramref name="patch"/> version number is negative.</exception>
    // Constructor needed to resolve ambiguity between other overloads with default parameters.
    public SemVersion(BigInteger major, BigInteger minor, BigInteger patch)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), InvalidMinorVersionMessage);
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch), InvalidPatchVersionMessage);
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = "";
        PrereleaseIdentifiers = ReadOnlyList<PrereleaseIdentifier>.Empty;
        Metadata = "";
        MetadataIdentifiers = ReadOnlyList<MetadataIdentifier>.Empty;
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="SemVersion" /> class.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="patch">The patch version number.</param>
    /// <param name="prerelease">The prerelease identifiers.</param>
    /// <param name="metadata">The build metadata identifiers.</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="major"/>,
    /// <paramref name="minor"/>, or <paramref name="patch"/> version number is negative.</exception>
    /// <exception cref="ArgumentException">A prerelease or metadata identifier has the default value.</exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public SemVersion(BigInteger major, BigInteger minor = default, BigInteger patch = default,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        IEnumerable<PrereleaseIdentifier>? prerelease = null,
        IEnumerable<MetadataIdentifier>? metadata = null)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), InvalidMinorVersionMessage);
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch), InvalidPatchVersionMessage);
        IReadOnlyList<PrereleaseIdentifier>? prereleaseIdentifiers;
        if (prerelease is null)
            prereleaseIdentifiers = null;
        else
        {
            prereleaseIdentifiers = prerelease.ToReadOnlyList();
            if (prereleaseIdentifiers.Any(i => i == default))
                throw new ArgumentException(PrereleaseIdentifierIsDefaultMessage, nameof(prerelease));
        }

        IReadOnlyList<MetadataIdentifier>? metadataIdentifiers;
        if (metadata is null)
            metadataIdentifiers = null;
        else
        {
            metadataIdentifiers = metadata.ToReadOnlyList();
            if (metadataIdentifiers.Any(i => i == default))
                throw new ArgumentException(MetadataIdentifierIsDefaultMessage, nameof(metadata));
        }

        Major = major;
        Minor = minor;
        Patch = patch;

        if (prereleaseIdentifiers is null || prereleaseIdentifiers.Count == 0)
        {
            Prerelease = "";
            PrereleaseIdentifiers = ReadOnlyList<PrereleaseIdentifier>.Empty;
        }
        else
        {
            Prerelease = string.Join(".", prereleaseIdentifiers);
            PrereleaseIdentifiers = prereleaseIdentifiers;
        }

        if (metadataIdentifiers is null || metadataIdentifiers.Count == 0)
        {
            Metadata = "";
            MetadataIdentifiers = ReadOnlyList<MetadataIdentifier>.Empty;
        }
        else
        {
            Metadata = string.Join(".", metadataIdentifiers);
            MetadataIdentifiers = metadataIdentifiers;
        }
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="SemVersion" /> class.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="patch">The patch version number.</param>
    /// <param name="prerelease">The prerelease identifiers.</param>
    /// <param name="metadata">The build metadata identifiers.</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="major"/>,
    /// <paramref name="minor"/>, or <paramref name="patch"/> version number is negative.</exception>
    /// <exception cref="ArgumentNullException">One of the prerelease or metadata identifiers is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A prerelease identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens) or has leading
    /// zeros for a numeric identifier. Or, a metadata identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens).</exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public SemVersion(BigInteger major, BigInteger minor = default, BigInteger patch = default,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        IEnumerable<string>? prerelease = null,
        IEnumerable<string>? metadata = null)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), InvalidMinorVersionMessage);
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch), InvalidPatchVersionMessage);
        var prereleaseIdentifiers = prerelease?
                                    .Select(i => new PrereleaseIdentifier(i, allowLeadingZeros: false, nameof(prerelease)))
                                    .ToReadOnlyList();

        var metadataIdentifiers = metadata?
                                  .Select(i => new MetadataIdentifier(i, nameof(metadata)))
                                  .ToReadOnlyList();

        Major = major;
        Minor = minor;
        Patch = patch;

        if (prereleaseIdentifiers is null || prereleaseIdentifiers.Count == 0)
        {
            Prerelease = "";
            PrereleaseIdentifiers = ReadOnlyList<PrereleaseIdentifier>.Empty;
        }
        else
        {
            Prerelease = string.Join(".", prereleaseIdentifiers);
            PrereleaseIdentifiers = prereleaseIdentifiers;
        }

        if (metadataIdentifiers is null || metadataIdentifiers.Count == 0)
        {
            Metadata = "";
            MetadataIdentifiers = ReadOnlyList<MetadataIdentifier>.Empty;
        }
        else
        {
            Metadata = string.Join(".", metadataIdentifiers);
            MetadataIdentifiers = metadataIdentifiers;
        }
    }

    /// <summary>
    /// Create a new instance of the <see cref="SemVersion" /> class. Parses prerelease
    /// and metadata identifiers from dot separated strings. If parsing is not needed, use a
    /// constructor instead.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="patch">The patch version number.</param>
    /// <param name="prerelease">The prerelease portion (e.g. "alpha.5").</param>
    /// <param name="metadata">The build metadata (e.g. "nightly.232").</param>
    /// <param name="allowLeadingZeros">Allow leading zeros in numeric prerelease identifiers. Leading
    /// zeros will be removed.</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="major"/>,
    /// <paramref name="minor"/>, or <paramref name="patch"/> version number is negative.</exception>
    /// <exception cref="ArgumentException">A prerelease identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens) or has leading
    /// zeros for a numeric identifier when <paramref name="allowLeadingZeros"/> is
    /// <see langword="false"/>. Or, a metadata identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens).</exception>
    public static SemVersion ParsedFrom(BigInteger major, BigInteger? minor = null, BigInteger? patch = null,
        string prerelease = "", string metadata = "", bool allowLeadingZeros = false)
    {
        var internalMajor = major; //for uniformity
        var internalMinor = minor ?? BigInteger.Zero;
        var internalPatch = patch ?? BigInteger.Zero;

        if (internalMajor < 0) throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        if (internalMinor < 0) throw new ArgumentOutOfRangeException(nameof(minor), InvalidMinorVersionMessage);
        if (internalPatch < 0) throw new ArgumentOutOfRangeException(nameof(patch), InvalidPatchVersionMessage);

        if (prerelease is null) throw new ArgumentNullException(nameof(prerelease));
        var prereleaseIdentifiers = prerelease.Length == 0
            ? ReadOnlyList<PrereleaseIdentifier>.Empty
            : prerelease.SplitAndMapToReadOnlyList('.',
                i => new PrereleaseIdentifier(i, allowLeadingZeros, nameof(prerelease)));
        if (allowLeadingZeros)
            // Leading zeros may have been removed, need to reconstruct the prerelease string
            prerelease = string.Join(".", prereleaseIdentifiers);

        if (metadata is null) throw new ArgumentNullException(nameof(metadata));
        var metadataIdentifiers = metadata.Length == 0
            ? ReadOnlyList<MetadataIdentifier>.Empty
            : metadata.SplitAndMapToReadOnlyList('.', i => new MetadataIdentifier(i, nameof(metadata)));

        return new SemVersion(internalMajor, internalMinor, internalPatch,
            prerelease, prereleaseIdentifiers, metadata, metadataIdentifiers);
    }

    /// <summary>
    /// Construct a <see cref="SemVersion"/> from its proper parts.
    /// </summary>
    /// <remarks>Parameter validation is not performed. The <paramref name="major"/>,
    /// <paramref name="minor"/>, and <paramref name="patch"/> version numbers must not be
    /// negative. The <paramref name="prereleaseIdentifiers"/> and
    /// <paramref name="metadataIdentifiers"/> must not be <see langword="null"/> or
    /// contain invalid values and must be immutable. The <paramref name="prerelease"/>
    /// and <paramref name="metadata"/> must not be null and must be equal to the
    /// corresponding identifiers.</remarks>
    internal SemVersion(BigInteger major, BigInteger minor, BigInteger patch,
        string prerelease, IReadOnlyList<PrereleaseIdentifier> prereleaseIdentifiers,
        string metadata, IReadOnlyList<MetadataIdentifier> metadataIdentifiers)
    {
        DebugChecks.IsValidVersionNumber(major, "Major", nameof(major));
        DebugChecks.IsValidVersionNumber(minor, "Minor", nameof(minor));
        DebugChecks.IsValidVersionNumber(patch, "Patch", nameof(patch));
        DebugChecks.IsNotNull(prerelease, nameof(prerelease));
        DebugChecks.IsNotNull(prerelease, nameof(prereleaseIdentifiers));
        DebugChecks.ContainsNoDefaultValues(prereleaseIdentifiers, "Prerelease", nameof(prereleaseIdentifiers));
        DebugChecks.AreEqualWhenJoinedWithDots(prerelease, nameof(prerelease),
            prereleaseIdentifiers, nameof(prereleaseIdentifiers));
        DebugChecks.IsNotNull(metadata, nameof(metadata));
        DebugChecks.IsNotNull(metadataIdentifiers, nameof(metadataIdentifiers));
        DebugChecks.ContainsNoDefaultValues(metadataIdentifiers, "Metadata", nameof(metadataIdentifiers));
        DebugChecks.AreEqualWhenJoinedWithDots(metadata, nameof(metadata),
            metadataIdentifiers, nameof(metadataIdentifiers));

        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        PrereleaseIdentifiers = prereleaseIdentifiers;
        Metadata = metadata;
        MetadataIdentifiers = metadataIdentifiers;
    }

    #region System.Version
    /// <summary>
    /// Converts a <see cref="Version"/> into the equivalent semantic version.
    /// </summary>
    /// <param name="version">The version to be converted to a semantic version.</param>
    /// <returns>The equivalent semantic version.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="version"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="version"/> has a revision number greater than zero.</exception>
    /// <remarks>
    /// <see cref="Version"/> numbers have the form <em>major</em>.<em>minor</em>[.<em>build</em>[.<em>revision</em>]]
    /// where square brackets ('[' and ']')  indicate optional components. The first three parts
    /// are converted to the major, minor, and patch version numbers of a semantic version. If the
    /// build component is not defined (-1), the patch number is assumed to be zero.
    /// <see cref="Version"/> numbers with a revision greater than zero cannot be converted to
    /// semantic versions. An <see cref="ArgumentException"/> is thrown when this method is called
    /// with such a <see cref="Version"/>.
    /// </remarks>
    public static SemVersion FromVersion(Version version)
    {
        if (version is null) throw new ArgumentNullException(nameof(version));
        if (version.Revision > 0) throw new ArgumentException("Version with Revision number can't be converted to SemVer.", nameof(version));
        var patch = version.Build > 0 ? version.Build : 0;
        return new SemVersion(version.Major, version.Minor, patch);
    }

    /// <summary>
    /// Converts this semantic version to a <see cref="Version"/>.
    /// </summary>
    /// <returns>The equivalent <see cref="Version"/>.</returns>
    /// <exception cref="InvalidOperationException">The semantic version is a prerelease version
    /// or has build metadata or has a major, minor, or patch version number greater than
    /// <see cref="int.MaxValue"/>.</exception>
    /// <remarks>
    /// A semantic version of the form <em>major</em>.<em>minor</em>.<em>patch</em>
    /// is converted to a <see cref="Version"/> of the form
    /// <em>major</em>.<em>minor</em>.<em>build</em> where the build number is the
    /// patch version of the semantic version. Prerelease versions and build metadata
    /// are not representable in a <see cref="Version"/>. This method throws
    /// an <see cref="InvalidOperationException"/> if the semantic version is a
    /// prerelease version or has build metadata.
    /// </remarks>
    public Version ToVersion()
    {
        if (IsPrerelease) throw new InvalidOperationException("Prerelease version can't be converted to System.Version.");
        if (Metadata.Length != 0) throw new InvalidOperationException("Version with build metadata can't be converted to System.Version.");
        if (Major > int.MaxValue)
            throw new InvalidOperationException(string.Format(MajorMinorOrPatchVersionToLargeToConvertMessage, "major", Major));
        if (Minor > int.MaxValue)
            throw new InvalidOperationException(string.Format(MajorMinorOrPatchVersionToLargeToConvertMessage, "minor", Minor));
        if (Patch > int.MaxValue)
            throw new InvalidOperationException(string.Format(MajorMinorOrPatchVersionToLargeToConvertMessage, "patch", Patch));

        return new Version((int)Major, (int)Minor, (int)Patch);
    }
    #endregion

    /// <summary>
    /// Converts the string representation of a semantic version to its <see cref="SemVersion"/> equivalent.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <param name="style">A bitwise combination of enumeration values that indicates the style
    /// elements that can be present in <paramref name="version"/>. The preferred value to use
    /// is <see cref="SemVersionStyles.Strict"/>.</param>
    /// <param name="maxLength">The maximum length of <paramref name="version"/> that should be
    /// parsed. This prevents attacks using very long version strings.</param>
    /// <exception cref="ArgumentException"><paramref name="style"/> is not a valid
    /// <see cref="SemVersionStyles"/> value.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="version"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The <paramref name="version"/> is invalid or not in a
    /// format compliant with <paramref name="style"/>.</exception>
    public static SemVersion Parse(string version, SemVersionStyles style, int maxLength = MaxVersionLength)
    {
        if (!style.IsValid()) throw new ArgumentException(InvalidSemVersionStylesMessage, nameof(style));
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength), InvalidMaxLengthMessage);
        var ex = SemVersionParser.Parse(version, style, null, maxLength, out var semver);

        if (ex is not null)
            throw ex;
        DebugChecks.IsNotNull(semver, nameof(semver));

        return semver;
    }

    public static SemVersion Parse(string version, int maxLength = MaxVersionLength)
        => Parse(version, SemVersionStyles.Strict, maxLength);

    /// <summary>
    /// Converts the string representation of a semantic version to its <see cref="SemVersion"/>
    /// equivalent. The return value indicates whether the conversion succeeded.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <param name="style">A bitwise combination of enumeration values that indicates the style
    /// elements that can be present in <paramref name="version"/>. The preferred value to use
    /// is <see cref="SemVersionStyles.Strict"/>.</param>
    /// <param name="semver">When this method returns, contains a <see cref="SemVersion"/> instance equivalent
    /// to the version string passed in, if the version string was valid, or <see langword="null"/> if the
    /// version string was invalid.</param>
    /// <param name="maxLength">The maximum length of <paramref name="version"/> that should be
    /// parsed. This prevents attacks using very long version strings.</param>
    /// <returns><see langword="false"/> when an invalid version string is passed, otherwise <see langword="true"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="style"/> is not a valid
    /// <see cref="SemVersionStyles"/> value.</exception>
    public static bool TryParse(string? version, SemVersionStyles style,
        [NotNullWhen(true)] out SemVersion? semver, int maxLength = MaxVersionLength)
    {
        if (!style.IsValid()) throw new ArgumentException(InvalidSemVersionStylesMessage, nameof(style));
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength), InvalidMaxLengthMessage);
        var exception = SemVersionParser.Parse(version, style, VersionParsing.FailedException, maxLength, out semver);

        DebugChecks.IsNotFailedException(exception, nameof(SemVersionParser), nameof(SemVersionParser.Parse));

        return exception is null;
    }

    public static bool TryParse(string? version, [NotNullWhen(true)] out SemVersion? semver, int maxLength = MaxVersionLength)
        => TryParse(version, SemVersionStyles.Strict, out semver, maxLength);

    /// <summary>
    /// Creates a copy of the current instance with multiple changed properties. If changing only
    /// one property use one of the more specific <c>WithX()</c> methods.
    /// </summary>
    /// <param name="major">The value to replace the major version number or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="minor">The value to replace the minor version number or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="patch">The value to replace the patch version number or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="prerelease">The value to replace the prerelease identifiers or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="metadata">The value to replace the build metadata identifiers or <see langword="null"/> to leave it unchanged.</param>
    /// <returns>The new version with changed properties.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="major"/>,
    /// <paramref name="minor"/>, or <paramref name="patch"/> version number is negative.</exception>
    /// <exception cref="ArgumentException">A prerelease or metadata identifier has the default value.</exception>
    /// <remarks>
    /// The <see cref="With"/> method is intended to be called using named argument syntax, passing only
    /// those fields to be changed.
    /// </remarks>
    /// <example>
    /// To change the minor and patch versions:
    /// <code>var modifiedVersion = version.With(minor: 2, patch: 4);</code>
    /// </example>
    public SemVersion With(
        BigInteger? major = null,
        BigInteger? minor = null,
        BigInteger? patch = null,
        IEnumerable<PrereleaseIdentifier>? prerelease = null,
        IEnumerable<MetadataIdentifier>? metadata = null)
    {
        if (major < 0)
            throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        if (minor < 0)
            throw new ArgumentOutOfRangeException(nameof(minor), InvalidMinorVersionMessage);
        if (patch < 0)
            throw new ArgumentOutOfRangeException(nameof(patch), InvalidPatchVersionMessage);

        IReadOnlyList<PrereleaseIdentifier>? prereleaseIdentifiers = null;
        string? prereleaseString = null;
        if (prerelease != null)
        {
            prereleaseIdentifiers = prerelease.ToReadOnlyList();
            if (prereleaseIdentifiers.Count == 0)
            {
                prereleaseIdentifiers = ReadOnlyList<PrereleaseIdentifier>.Empty;
                prereleaseString = "";
            }
            else if (prereleaseIdentifiers.Any(i => i == default))
                throw new ArgumentException(PrereleaseIdentifierIsDefaultMessage, nameof(prerelease));
            else
                prereleaseString = string.Join(".", prereleaseIdentifiers);
        }

        IReadOnlyList<MetadataIdentifier>? metadataIdentifiers = null;
        string? metadataString = null;
        if (metadata != null)
        {
            metadataIdentifiers = metadata.ToReadOnlyList();
            if (metadataIdentifiers.Count == 0)
            {
                metadataIdentifiers = ReadOnlyList<MetadataIdentifier>.Empty;
                metadataString = "";
            }
            else if (metadataIdentifiers.Any(i => i == default))
                throw new ArgumentException(MetadataIdentifierIsDefaultMessage, nameof(metadata));
            else
                metadataString = string.Join(".", metadataIdentifiers);
        }

        return new SemVersion(
            major ?? Major,
            minor ?? Minor,
            patch ?? Patch,
            prereleaseString ?? Prerelease,
            prereleaseIdentifiers ?? PrereleaseIdentifiers,
            metadataString ?? Metadata,
            metadataIdentifiers ?? MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with multiple changed properties. Parses prerelease
    /// and metadata identifiers from dot separated strings. Use <see cref="With"/> instead if
    /// parsing is not needed. If changing only one property use one of the more specific
    /// <c>WithX()</c> methods.
    /// </summary>
    /// <param name="major">The value to replace the major version number or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="minor">The value to replace the minor version number or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="patch">The value to replace the patch version number or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="prerelease">The value to replace the prerelease identifiers or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="metadata">The value to replace the build metadata identifiers or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="allowLeadingZeros">Allow leading zeros in numeric prerelease identifiers. Leading
    /// zeros will be removed.</param>
    /// <returns>The new version with changed properties.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="major"/>,
    /// <paramref name="minor"/>, or <paramref name="patch"/> version number is negative.</exception>
    /// <exception cref="ArgumentException">A prerelease identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens) or has leading
    /// zeros for a numeric identifier when <paramref name="allowLeadingZeros"/> is
    /// <see langword="false"/>. Or, a metadata identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens).</exception>
    /// <remarks>
    /// The <see cref="WithParsedFrom"/> method is intended to be called using named argument
    /// syntax, passing only those fields to be changed.
    /// </remarks>
    /// <example>
    /// To change the patch version and prerelease identifiers version:
    /// <code>var modifiedVersion = version.WithParsedFrom(patch: 4, prerelease: "alpha.5");</code>
    /// </example>
    public SemVersion WithParsedFrom(
        BigInteger? major = null,
        BigInteger? minor = null,
        BigInteger? patch = null,
        string? prerelease = null,
        string? metadata = null,
        bool allowLeadingZeros = false)
    {
        if (major < 0)
            throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        if (minor < 0)
            throw new ArgumentOutOfRangeException(nameof(minor), InvalidMinorVersionMessage);
        if (patch < 0)
            throw new ArgumentOutOfRangeException(nameof(patch), InvalidPatchVersionMessage);

        var prereleaseIdentifiers = prerelease?.SplitAndMapToReadOnlyList('.',
            i => new PrereleaseIdentifier(i, allowLeadingZeros, nameof(prerelease)));
        var metadataIdentifiers = metadata?.SplitAndMapToReadOnlyList('.',
            i => new MetadataIdentifier(i, nameof(metadata)));

        if (allowLeadingZeros && prereleaseIdentifiers != null)
            // Leading zeros may have been removed, need to reconstruct the prerelease string
            prerelease = string.Join(".", prereleaseIdentifiers);

        return new SemVersion(
            major ?? Major,
            minor ?? Minor,
            patch ?? Patch,
            prerelease ?? Prerelease,
            prereleaseIdentifiers ?? PrereleaseIdentifiers,
            metadata ?? Metadata,
            metadataIdentifiers ?? MetadataIdentifiers);
    }

    #region With... Methods
    /// <summary>
    /// Creates a copy of the current instance with a different major version number.
    /// </summary>
    /// <param name="major">The value to replace the major version number.</param>
    /// <returns>The new version with the different major version number.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="major"/> is negative.</exception>
    public SemVersion WithMajor(BigInteger major)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), InvalidMajorVersionMessage);
        if (Major == major) return this;
        return new SemVersion(major, Minor, Patch,
            Prerelease, PrereleaseIdentifiers, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with a different minor version number.
    /// </summary>
    /// <param name="minor">The value to replace the minor version number.</param>
    /// <returns>The new version with the different minor version number.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minor"/> is negative.</exception>
    public SemVersion WithMinor(BigInteger minor)
    {
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), InvalidMinorVersionMessage);
        if (Minor == minor) return this;
        return new SemVersion(Major, minor, Patch,
            Prerelease, PrereleaseIdentifiers, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with a different patch version number.
    /// </summary>
    /// <param name="patch">The value to replace the patch version number.</param>
    /// <returns>The new version with the different patch version number.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="patch"/> is negative.</exception>
    public SemVersion WithPatch(BigInteger patch)
    {
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch), InvalidPatchVersionMessage);
        if (Patch == patch) return this;
        return new SemVersion(Major, Minor, patch,
            Prerelease, PrereleaseIdentifiers, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with a different prerelease portion.
    /// </summary>
    /// <param name="prerelease">The value to replace the prerelease portion.</param>
    /// <param name="allowLeadingZeros">Whether to allow leading zeros in the prerelease identifiers.
    /// If <see langword="true"/>, leading zeros will be allowed on numeric identifiers
    /// but will be removed.</param>
    /// <returns>The new version with the different prerelease identifiers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prerelease"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A prerelease identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens) or has leading
    /// zeros for a numeric identifier when <paramref name="allowLeadingZeros"/> is <see langword="false"/>.</exception>
    /// <remarks>Because a valid numeric identifier does not have leading zeros, this constructor
    /// will never create a <see cref="PrereleaseIdentifier"/> with leading zeros even if
    /// <paramref name="allowLeadingZeros"/> is <see langword="true"/>. Any leading zeros will
    /// be removed.</remarks>
    public SemVersion WithPrereleaseParsedFrom(string prerelease, bool allowLeadingZeros = false)
    {
        if (prerelease is null) throw new ArgumentNullException(nameof(prerelease));
        if (prerelease.Length == 0) return WithoutPrerelease();
        var identifiers = prerelease.SplitAndMapToReadOnlyList('.',
            i => new PrereleaseIdentifier(i, allowLeadingZeros, nameof(prerelease)));
        if (allowLeadingZeros)
            // Leading zeros may have been removed, need to reconstruct the prerelease string
            prerelease = string.Join(".", identifiers);
        return new SemVersion(Major, Minor, Patch,
            prerelease, identifiers, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different prerelease identifiers.
    /// </summary>
    /// <param name="prereleaseIdentifier">The first identifier to replace the existing
    /// prerelease identifiers.</param>
    /// <param name="prereleaseIdentifiers">The rest of the identifiers to replace the
    /// existing prerelease identifiers.</param>
    /// <returns>The new version with the different prerelease identifiers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prereleaseIdentifier"/> or
    /// <paramref name="prereleaseIdentifiers"/> is <see langword="null"/> or one of the
    /// prerelease identifiers is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A prerelease identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens) or has leading
    /// zeros for a numeric identifier.</exception>
    public SemVersion WithPrerelease(string prereleaseIdentifier, params string[] prereleaseIdentifiers)
    {
        if (prereleaseIdentifier is null) throw new ArgumentNullException(nameof(prereleaseIdentifier));
        if (prereleaseIdentifiers is null) throw new ArgumentNullException(nameof(prereleaseIdentifiers));
        var identifiers = prereleaseIdentifiers
                          .Prepend(prereleaseIdentifier)
                          .Select(i => new PrereleaseIdentifier(i, allowLeadingZeros: false, nameof(prereleaseIdentifiers)))
                          .ToReadOnlyList();
        return new SemVersion(Major, Minor, Patch,
            string.Join(".", identifiers), identifiers, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different prerelease identifiers.
    /// </summary>
    /// <param name="prereleaseIdentifiers">The identifiers to replace the prerelease identifiers.</param>
    /// <returns>The new version with the different prerelease identifiers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prereleaseIdentifiers"/> is
    /// <see langword="null"/> or one of the prerelease identifiers is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A prerelease identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens) or has leading
    /// zeros for a numeric identifier.</exception>
    public SemVersion WithPrerelease(IEnumerable<string> prereleaseIdentifiers)
    {
        if (prereleaseIdentifiers is null) throw new ArgumentNullException(nameof(prereleaseIdentifiers));
        var identifiers = prereleaseIdentifiers
                          .Select(i => new PrereleaseIdentifier(i, allowLeadingZeros: false, nameof(prereleaseIdentifiers)))
                          .ToReadOnlyList();
        if (identifiers.Count == 0) return WithoutPrerelease();
        return new SemVersion(Major, Minor, Patch,
            string.Join(".", identifiers), identifiers, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different prerelease identifiers.
    /// </summary>
    /// <param name="prereleaseIdentifier">The first identifier to replace the existing
    /// prerelease identifiers.</param>
    /// <param name="prereleaseIdentifiers">The rest of the identifiers to replace the
    /// existing prerelease identifiers.</param>
    /// <returns>The new version with the different prerelease identifiers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prereleaseIdentifiers"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A prerelease identifier has the default value.</exception>
    public SemVersion WithPrerelease(
                PrereleaseIdentifier prereleaseIdentifier,
                params PrereleaseIdentifier[] prereleaseIdentifiers)
    {
        if (prereleaseIdentifiers is null) throw new ArgumentNullException(nameof(prereleaseIdentifiers));
        var identifiers = prereleaseIdentifiers.Prepend(prereleaseIdentifier).ToReadOnlyList();
        if (identifiers.Any(i => i == default)) throw new ArgumentException(PrereleaseIdentifierIsDefaultMessage, nameof(prereleaseIdentifiers));
        return new SemVersion(Major, Minor, Patch,
            string.Join(".", identifiers), identifiers, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different prerelease identifiers.
    /// </summary>
    /// <param name="prereleaseIdentifiers">The identifiers to replace the prerelease identifiers.</param>
    /// <returns>The new version with the different prerelease identifiers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prereleaseIdentifiers"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A prerelease identifier has the default value.</exception>
    public SemVersion WithPrerelease(IEnumerable<PrereleaseIdentifier> prereleaseIdentifiers)
    {
        if (prereleaseIdentifiers is null) throw new ArgumentNullException(nameof(prereleaseIdentifiers));
        var identifiers = prereleaseIdentifiers.ToReadOnlyList();
        if (identifiers.Count == 0) return WithoutPrerelease();
        if (identifiers.Any(i => i == default)) throw new ArgumentException(PrereleaseIdentifierIsDefaultMessage, nameof(prereleaseIdentifiers));
        return new SemVersion(Major, Minor, Patch,
            string.Join(".", identifiers), identifiers, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance without prerelease identifiers.
    /// </summary>
    /// <returns>The new version without prerelease identifiers.</returns>
    public SemVersion WithoutPrerelease()
    {
        if (!IsPrerelease) return this;
        return new SemVersion(Major, Minor, Patch,
            "", ReadOnlyList<PrereleaseIdentifier>.Empty, Metadata, MetadataIdentifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different build metadata.
    /// </summary>
    /// <param name="metadata">The value to replace the build metadata.</param>
    /// <returns>The new version with the different build metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A metadata identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens).</exception>
    public SemVersion WithMetadataParsedFrom(string metadata)
    {
        if (metadata is null) throw new ArgumentNullException(nameof(metadata));
        if (metadata.Length == 0) return WithoutMetadata();
        var identifiers = metadata.SplitAndMapToReadOnlyList('.',
            i => new MetadataIdentifier(i, nameof(metadata)));
        return new SemVersion(Major, Minor, Patch,
            Prerelease, PrereleaseIdentifiers, metadata, identifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different build metadata identifiers.
    /// </summary>
    /// <param name="metadataIdentifier">The first identifier to replace the existing
    /// build metadata identifiers.</param>
    /// <param name="metadataIdentifiers">The rest of the build metadata identifiers to replace the
    /// existing build metadata identifiers.</param>
    /// <returns>The new version with the different build metadata identifiers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadataIdentifier"/> or
    /// <paramref name="metadataIdentifiers"/> is <see langword="null"/> or one of the metadata
    /// identifiers is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A metadata identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens).</exception>
    public SemVersion WithMetadata(string metadataIdentifier, params string[] metadataIdentifiers)
    {
        if (metadataIdentifier is null) throw new ArgumentNullException(nameof(metadataIdentifier));
        if (metadataIdentifiers is null) throw new ArgumentNullException(nameof(metadataIdentifiers));
        var identifiers = metadataIdentifiers
                          .Prepend(metadataIdentifier)
                          .Select(i => new MetadataIdentifier(i, nameof(metadataIdentifiers)))
                          .ToReadOnlyList();
        return new SemVersion(Major, Minor, Patch,
            Prerelease, PrereleaseIdentifiers, string.Join(".", identifiers), identifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different build metadata identifiers.
    /// </summary>
    /// <param name="metadataIdentifiers">The identifiers to replace the build metadata identifiers.</param>
    /// <returns>The new version with the different build metadata identifiers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadataIdentifiers"/> is
    /// <see langword="null"/> or one of the metadata identifiers is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A metadata identifier is empty or contains invalid
    /// characters (i.e. characters that are not ASCII alphanumerics or hyphens).</exception>
    public SemVersion WithMetadata(IEnumerable<string> metadataIdentifiers)
    {
        if (metadataIdentifiers is null) throw new ArgumentNullException(nameof(metadataIdentifiers));
        var identifiers = metadataIdentifiers
                          .Select(i => new MetadataIdentifier(i, nameof(metadataIdentifiers)))
                          .ToReadOnlyList();
        if (identifiers.Count == 0) return WithoutMetadata();
        return new SemVersion(Major, Minor, Patch,
            Prerelease, PrereleaseIdentifiers, string.Join(".", identifiers), identifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different build metadata identifiers.
    /// </summary>
    /// <param name="metadataIdentifier">The first identifier to replace the existing
    /// build metadata identifiers.</param>
    /// <param name="metadataIdentifiers">The rest of the identifiers to replace the
    /// existing build metadata identifiers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="metadataIdentifiers"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A metadata identifier has the default value.</exception>
    public SemVersion WithMetadata(
        MetadataIdentifier metadataIdentifier,
        params MetadataIdentifier[] metadataIdentifiers)
    {
        if (metadataIdentifiers is null) throw new ArgumentNullException(nameof(metadataIdentifiers));
        var identifiers = metadataIdentifiers.Prepend(metadataIdentifier).ToReadOnlyList();
        if (identifiers.Any(i => i == default))
            throw new ArgumentException(MetadataIdentifierIsDefaultMessage, nameof(metadataIdentifiers));
        return new SemVersion(Major, Minor, Patch,
            Prerelease, PrereleaseIdentifiers, string.Join(".", identifiers), identifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance with different build metadata identifiers.
    /// </summary>
    /// <param name="metadataIdentifiers">The identifiers to replace the build metadata identifiers.</param>
    /// <returns>The new version with the different build metadata identifiers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadataIdentifiers"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A metadata identifier has the default value.</exception>
    public SemVersion WithMetadata(IEnumerable<MetadataIdentifier> metadataIdentifiers)
    {
        if (metadataIdentifiers is null) throw new ArgumentNullException(nameof(metadataIdentifiers));
        var identifiers = metadataIdentifiers.ToReadOnlyList();
        if (identifiers.Count == 0) return WithoutMetadata();
        if (identifiers.Any(i => i == default))
            throw new ArgumentException(MetadataIdentifierIsDefaultMessage, nameof(metadataIdentifiers));
        return new SemVersion(Major, Minor, Patch,
            Prerelease, PrereleaseIdentifiers, string.Join(".", identifiers), identifiers);
    }

    /// <summary>
    /// Creates a copy of the current instance without build metadata.
    /// </summary>
    /// <returns>The new version without build metadata.</returns>
    public SemVersion WithoutMetadata()
    {
        if (MetadataIdentifiers.Count == 0) return this;
        return new SemVersion(Major, Minor, Patch,
            Prerelease, PrereleaseIdentifiers, "", ReadOnlyList<MetadataIdentifier>.Empty);
    }

    /// <summary>
    /// Creates a copy of the current instance without prerelease identifiers or build metadata.
    /// </summary>
    /// <returns>The new version without prerelease identifiers or build metadata.</returns>
    public SemVersion WithoutPrereleaseOrMetadata()
    {
        if (!IsPrerelease && MetadataIdentifiers.Count == 0) return this;
        return new SemVersion(Major, Minor, Patch,
            "", ReadOnlyList<PrereleaseIdentifier>.Empty, "", ReadOnlyList<MetadataIdentifier>.Empty);
    }
    #endregion

    /// <summary>The major version number.</summary>
    /// <value>The major version number.</value>
    /// <remarks>An increase in the major version number indicates a backwards
    /// incompatible change.</remarks>
    public BigInteger Major { get; }

    /// <summary>The minor version number.</summary>
    /// <value>The minor version number.</value>
    /// <remarks>An increase in the minor version number indicates backwards
    /// compatible changes.</remarks>
    public BigInteger Minor { get; }

    /// <summary>The patch version number.</summary>
    /// <value>The patch version number.</value>
    /// <remarks>An increase in the patch version number indicates backwards
    /// compatible bug fixes.</remarks>
    public BigInteger Patch { get; }

    /// <summary>
    /// The prerelease identifiers for this version.
    /// </summary>
    /// <value>
    /// The prerelease identifiers for this version or empty string if this is a release version.
    /// </value>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="PrereleaseIdentifiers"]/*'/>
    // Design Note: `null` is not used to represent a release version because it is not possible to
    // express a non-empty string type, but it is possible to express a non-null string type.
    public string Prerelease { get; }

    /// <summary>
    /// The prerelease identifiers for this version.
    /// </summary>
    /// <value>
    /// The prerelease identifiers for this version or empty if this is a release version.
    /// </value>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="PrereleaseIdentifiers"]/*'/>
    public IReadOnlyList<PrereleaseIdentifier> PrereleaseIdentifiers { get; }

    /// <summary>
    /// Whether this is a prerelease version where the prerelease version is zero (i.e. "-0").
    /// </summary>
    internal bool PrereleaseIsZero
        => PrereleaseIdentifiers.Count == 1
           && PrereleaseIdentifiers[0] == PrereleaseIdentifier.Zero;

    /// <summary>Whether this is a prerelease version.</summary>
    /// <value>Whether this is a prerelease version. A semantic version with
    /// prerelease identifiers is a prerelease version.</value>
    /// <remarks>When this is <see langword="true"/>, the <see cref="Prerelease"/>
    /// and <see cref="PrereleaseIdentifiers"/> properties are non-empty. When
    /// this is <see langword="false"/>, the <see cref="Prerelease"/> property
    /// will be an empty string and the <see cref="PrereleaseIdentifiers"/> will
    /// be an empty collection.</remarks>
    public bool IsPrerelease => Prerelease.Length != 0;

    /// <summary>Whether this is a release version.</summary>
    /// <value>Whether this is a release version. A semantic version without
    /// prerelease identifiers is a release version.</value>
    /// <remarks>When this is <see langword="true"/>, the <see cref="Prerelease"/>
    /// property will be an empty string and the <see cref="PrereleaseIdentifiers"/>
    /// will be an empty collection. When this is <see langword="false"/>,
    /// the <see cref="Prerelease"/> and <see cref="PrereleaseIdentifiers"/>
    /// properties are non-empty.</remarks>
    public bool IsRelease => Prerelease.Length == 0;

    /// <summary>The build metadata for this version.</summary>
    /// <value>The build metadata for this version or empty string if there
    /// is no metadata.</value>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="MetadataIdentifiers"]/*'/>
    // Design Note: `null` is not used to represent a version without metadata because it is not
    // possible to express a non-empty string type, but it is possible to express a non-null string
    // type.
    public string Metadata { get; }

    /// <summary>The build metadata identifiers for this version.</summary>
    /// <value>The build metadata identifiers for this version or empty if there
    /// is no metadata.</value>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="MetadataIdentifiers"]/*'/>
    public IReadOnlyList<MetadataIdentifier> MetadataIdentifiers { get; }

    /// <summary>
    /// Converts this version to an equivalent string value.
    /// </summary>
    /// <returns>
    /// The <see cref="string" /> equivalent of this version.
    /// </returns>
    public override string ToString()
    {
        var major = Major.ToString();
        var minor = Minor.ToString();
        var patch = Patch.ToString();
        // Assume all separators ("..-+"), at most 4 extra chars
        var estimatedLength = 4 + major.Length
                                + minor.Length
                                + patch.Length
                                + Prerelease.Length + Metadata.Length;
        var version = new StringBuilder(estimatedLength);
        version.Append(major);
        version.Append('.');
        version.Append(minor);
        version.Append('.');
        version.Append(patch);
        if (Prerelease.Length > 0)
        {
            version.Append('-');
            version.Append(Prerelease);
        }
        if (Metadata.Length > 0)
        {
            version.Append('+');
            version.Append(Metadata);
        }
        return version.ToString();
    }

    #region Equality
    /// <summary>
    /// Determines whether two semantic versions are equal.
    /// </summary>
    /// <returns><see langword="true"/> if the two versions are equal, otherwise <see langword="false"/>.</returns>
    /// <remarks>Two versions are equal if every part of the version numbers are equal. Thus, two
    /// versions with the same precedence may not be equal.</remarks>
    public static bool Equals(SemVersion? left, SemVersion? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>Determines whether the given object is equal to this version.</summary>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is equal to this version;
    /// otherwise <see langword="false"/>.</returns>
    /// <remarks>Two versions are equal if every part of the version numbers are equal. Thus, two
    /// versions with the same precedence may not be equal.</remarks>
    public override bool Equals(object? obj)
        => obj is SemVersion version && Equals(version);

    /// <summary>
    /// Determines whether two semantic versions are equal.
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="other"/> is equal to this version;
    /// otherwise <see langword="false"/>.</returns>
    /// <remarks>Two versions are equal if every part of the version numbers are equal. Thus, two
    /// versions with the same precedence may not be equal.</remarks>
    public bool Equals(SemVersion? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Major == other.Major
            && Minor == other.Minor
            && Patch == other.Patch
            && string.Equals(Prerelease, other.Prerelease, StringComparison.Ordinal)
            && string.Equals(Metadata, other.Metadata, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether two semantic versions have the same precedence. Versions that differ
    /// only by build metadata have the same precedence.
    /// </summary>
    /// <param name="other">The semantic version to compare to.</param>
    /// <returns><see langword="true"/> if the version precedences are equal, otherwise
    /// <see langword="false"/>.</returns>
    public bool PrecedenceEquals(SemVersion? other)
        => PrecedenceComparer.Compare(this, other!) == 0;

    /// <summary>
    /// Determines whether two semantic versions have the same precedence. Versions that differ
    /// only by build metadata have the same precedence.
    /// </summary>
    /// <returns><see langword="true"/> if the version precedences are equal, otherwise
    /// <see langword="false"/>.</returns>
    public static bool PrecedenceEquals(SemVersion? left, SemVersion? right)
        => PrecedenceComparer.Compare(left!, right!) == 0;

    internal bool MajorMinorPatchEquals([NotNullWhen(true)] SemVersion? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return Major == other.Major
               && Minor == other.Minor
               && Patch == other.Patch;
    }

    /// <summary>
    /// Gets a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms
    /// and data structures like a hash table.
    /// </returns>
    /// <remarks>Two versions are equal if every part of the version numbers are equal. Thus, two
    /// versions with the same precedence may not have the same hash code.</remarks>
    public override int GetHashCode()
        => HashCode.Combine(Major, Minor, Patch, Prerelease, Metadata);

    /// <summary>
    /// Determines whether two semantic versions are equal.
    /// </summary>
    /// <returns><see langword="true"/> if the two versions are equal, otherwise <see langword="false"/>.</returns>
    /// <remarks>Two versions are equal if every part of the version numbers are equal. Thus, two
    /// versions with the same precedence may not be equal.</remarks>
    public static bool operator ==(SemVersion? left, SemVersion? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two semantic versions are <em>not</em> equal.
    /// </summary>
    /// <returns><see langword="true"/> if the two versions are <em>not</em> equal, otherwise <see langword="false"/>.</returns>
    /// <remarks>Two versions are equal if every part of the version numbers are equal. Thus, two
    /// versions with the same precedence may not be equal.</remarks>
    public static bool operator !=(SemVersion? left, SemVersion? right) => !Equals(left, right);
    #endregion

    #region Comparison
    /// <summary>
    /// An <see cref="IEqualityComparer{T}"/> and <see cref="IComparer{T}"/>
    /// that compares <see cref="SemVersion"/> by precedence. This can be used for sorting,
    /// binary search, and using <see cref="SemVersion"/> as a dictionary key.
    /// </summary>
    /// <value>A precedence comparer that implements <see cref="IEqualityComparer{T}"/> and
    /// <see cref="IComparer{T}"/> for <see cref="SemVersion"/>.</value>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="PrecedenceOrder"]/*'/>
    public static ISemVersionComparer PrecedenceComparer { get; } = Comparers.PrecedenceComparer.Instance;

    /// <summary>
    /// An <see cref="IEqualityComparer{T}"/> and <see cref="IComparer{T}"/>
    /// that compares <see cref="SemVersion"/> by sort order. This can be used for sorting,
    /// binary search, and using <see cref="SemVersion"/> as a dictionary key.
    /// </summary>
    /// <value>A sort order comparer that implements <see cref="IEqualityComparer{T}"/> and
    /// <see cref="IComparer{T}"/> for <see cref="SemVersion"/>.</value>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="SortOrder"]/*'/>
    public static ISemVersionComparer SortOrderComparer { get; } = Comparers.SortOrderComparer.Instance;

    /// <summary>
    /// Compares two versions and indicates whether this instance precedes, follows, or is in the same
    /// position as the other in the precedence order. Versions that differ only by build metadata
    /// have the same precedence.
    /// </summary>
    /// <returns>
    /// An integer that indicates whether this instance precedes, follows, or is in the same
    /// position as <paramref name="other"/> in the precedence order.
    /// <list type="table">
    ///     <listheader>
    ///         <term>Value</term>
    ///         <description>Condition</description>
    ///     </listheader>
    ///     <item>
    ///         <term>-1</term>
    ///         <description>This instance precedes <paramref name="other"/> in the precedence order.</description>
    ///     </item>
    ///     <item>
    ///         <term>0</term>
    ///         <description>This instance has the same precedence as <paramref name="other"/>.</description>
    ///     </item>
    ///     <item>
    ///         <term>1</term>
    ///         <description>
    ///             This instance follows <paramref name="other"/> in the precedence order
    ///             or <paramref name="other"/> is <see langword="null" />.
    ///         </description>
    ///     </item>
    /// </list>
    /// </returns>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="PrecedenceOrder"]/*'/>
    public int ComparePrecedenceTo(SemVersion? other) => PrecedenceComparer.Compare(this, other!);

    /// <summary>
    /// Compares two versions and indicates whether this instance precedes, follows, or is equal
    /// to the other in the sort order. Note that sort order is more specific than precedence order.
    /// </summary>
    /// <returns>
    /// An integer that indicates whether this instance precedes, follows, or is equal to the
    /// other in the sort order.
    /// <list type="table">
    /// 	<listheader>
    /// 		<term>Value</term>
    /// 		<description>Condition</description>
    /// 	</listheader>
    /// 	<item>
    /// 		<term>-1</term>
    /// 		<description>This instance precedes the other in the sort order.</description>
    /// 	</item>
    /// 	<item>
    /// 		<term>0</term>
    /// 		<description>This instance is equal to the other.</description>
    /// 	</item>
    /// 	<item>
    /// 		<term>1</term>
    /// 		<description>
    /// 			This instance follows the other in the sort order
    /// 			or the other is <see langword="null" />.
    /// 		</description>
    /// 	</item>
    /// </list>
    /// </returns>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="SortOrder"]/*'/>
    public int CompareSortOrderTo(SemVersion? other) => SortOrderComparer.Compare(this, other!);

    /// <summary>
    /// Compares two versions and indicates whether the first precedes, follows, or is in the same
    /// position as the second in the precedence order. Versions that differ only by build metadata
    /// have the same precedence.
    /// </summary>
    /// <returns>
    /// An integer that indicates whether <paramref name="left"/> precedes, follows, or is in the same
    /// position as <paramref name="right"/> in the precedence order.
    /// <list type="table">
    ///     <listheader>
    ///         <term>Value</term>
    ///         <description>Condition</description>
    ///     </listheader>
    ///     <item>
    ///         <term>-1</term>
    ///         <description>
    ///             <paramref name="left"/> precedes <paramref name="right"/> in the precedence
    ///             order or <paramref name="left"/> is <see langword="null" />.</description>
    ///     </item>
    ///     <item>
    ///         <term>0</term>
    ///         <description><paramref name="left"/> has the same precedence as <paramref name="right"/>.</description>
    ///     </item>
    ///     <item>
    ///         <term>1</term>
    ///         <description>
    ///             <paramref name="left"/> follows <paramref name="right"/> in the precedence order
    ///             or <paramref name="right"/> is <see langword="null" />.
    ///         </description>
    ///     </item>
    /// </list>
    /// </returns>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="PrecedenceOrder"]/*'/>
    public static int ComparePrecedence(SemVersion? left, SemVersion? right)
        => PrecedenceComparer.Compare(left!, right!);

    /// <summary>
    /// Compares two versions and indicates whether the first precedes, follows, or is equal to
    /// the second in the sort order. Note that sort order is more specific than precedence order.
    /// </summary>
    /// <returns>
    /// An integer that indicates whether <paramref name="left"/> precedes, follows, or is equal
    /// to <paramref name="right"/> in the sort order.
    /// <list type="table">
    ///     <listheader>
    ///         <term>Value</term>
    ///         <description>Condition</description>
    ///     </listheader>
    ///     <item>
    ///         <term>-1</term>
    ///         <description>
    ///             <paramref name="left"/> precedes <paramref name="right"/> in the sort
    ///             order or <paramref name="left"/> is <see langword="null" />.</description>
    ///     </item>
    ///     <item>
    ///         <term>0</term>
    ///         <description><paramref name="left"/> is equal to <paramref name="right"/>.</description>
    ///     </item>
    ///     <item>
    ///         <term>1</term>
    ///         <description>
    ///             <paramref name="left"/> follows <paramref name="right"/> in the sort order
    ///             or <paramref name="right"/> is <see langword="null" />.
    ///         </description>
    ///     </item>
    /// </list>
    /// </returns>
    /// <include file='SemVersionDocParts.xml' path='docParts/part[@id="SortOrder"]/*'/>
    public static int CompareSortOrder(SemVersion? left, SemVersion? right)
        => SortOrderComparer.Compare(left!, right!);
    #endregion

    #region Satisfies
    /// <summary>
    /// Checks if this version satisfies the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate on this version.</param>
    /// <returns><see langword="true"/> if the version satisfies the predicate,
    /// otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is
    /// <see langword="null"/>.</exception>
    public bool Satisfies(Predicate<SemVersion> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return predicate(this);
    }

    /// <summary>
    /// Checks if this version is contained in the given range.
    /// </summary>
    /// <param name="range">The range to evaluate.</param>
    /// <returns><see langword="true"/> if the version is contained in the range,
    /// otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is
    /// <see langword="null"/>.</exception>
    public bool Satisfies(SemVersionRange range)
    {
        if (range is null) throw new ArgumentNullException(nameof(range));
        return range.Contains(this);
    }

    /// <summary>
    /// Checks if this version is contained in the given unbroken range.
    /// </summary>
    /// <param name="range">The unbroken range to evaluate.</param>
    /// <returns><see langword="true"/> if the version is contained in the range,
    /// otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is
    /// <see langword="null"/>.</exception>
    public bool Satisfies(UnbrokenSemVersionRange range)
    {
        if (range is null) throw new ArgumentNullException(nameof(range));
        return range.Contains(this);
    }

    /// <summary>
    /// Checks if this version is contained in the given range.
    /// </summary>
    /// <param name="range">The range to parse and evaluate.</param>
    /// <param name="options">A bitwise combination of enumeration values that indicates the style
    /// elements that can be present in <paramref name="range"/>. The overload without this
    /// parameter defaults to <see cref="SemVersionRangeOptions.Strict"/>.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long range strings.</param>
    /// <returns><see langword="true"/> if the version is contained in the range,
    /// otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> is not a valid
    /// <see cref="SemVersionRangeOptions"/> value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than
    /// zero.</exception>
    /// <exception cref="FormatException">The <paramref name="range"/> is invalid or not in a
    /// format compliant with <paramref name="options"/>.</exception>
    /// <remarks>If checks against a range will be performed repeatedly, it is much more
    /// efficient to parse the range into a <see cref="SemVersionRange"/> once and use that
    /// object to repeatedly check for containment.</remarks>
    public bool Satisfies(
        string range,
        SemVersionRangeOptions options,
        int maxLength = SemVersionRange.MaxRangeLength)
    {
        if (range == null) throw new ArgumentNullException(nameof(range));

        var parsedRange = SemVersionRange.Parse(range, options, maxLength);
        return parsedRange.Contains(this);
    }

    /// <summary>
    /// Checks if this version is contained in the given range. The range is parsed using
    /// <see cref="SemVersionRangeOptions.Strict"/>.
    /// </summary>
    /// <param name="range">The range to parse and evaluate.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long range strings.</param>
    /// <returns><see langword="true"/> if the version is contained in the range,
    /// otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than
    /// zero.</exception>
    /// <exception cref="FormatException">The <paramref name="range"/> is invalid or not in a
    /// format compliant with <see cref="SemVersionRangeOptions.Strict"/>.</exception>
    /// <remarks>If checks against a range will be performed repeatedly, it is much more
    /// efficient to parse the range into a <see cref="SemVersionRange"/> once and use that
    /// object to repeatedly check for containment.</remarks>
    public bool Satisfies(string range, int maxLength = SemVersionRange.MaxRangeLength)
        => Satisfies(range, SemVersionRangeOptions.Strict, maxLength);

    /// <summary>
    /// Checks if this version is contained in the given range in npm format.
    /// </summary>
    /// <param name="range">The npm format range to parse and evaluate.</param>
    /// <param name="includeAllPrerelease">Whether to include all prerelease versions satisfying
    /// the bounds in the range or to only include prerelease versions when it matches a bound
    /// that explicitly includes prerelease versions.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long range strings.</param>
    /// <returns><see langword="true"/> if the version is contained in the range,
    /// otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than
    /// zero.</exception>
    /// <exception cref="FormatException">The <paramref name="range"/> is invalid.</exception>
    /// <remarks>If checks against a range will be performed repeatedly, it is much more
    /// efficient to parse the range into a <see cref="SemVersionRange"/> once using
    /// <see cref="SemVersionRange.ParseNpm(string,bool,int)"/> and use that object to
    /// repeatedly check for containment.</remarks>
    public bool SatisfiesNpm(string range, bool includeAllPrerelease, int maxLength = SemVersionRange.MaxRangeLength)
    {
        if (range == null) throw new ArgumentNullException(nameof(range));

        var parsedRange = SemVersionRange.ParseNpm(range, includeAllPrerelease, maxLength);
        return parsedRange.Contains(this);
    }

    /// <summary>
    /// Checks if this version is contained in the given range in npm format. Does not include
    /// all prerelease when parsing the range.
    /// </summary>
    /// <param name="range">The npm format range to parse and evaluate.</param>
    /// <param name="maxLength">The maximum length of <paramref name="range"/> that should be
    /// parsed. This prevents attacks using very long range strings.</param>
    /// <returns><see langword="true"/> if the version is contained in the range,
    /// otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="range"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than
    /// zero.</exception>
    /// <exception cref="FormatException">The <paramref name="range"/> is invalid.</exception>
    /// <remarks>If checks against a range will be performed repeatedly, it is much more
    /// efficient to parse the range into a <see cref="SemVersionRange"/> once using
    /// <see cref="SemVersionRange.ParseNpm(string,int)"/> and use that object to
    /// repeatedly check for containment.</remarks>
    public bool SatisfiesNpm(string range, int maxLength = SemVersionRange.MaxRangeLength)
        => SatisfiesNpm(range, false, maxLength);
    #endregion
}
