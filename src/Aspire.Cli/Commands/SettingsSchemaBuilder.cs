// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
using Aspire.Cli.Configuration;

namespace Aspire.Cli.Commands;

/// <summary>
/// Builds schema information from AspireJsonConfiguration using reflection.
/// </summary>
internal static class SettingsSchemaBuilder
{
    /// <summary>
    /// Builds a SettingsSchema from AspireJsonConfiguration by inspecting its properties.
    /// </summary>
    /// <param name="excludeLocalOnly">If true, excludes properties marked with <see cref="LocalAspireJsonConfigurationPropertyAttribute"/>.</param>
    public static SettingsSchema BuildSchema(bool excludeLocalOnly)
    {
        var properties = new List<PropertyInfo>();
        var type = typeof(AspireJsonConfiguration);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Skip extension data property as it's for capturing additional properties
            if (prop.GetCustomAttribute<JsonExtensionDataAttribute>() != null)
            {
                continue;
            }

            // Skip local-only properties when building global settings schema
            if (excludeLocalOnly && prop.GetCustomAttribute<LocalAspireJsonConfigurationPropertyAttribute>() != null)
            {
                continue;
            }

            var jsonPropertyName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
            
            // Skip $schema as it's a special JSON Schema property, not a user-editable setting
            if (jsonPropertyName == "$schema")
            {
                continue;
            }
            
            var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
            var jsonType = GetJsonType(prop.PropertyType);
            
            // For now, no properties are strictly required (all nullable/optional)
            const bool required = false;

            properties.Add(new PropertyInfo(jsonPropertyName, jsonType, description, required));
        }

        return new SettingsSchema(properties.OrderBy(p => p.Name).ToList());
    }

    /// <summary>
    /// Maps C# types to JSON schema types.
    /// </summary>
    private static string GetJsonType(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
        {
            return "string";
        }
        
        if (underlyingType == typeof(bool))
        {
            return "boolean";
        }
        
        if (underlyingType == typeof(int) || underlyingType == typeof(long) || 
            underlyingType == typeof(decimal) || underlyingType == typeof(double) || 
            underlyingType == typeof(float))
        {
            return "number";
        }
        
        if (typeof(System.Collections.IDictionary).IsAssignableFrom(underlyingType))
        {
            return "object";
        }
        
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string))
        {
            return "array";
        }
        
        return "object";
    }
}
