// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;

namespace Aspire.Hosting.RemoteHost.Ats;

internal static class AtsScannerAdapter
{
    internal static AtsScanArtifacts ProjectArtifacts(object isolatedContext)
    {
        return new AtsScanArtifacts
        {
            Context = ProjectContext(isolatedContext),
            IsolatedContext = isolatedContext
        };
    }

    private static AtsContext ProjectContext(object scanResult)
    {
        return new AtsContext
        {
            Capabilities = MapList(GetRequiredProperty(scanResult, "Capabilities"), MapCapabilityInfo),
            HandleTypes = MapList(GetRequiredProperty(scanResult, "HandleTypes"), MapTypeInfo),
            DtoTypes = MapList(GetRequiredProperty(scanResult, "DtoTypes"), MapDtoTypeInfo),
            EnumTypes = MapList(GetRequiredProperty(scanResult, "EnumTypes"), MapEnumTypeInfo),
            Diagnostics = MapList(GetRequiredProperty(scanResult, "Diagnostics"), MapDiagnostic)
        };
    }

    private static List<TLocal> MapList<TLocal>(object sourceCollection, Func<object, TLocal> map)
    {
        var items = new List<TLocal>();
        foreach (var item in (IEnumerable)sourceCollection)
        {
            if (item is not null)
            {
                items.Add(map(item));
            }
        }

        return items;
    }

    private static AtsCapabilityInfo MapCapabilityInfo(object source) => new()
    {
        CapabilityId = GetRequiredString(source, "CapabilityId"),
        MethodName = GetRequiredString(source, "MethodName"),
        OwningTypeName = GetOptionalString(source, "OwningTypeName"),
        Description = GetOptionalString(source, "Description"),
        Parameters = MapList(GetRequiredProperty(source, "Parameters"), MapParameterInfo),
        ReturnType = MapTypeRef(GetRequiredProperty(source, "ReturnType")),
        TargetTypeId = GetOptionalString(source, "TargetTypeId"),
        TargetType = GetOptionalObject(source, "TargetType") is { } targetType ? MapTypeRef(targetType) : null,
        TargetParameterName = GetOptionalString(source, "TargetParameterName"),
        ExpandedTargetTypes = MapList(GetRequiredProperty(source, "ExpandedTargetTypes"), MapTypeRef),
        ReturnsBuilder = GetRequiredValue<bool>(source, "ReturnsBuilder"),
        CapabilityKind = ParseEnum<AtsCapabilityKind>(source, "CapabilityKind"),
        SourceLocation = GetOptionalString(source, "SourceLocation")
    };

    private static AtsParameterInfo MapParameterInfo(object source) => new()
    {
        Name = GetRequiredString(source, "Name"),
        Type = GetOptionalObject(source, "Type") is { } type ? MapTypeRef(type) : null,
        IsOptional = GetRequiredValue<bool>(source, "IsOptional"),
        IsNullable = GetRequiredValue<bool>(source, "IsNullable"),
        IsCallback = GetRequiredValue<bool>(source, "IsCallback"),
        CallbackParameters = GetOptionalObject(source, "CallbackParameters") is { } callbackParameters
            ? MapList(callbackParameters, MapCallbackParameterInfo)
            : null,
        CallbackReturnType = GetOptionalObject(source, "CallbackReturnType") is { } callbackReturnType
            ? MapTypeRef(callbackReturnType)
            : null,
        DefaultValue = GetOptionalObject(source, "DefaultValue")
    };

    private static AtsCallbackParameterInfo MapCallbackParameterInfo(object source) => new()
    {
        Name = GetRequiredString(source, "Name"),
        Type = MapTypeRef(GetRequiredProperty(source, "Type"))
    };

