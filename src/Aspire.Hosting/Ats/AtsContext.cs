// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Contains all scanned types, capabilities, and metadata from ATS assembly scanning.
/// </summary>
public sealed class AtsContext
{
    private HashSet<Type>? _dtoTypes;
    private HashSet<Type>? _handleTypes;

    /// <summary>
    /// Gets the capabilities discovered during scanning.
    /// </summary>
    public required IReadOnlyList<AtsCapabilityInfo> Capabilities { get; init; }

    /// <summary>
    /// Gets the type information for all discovered types.
    /// </summary>
    public required IReadOnlyList<AtsTypeInfo> TypeInfos { get; init; }

    /// <summary>
    /// Gets the DTO types discovered during scanning.
    /// </summary>
    public required IReadOnlyList<AtsDtoTypeInfo> DtoTypes { get; init; }

    /// <summary>
    /// Gets the enum types discovered during scanning.
    /// </summary>
    public required IReadOnlyList<AtsEnumTypeInfo> EnumTypes { get; init; }

    /// <summary>
    /// Gets any diagnostics (warnings/errors) generated during scanning.
    /// </summary>
    public IReadOnlyList<AtsDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Runtime registry mapping capability IDs to methods.
    /// Internal - only used by dispatcher, not part of the serializable model.
    /// </summary>
    internal Dictionary<string, MethodInfo> Methods { get; } = new();

    /// <summary>
    /// Runtime registry mapping capability IDs to properties.
    /// Internal - only used by dispatcher, not part of the serializable model.
    /// </summary>
    internal Dictionary<string, PropertyInfo> Properties { get; } = new();

    /// <summary>
    /// Gets the type category for a CLR type based on scanned data.
    /// Used at runtime for marshalling.
    /// </summary>
    /// <param name="type">The CLR type to classify.</param>
    /// <returns>The type category.</returns>
    public AtsTypeCategory GetCategory(Type type)
    {
        // Check primitives
        if (AtsConstants.IsPrimitiveType(type))
        {
            return AtsTypeCategory.Primitive;
        }

        // Check enums
        if (type.IsEnum)
        {
            return AtsTypeCategory.Enum;
        }

        // Check arrays
        if (type.IsArray)
        {
            return AtsTypeCategory.Array;
        }

        // Check generic collections
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();

            if (genericDef == typeof(List<>) || genericDef == typeof(IList<>))
            {
                return AtsTypeCategory.List;
            }

            if (genericDef == typeof(IReadOnlyList<>) || genericDef == typeof(IReadOnlyCollection<>))
            {
                return AtsTypeCategory.Array;
            }

            if (genericDef == typeof(Dictionary<,>) || genericDef == typeof(IDictionary<,>) ||
                genericDef == typeof(IReadOnlyDictionary<,>))
            {
                return AtsTypeCategory.Dict;
            }
        }

        // Check delegates
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            return AtsTypeCategory.Callback;
        }

        // Check scanned DTOs
        _dtoTypes ??= new HashSet<Type>(DtoTypes.Where(d => d.ClrType != null).Select(d => d.ClrType!));
        if (_dtoTypes.Contains(type))
        {
            return AtsTypeCategory.Dto;
        }

        // Check scanned handle types (TypeInfos are [AspireExport] types)
        _handleTypes ??= new HashSet<Type>(TypeInfos.Where(t => t.ClrType != null).Select(t => t.ClrType!));
        if (_handleTypes.Contains(type))
        {
            return AtsTypeCategory.Handle;
        }

        // Fallback to Handle for unknown reference types at runtime
        return AtsTypeCategory.Handle;
    }
}
