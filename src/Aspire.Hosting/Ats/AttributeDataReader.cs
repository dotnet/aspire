// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Provides full-name-based discovery of ATS attributes from <see cref="CustomAttributeData"/>,
/// so that third-party authors can define their own attribute types with matching names and namespace
/// without requiring a package reference to Aspire.Hosting.
/// </summary>
internal static class AttributeDataReader
{
    private static readonly string s_aspireExportAttributeFullName = typeof(AspireExportAttribute).FullName!;
    private static readonly string s_aspireExportIgnoreAttributeFullName = typeof(AspireExportIgnoreAttribute).FullName!;
    private static readonly string s_aspireDtoAttributeFullName = typeof(AspireDtoAttribute).FullName!;
    private static readonly string s_aspireUnionAttributeFullName = typeof(AspireUnionAttribute).FullName!;

    // --- AspireExport lookup ---

    internal static AspireExportData? GetAspireExportData(Type type)
        => FindSingleAttribute<AspireExportData>(type.GetCustomAttributesData(), s_aspireExportAttributeFullName, ParseAspireExportData);

    internal static AspireExportData? GetAspireExportData(MethodInfo method)
        => FindSingleAttribute<AspireExportData>(method.GetCustomAttributesData(), s_aspireExportAttributeFullName, ParseAspireExportData);

    internal static AspireExportData? GetAspireExportData(PropertyInfo property)
        => FindSingleAttribute<AspireExportData>(property.GetCustomAttributesData(), s_aspireExportAttributeFullName, ParseAspireExportData);

    internal static IEnumerable<AspireExportData> GetAspireExportDataAll(Assembly assembly)
        => FindAllAttributes(assembly.GetCustomAttributesData(), s_aspireExportAttributeFullName, ParseAspireExportData);

    // --- AspireExportIgnore lookup ---

    internal static bool HasAspireExportIgnoreData(PropertyInfo property)
        => HasAttribute(property.GetCustomAttributesData(), s_aspireExportIgnoreAttributeFullName);

    internal static bool HasAspireExportIgnoreData(MethodInfo method)
        => HasAttribute(method.GetCustomAttributesData(), s_aspireExportIgnoreAttributeFullName);

    // --- AspireDto lookup ---

    internal static bool HasAspireDtoData(Type type)
        => HasAttribute(type.GetCustomAttributesData(), s_aspireDtoAttributeFullName);

    // --- AspireUnion lookup ---

    internal static AspireUnionData? GetAspireUnionData(ParameterInfo parameter)
        => FindSingleAttribute<AspireUnionData>(parameter.GetCustomAttributesData(), s_aspireUnionAttributeFullName, ParseAspireUnionData);

    internal static AspireUnionData? GetAspireUnionData(PropertyInfo property)
        => FindSingleAttribute<AspireUnionData>(property.GetCustomAttributesData(), s_aspireUnionAttributeFullName, ParseAspireUnionData);

    // --- Generic helpers ---

    private static bool HasAttribute(IList<CustomAttributeData> attributes, string attributeFullName)
    {
        for (var i = 0; i < attributes.Count; i++)
        {
            if (IsMatch(attributes[i], attributeFullName))
            {
                return true;
            }
        }

        return false;
    }

    private static T? FindSingleAttribute<T>(IList<CustomAttributeData> attributes, string attributeFullName, Func<CustomAttributeData, T> parser) where T : class
    {
        for (var i = 0; i < attributes.Count; i++)
        {
            if (IsMatch(attributes[i], attributeFullName))
            {
                return parser(attributes[i]);
            }
        }

        return null;
    }

    private static IEnumerable<T> FindAllAttributes<T>(IList<CustomAttributeData> attributes, string attributeFullName, Func<CustomAttributeData, T> parser) where T : class
    {
        for (var i = 0; i < attributes.Count; i++)
        {
            if (IsMatch(attributes[i], attributeFullName))
            {
                yield return parser(attributes[i]);
            }
        }
    }

    private static bool IsMatch(CustomAttributeData data, string attributeFullName)
    {
        return string.Equals(data.AttributeType.FullName, attributeFullName, StringComparison.Ordinal);
    }