    private static AtsTypeRef MapTypeRef(object source) => new()
    {
        TypeId = GetRequiredString(source, "TypeId"),
        Category = ParseEnum<AtsTypeCategory>(source, "Category"),
        IsInterface = GetRequiredValue<bool>(source, "IsInterface"),
        ElementType = GetOptionalObject(source, "ElementType") is { } elementType ? MapTypeRef(elementType) : null,
        KeyType = GetOptionalObject(source, "KeyType") is { } keyType ? MapTypeRef(keyType) : null,
        ValueType = GetOptionalObject(source, "ValueType") is { } valueType ? MapTypeRef(valueType) : null,
        IsReadOnly = GetRequiredValue<bool>(source, "IsReadOnly"),
        UnionTypes = GetOptionalObject(source, "UnionTypes") is { } unionTypes
            ? MapList(unionTypes, MapTypeRef)
            : null
    };

    private static AtsTypeInfo MapTypeInfo(object source) => new()
    {
        AtsTypeId = GetRequiredString(source, "AtsTypeId"),
        IsInterface = GetRequiredValue<bool>(source, "IsInterface"),
        ImplementedInterfaces = MapList(GetRequiredProperty(source, "ImplementedInterfaces"), MapTypeRef),
        BaseTypeHierarchy = MapList(GetRequiredProperty(source, "BaseTypeHierarchy"), MapTypeRef),
        HasExposeProperties = GetRequiredValue<bool>(source, "HasExposeProperties"),
        HasExposeMethods = GetRequiredValue<bool>(source, "HasExposeMethods")
    };

    private static AtsDtoTypeInfo MapDtoTypeInfo(object source) => new()
    {
        TypeId = GetRequiredString(source, "TypeId"),
        Name = GetRequiredString(source, "Name"),
        Description = GetOptionalString(source, "Description"),
        Properties = MapList(GetRequiredProperty(source, "Properties"), MapDtoPropertyInfo)
    };

    private static AtsDtoPropertyInfo MapDtoPropertyInfo(object source) => new()
    {
        Name = GetRequiredString(source, "Name"),
        Description = GetOptionalString(source, "Description"),
        Type = MapTypeRef(GetRequiredProperty(source, "Type")),
        IsOptional = GetRequiredValue<bool>(source, "IsOptional")
    };

    private static AtsEnumTypeInfo MapEnumTypeInfo(object source) => new()
    {
        TypeId = GetRequiredString(source, "TypeId"),
        Name = GetRequiredString(source, "Name"),
        Values = MapList(GetRequiredProperty(source, "Values"), value => (string)value)
    };

    private static AtsDiagnostic MapDiagnostic(object source) => new()
    {
        Severity = ParseEnum<AtsDiagnosticSeverity>(source, "Severity"),
        Message = GetRequiredString(source, "Message"),
        Location = GetOptionalString(source, "Location")
    };

    private static TEnum ParseEnum<TEnum>(object source, string propertyName)
        where TEnum : struct, Enum
    {
        var value = GetRequiredProperty(source, propertyName);
        return value switch
        {
            TEnum typed => typed,
            _ => Enum.Parse<TEnum>(value.ToString()!, ignoreCase: false)
        };
    }

    private static T GetRequiredValue<T>(object source, string propertyName)
    {
        var value = GetRequiredProperty(source, propertyName);
        return value is T typed
            ? typed
            : (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string GetRequiredString(object source, string propertyName) =>
        GetRequiredProperty(source, propertyName) as string
        ?? throw new InvalidOperationException($"Property '{propertyName}' on '{source.GetType().FullName}' was null.");

    private static string? GetOptionalString(object source, string propertyName) =>
        GetOptionalObject(source, propertyName) as string;

    private static object GetRequiredProperty(object source, string propertyName) =>
        GetOptionalObject(source, propertyName)
        ?? throw new InvalidOperationException($"Property '{propertyName}' was not found or was null on '{source.GetType().FullName}'.");

    private static object? GetOptionalObject(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is null)
        {
            return null;
        }

        return property.GetValue(source);
    }
}

internal sealed class AtsScanArtifacts
{
    public required AtsContext Context { get; init; }

    public required object IsolatedContext { get; init; }
}
