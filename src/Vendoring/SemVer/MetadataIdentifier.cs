using System;
using System.Runtime.CompilerServices;
using Semver.Utility;

namespace Semver;

/// <summary>
/// An individual metadata identifier for a semantic version.
/// </summary>
/// <remarks>
/// <para>The metadata for a semantic version is composed of dot ('<c>.</c>') separated identifiers.
/// A valid identifier is a non-empty string of ASCII alphanumeric and hyphen characters
/// (<c>[0-9A-Za-z-]</c>). Metadata identifiers are compared lexically in ASCII sort order.</para>
///
/// <para>Because <see cref="MetadataIdentifier"/> is a struct, the default value is a
/// <see cref="MetadataIdentifier"/> with a <see langword="null"/> value. However, the
/// <see cref="Semver"/> namespace types do not accept and will not return such a
/// <see cref="MetadataIdentifier"/>.</para>
/// </remarks>
internal readonly struct MetadataIdentifier : IEquatable<MetadataIdentifier>, IComparable<MetadataIdentifier>, IComparable
{
    /// <summary>
    /// The string value of the metadata identifier.
    /// </summary>
    /// <value>The string value of this metadata identifier or <see langword="null"/> if this is
    /// a default <see cref="MetadataIdentifier"/>.</value>
    public string Value { get; }

    /// <summary>
    /// Constructs a <see cref="MetadataIdentifier"/> without checking that any of the invariants
    /// hold. Used by the parser for performance.
    /// </summary>
    /// <remarks>This is a create method rather than a constructor to clearly indicate uses
    /// of it. The other constructors have not been hidden behind create methods because only
    /// constructors are visible to the package users. So they see a class consistently
    /// using constructors without any create methods.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static MetadataIdentifier CreateUnsafe(string value)
    {
        DebugChecks.IsNotNull(value, nameof(value));
        DebugChecks.IsNotEmpty(value, nameof(value));
#if DEBUG
            if (!value.IsAlphanumericOrHyphens())
                throw new ArgumentException($"DEBUG: A metadata identifier can contain only ASCII alphanumeric characters and hyphens '{value}'.", nameof(value));
#endif
        return new MetadataIdentifier(value, UnsafeOverload.Marker);
    }

    /// <summary>
    /// Private constructor used by <see cref="CreateUnsafe"/>.
    /// </summary>
    /// <param name="value">The value for the identifier. Not validated.</param>
    /// <param name="_">Unused parameter that differentiates this from the
    /// constructor that performs validation.</param>
    private MetadataIdentifier(string value, UnsafeOverload _)
    {
        Value = value;
    }

    /// <summary>
    /// Constructs a valid <see cref="MetadataIdentifier"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The <paramref name="value"/> is empty or contains invalid characters
    /// (i.e. characters that are not ASCII alphanumerics or hyphens).</exception>
    public MetadataIdentifier(string value)
        : this(value, nameof(value))
    {
    }

    /// <summary>
    /// Constructs a valid <see cref="MetadataIdentifier"/>.
    /// </summary>
    /// <remarks>
    /// Internal constructor allows changing the parameter name to enable methods using this
    /// as part of their metadata identifier validation to match the parameter name to their
    /// parameter name.
    /// </remarks>
    internal MetadataIdentifier(string value, string paramName)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        if (value.Length == 0)
            throw new ArgumentException("Metadata identifier cannot be empty.", paramName);
        if (!value.IsAlphanumericOrHyphens())
            throw new ArgumentException($"A metadata identifier can contain only ASCII alphanumeric characters and hyphens '{value}'.", paramName);

        Value = value;
    }

    #region Equality
    /// <summary>
    /// Determines whether two identifiers are equal.
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="value"/> is equal to this identifier;
    /// otherwise <see langword="false"/>.</returns>
    public bool Equals(MetadataIdentifier value) => Value == value.Value;

    /// <summary>Determines whether the given object is equal to this identifier.</summary>
    /// <returns><see langword="true"/> if <paramref name="value"/> is equal to this identifier;
    /// otherwise <see langword="false"/>.</returns>
    public override bool Equals(object? value)
        => value is MetadataIdentifier other && Equals(other);

    /// <summary>Gets a hash code for this identifier.</summary>
    /// <returns>A hash code for this identifier.</returns>
    public override int GetHashCode() => HashCode.Combine(Value);

    /// <summary>
    /// Determines whether two identifiers are equal.
    /// </summary>
    /// <returns><see langword="true"/> if the value of <paramref name="left"/> is the same as
    /// the value of <paramref name="right"/>; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(MetadataIdentifier left, MetadataIdentifier right)
        => left.Value == right.Value;

    /// <summary>
    /// Determines whether two identifiers are <em>not</em> equal.
    /// </summary>
    /// <returns><see langword="true"/> if the value of <paramref name="left"/> is different
    /// from the value of <paramref name="right"/>; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(MetadataIdentifier left, MetadataIdentifier right)
        => left.Value != right.Value;
    #endregion

    #region Comparison
    /// <summary>
    /// Compares two identifiers and indicates whether this instance precedes, follows, or is
    /// equal to the other in sort order.
    /// </summary>
    /// <returns>
    /// An integer that indicates whether this instance precedes, follows, or is equal to
    /// <paramref name="value"/> in sort order.
    /// <list type="table">
    ///     <listheader>
    ///         <term>Value</term>
    ///         <description>Condition</description>
    ///     </listheader>
    ///     <item>
    ///          <term>-1</term>
    ///          <description>This instance precedes <paramref name="value"/>.</description>
    ///     </item>
    ///     <item>
    ///          <term>0</term>
    ///          <description>This instance is equal to <paramref name="value"/>.</description>
    ///     </item>
    ///     <item>
    ///          <term>1</term>
    ///          <description>This instance follows <paramref name="value"/>.</description>
    ///     </item>
    /// </list>
    /// </returns>
    /// <remarks>Identifiers are compared lexically in ASCII sort order. Invalid identifiers are
    /// compared via an ordinal string comparison.</remarks>
    public int CompareTo(MetadataIdentifier value)
        => IdentifierString.Compare(Value, value.Value);

    /// <summary>
    /// Compares this identifier to an <see cref="Object"/> and indicates whether this instance
    /// precedes, follows, or is equal to the object in sort order.
    /// </summary>
    /// <returns>
    /// An integer that indicates whether this instance precedes, follows, or is equal to
    /// <paramref name="value"/> in sort order.
    /// <list type="table">
    ///     <listheader>
    ///         <term>Value</term>
    ///         <description>Condition</description>
    ///     </listheader>
    ///     <item>
    ///          <term>-1</term>
    ///          <description>This instance precedes <paramref name="value"/>.</description>
    ///     </item>
    ///     <item>
    ///          <term>0</term>
    ///          <description>This instance is equal to <paramref name="value"/>.</description>
    ///     </item>
    ///     <item>
    ///          <term>1</term>
    ///          <description>This instance follows <paramref name="value"/> or <paramref name="value"/>
    ///                         is <see langword="null"/>.</description>
    ///     </item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a <see cref="MetadataIdentifier"/>.</exception>
    /// <remarks>Identifiers are compared lexically in ASCII sort order. Invalid identifiers are
    /// compared via an ordinal string comparison.</remarks>
    public int CompareTo(object? value)
    {
        if (value is null) return 1;
        return value is MetadataIdentifier other
            ? IdentifierString.Compare(Value, other.Value)
            : throw new ArgumentException($"Object must be of type {nameof(MetadataIdentifier)}.", nameof(value));
    }
    #endregion

    /// <summary>
    /// Converts this identifier into an equivalent string value.
    /// </summary>
    /// <returns>The string value of this identifier or <see langword="null"/> if this is
    /// a default <see cref="MetadataIdentifier"/>.</returns>
    public static implicit operator string(MetadataIdentifier metadataIdentifier)
        => metadataIdentifier.Value;

    /// <summary>
    /// Converts this identifier into an equivalent string value.
    /// </summary>
    /// <returns>The string value of this identifier or <see langword="null"/> if this is
    /// a default <see cref="MetadataIdentifier"/></returns>
    public override string ToString() => Value;
}
