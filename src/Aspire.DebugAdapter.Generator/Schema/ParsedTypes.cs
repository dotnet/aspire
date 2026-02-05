// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.DebugAdapter.Generator.Schema;

/// <summary>
/// Classification of a parsed type.
/// </summary>
public enum TypeKind
{
    /// <summary>Abstract base types like ProtocolMessage, Request, Event, Response.</summary>
    AbstractBase,
    /// <summary>Concrete request types.</summary>
    Request,
    /// <summary>Concrete response types.</summary>
    Response,
    /// <summary>Concrete event types.</summary>
    Event,
    /// <summary>Request/response arguments.</summary>
    Arguments,
    /// <summary>Other object types (Breakpoint, Source, etc.).</summary>
    SimpleObject,
    /// <summary>Pure string enums.</summary>
    Enum,
    /// <summary>Extracted inline body types.</summary>
    Body
}

/// <summary>
/// Represents a parsed and classified type from the schema.
/// </summary>
public sealed class ParsedType
{
    /// <summary>The name of the type.</summary>
    public required string Name { get; init; }

    /// <summary>The classification of the type.</summary>
    public required TypeKind Kind { get; init; }

    /// <summary>The name of the base type, if any.</summary>
    public string? BaseTypeName { get; init; }

    /// <summary>The title from the schema.</summary>
    public string? Title { get; init; }

    /// <summary>The description from the schema.</summary>
    public string? Description { get; init; }

    /// <summary>The properties of the type.</summary>
    public List<ParsedProperty> Properties { get; init; } = new();

    /// <summary>The set of required property names.</summary>
    public HashSet<string> RequiredProperties { get; init; } = new();

    /// <summary>The enum values for enum types.</summary>
    public List<string>? EnumValues { get; init; }

    /// <summary>The descriptions for enum values.</summary>
    public List<string>? EnumDescriptions { get; init; }

    /// <summary>
    /// For requests/events: the discriminator value (e.g., "initialize" for InitializeRequest).
    /// </summary>
    public string? DiscriminatorValue { get; init; }

    /// <summary>
    /// Whether this is a reverse request (adapter→client).
    /// </summary>
    public bool IsReverseRequest { get; init; }
}

/// <summary>
/// Represents a parsed property.
/// </summary>
public sealed record ParsedProperty
{
    /// <summary>The C# property name.</summary>
    public required string Name { get; init; }

    /// <summary>The JSON property name.</summary>
    public required string JsonName { get; init; }

    /// <summary>The resolved type of the property.</summary>
    public required ParsedPropertyType Type { get; init; }

    /// <summary>The description from the schema.</summary>
    public string? Description { get; init; }

    /// <summary>Whether this property is required.</summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// For _enum properties: the known constant values.
    /// </summary>
    public List<string>? SoftEnumValues { get; init; }

    /// <summary>The descriptions for enum values.</summary>
    public List<string>? EnumDescriptions { get; init; }
}

/// <summary>
/// Represents a resolved property type.
/// </summary>
public sealed class ParsedPropertyType
{
    /// <summary>The kind of property type.</summary>
    public required PropertyTypeKind Kind { get; init; }

    /// <summary>
    /// For simple types: the C# type name.
    /// For references: the referenced type name.
    /// For arrays: null (see ElementType).
    /// </summary>
    public string? TypeName { get; init; }

    /// <summary>
    /// For array types: the element type.
    /// </summary>
    public ParsedPropertyType? ElementType { get; init; }

    /// <summary>
    /// For dictionary types: the value type.
    /// </summary>
    public ParsedPropertyType? ValueType { get; init; }

    /// <summary>
    /// For union types: the constituent type names.
    /// </summary>
    public List<string>? UnionTypes { get; init; }

    /// <summary>
    /// Whether this type is nullable (appears in a union with "null").
    /// </summary>
    public bool IsNullable { get; init; }
}

/// <summary>
/// Classification of property types.
/// </summary>
public enum PropertyTypeKind
{
    /// <summary>Simple types: string, int, bool, double.</summary>
    Simple,
    /// <summary>Reference to another type via $ref.</summary>
    Reference,
    /// <summary>Array with items.</summary>
    Array,
    /// <summary>Dictionary via additionalProperties.</summary>
    Dictionary,
    /// <summary>Union type array (excluding null).</summary>
    Union,
    /// <summary>Any type (full union or object).</summary>
    Any
}