    // --- Parsers ---

    private static AspireExportData ParseAspireExportData(CustomAttributeData data)
    {
        string? id = null;
        Type? type = null;

        // Match constructor arguments by signature (arity + type) rather than parameter name,
        // so third-party attribute copies with different parameter names still work.
        // The three recognized constructor signatures are:
        //   ()           — type export
        //   (string)     — capability export (the string is the method/capability id)
        //   (Type)       — assembly-level type export
        if (data.ConstructorArguments.Count == 1)
        {
            var arg = data.ConstructorArguments[0];
            if (arg.Value is string idValue)
            {
                id = idValue;
            }
            else if (arg.Value is Type typeValue)
            {
                type = typeValue;
            }
        }

        // Read named arguments
        string? description = null;
        string? methodName = null;
        var exposeProperties = false;
        var exposeMethods = false;

        for (var i = 0; i < data.NamedArguments.Count; i++)
        {
            var named = data.NamedArguments[i];
            switch (named.MemberName)
            {
                case nameof(AspireExportData.Type):
                    if (named.TypedValue.Value is Type namedType)
                    {
                        type = namedType;
                    }
                    break;
                case nameof(AspireExportData.Description):
                    description = named.TypedValue.Value as string;
                    break;
                case nameof(AspireExportData.MethodName):
                    methodName = named.TypedValue.Value as string;
                    break;
                case nameof(AspireExportData.ExposeProperties):
                    if (named.TypedValue.Value is bool ep)
                    {
                        exposeProperties = ep;
                    }
                    break;
                case nameof(AspireExportData.ExposeMethods):
                    if (named.TypedValue.Value is bool em)
                    {
                        exposeMethods = em;
                    }
                    break;
            }
        }

        return new AspireExportData
        {
            Id = id,
            Type = type,
            Description = description,
            MethodName = methodName,
            ExposeProperties = exposeProperties,
            ExposeMethods = exposeMethods
        };
    }

    private static AspireUnionData ParseAspireUnionData(CustomAttributeData data)
    {
        // The constructor is AspireUnionAttribute(params Type[] types).
        // CustomAttributeData represents params as either:
        //   1. A single constructor argument of type Type[] (ReadOnlyCollection<CustomAttributeTypedArgument>)
        //   2. Multiple individual constructor arguments of type Type
        var types = new List<Type>();

        if (data.ConstructorArguments.Count == 1 &&
            data.ConstructorArguments[0].Value is IReadOnlyCollection<CustomAttributeTypedArgument> elements)
        {
            // params represented as a single array argument
            foreach (var element in elements)
            {
                if (element.Value is Type t)
                {
                    types.Add(t);
                }
            }
        }
        else
        {
            // params represented as individual arguments
            for (var i = 0; i < data.ConstructorArguments.Count; i++)
            {
                if (data.ConstructorArguments[i].Value is Type t)
                {
                    types.Add(t);
                }
            }
        }

        return new AspireUnionData
        {
            Types = [.. types]
        };
    }
}

/// <summary>
/// Name-based adapter for [AspireExport] attribute data, parsed from <see cref="CustomAttributeData"/>.
/// </summary>
internal sealed class AspireExportData
{
    /// <summary>
    /// Gets the method name / capability id from the constructor argument.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the CLR type for assembly-level type exports.
    /// </summary>
    public Type? Type { get; init; }

    /// <summary>
    /// Gets a description of what this export does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the method name override for generated polyglot SDKs.
    /// </summary>
    public string? MethodName { get; init; }

    /// <summary>
    /// Gets whether to expose properties of this type as ATS capabilities.
    /// </summary>
    public bool ExposeProperties { get; init; }

    /// <summary>
    /// Gets whether to expose public instance methods of this type as ATS capabilities.
    /// </summary>
    public bool ExposeMethods { get; init; }
}

/// <summary>
/// Name-based adapter for [AspireUnion] attribute data, parsed from <see cref="CustomAttributeData"/>.
/// </summary>
internal sealed class AspireUnionData
{
    /// <summary>
    /// Gets the CLR types that form the union.
    /// </summary>
    public required Type[] Types { get; init; }
}
