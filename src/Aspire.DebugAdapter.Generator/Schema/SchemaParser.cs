// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.DebugAdapter.Generator.Schema;

/// <summary>
/// Parses a JSON schema document into classified types.
/// </summary>
public sealed class SchemaParser
{
    private static readonly HashSet<string> s_abstractBaseTypes = new() { "ProtocolMessage", "Request", "Event", "Response" };
    private static readonly HashSet<string> s_reverseRequestTypes = new() { "RunInTerminalRequest", "StartDebuggingRequest" };

    private readonly JsonSchemaDocument _schema;
    private readonly Dictionary<string, ParsedType> _parsedTypes = new();
    private readonly Dictionary<string, ParsedType> _bodyTypes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaParser"/> class.
    /// </summary>
    /// <param name="schema">The JSON schema document to parse.</param>
    public SchemaParser(JsonSchemaDocument schema)
    {
        _schema = schema;
    }

    /// <summary>
    /// Parses the schema and returns all classified types.
    /// </summary>
    /// <returns>A dictionary of type name to parsed type.</returns>
    public Dictionary<string, ParsedType> Parse()
    {
        // First pass: extract inline body types
        ExtractAllBodyTypes();

        // Second pass: parse all definitions
        foreach (var (name, definition) in _schema.Definitions)
        {
            var parsedType = ParseDefinition(name, definition);
            _parsedTypes[name] = parsedType;
        }

        // Add extracted body types
        foreach (var (name, bodyType) in _bodyTypes)
        {
            _parsedTypes[name] = bodyType;
        }

        return _parsedTypes;
    }

    private void ExtractAllBodyTypes()
    {
        foreach (var (name, definition) in _schema.Definitions)
        {
            if (definition.AllOf is not { } allOf)
            {
                continue;
            }

            foreach (var entry in allOf)
            {
                if (entry.Properties is null)
                {
                    continue;
                }

                if (entry.Properties.TryGetValue("body", out var bodyProp))
                {
                    // Check if this is an inline object definition (not a $ref)
                    if (bodyProp.Ref is null && bodyProp.Properties is not null)
                    {
                        var bodyTypeName = $"{name}Body";
                        ExtractBodyType(bodyTypeName, bodyProp);
                    }
                }
            }
        }
    }

    private void ExtractBodyType(string bodyTypeName, JsonSchemaProperty bodyProp)
    {
        if (_bodyTypes.ContainsKey(bodyTypeName))
        {
            return;
        }

        var properties = new List<ParsedProperty>();
        var requiredProperties = new HashSet<string>();

        if (bodyProp.Required is not null)
        {
            foreach (var req in bodyProp.Required)
            {
                requiredProperties.Add(req);
            }
        }

        if (bodyProp.Properties is not null)
        {
            foreach (var (propName, propDef) in bodyProp.Properties)
            {
                var prop = ParseProperty(bodyTypeName, propName, propDef);
                properties.Add(prop with { IsRequired = requiredProperties.Contains(propName) });
            }
        }

        _bodyTypes[bodyTypeName] = new ParsedType
        {
            Name = bodyTypeName,
            Kind = TypeKind.Body,
            Description = bodyProp.Description,
            Properties = properties,
            RequiredProperties = requiredProperties
        };
    }

    private ParsedType ParseDefinition(string name, JsonSchemaDefinition definition)
    {
        var kind = ClassifyType(name, definition);
        var baseTypeName = GetBaseTypeName(definition);
        var discriminatorValue = GetDiscriminatorValue(name, kind, definition);

        var properties = new List<ParsedProperty>();
        var requiredProperties = new HashSet<string>();

        // Collect properties and required from allOf chain
        if (definition.AllOf is { } allOf)
        {
            foreach (var entry in allOf)
            {
                if (entry.Ref is null && entry.Properties is not null)
                {
                    foreach (var (propName, propDef) in entry.Properties)
                    {
                        var prop = ParseProperty(name, propName, propDef);
                        properties.Add(prop);
                    }
                }
                if (entry.Required is not null)
                {
                    foreach (var req in entry.Required)
                    {
                        requiredProperties.Add(req);
                    }
                }
            }
        }

        // Collect from direct properties
        if (definition.Properties is not null)
        {
            foreach (var (propName, propDef) in definition.Properties)
            {
                var prop = ParseProperty(name, propName, propDef);
                properties.Add(prop);
            }
        }

        if (definition.Required is not null)
        {
            foreach (var req in definition.Required)
            {
                requiredProperties.Add(req);
            }
        }

        // Handle pure enum types
        List<string>? enumValues = null;
        List<string>? enumDescriptions = null;
        if (kind == TypeKind.Enum)
        {
            enumValues = definition.Enum;
            enumDescriptions = definition.EnumDescriptions;
        }

        // Update IsRequired on properties
        var parsedProperties = properties.Select(p => p with
        {
            IsRequired = requiredProperties.Contains(p.JsonName)
        }).ToList();

        return new ParsedType
        {
            Name = name,
            Kind = kind,
            BaseTypeName = baseTypeName,
            Title = definition.Title ?? GetTitleFromAllOf(definition),
            Description = definition.Description ?? GetDescriptionFromAllOf(definition),
            Properties = parsedProperties,
            RequiredProperties = requiredProperties,
            EnumValues = enumValues,
            EnumDescriptions = enumDescriptions,
            DiscriminatorValue = discriminatorValue,
            IsReverseRequest = s_reverseRequestTypes.Contains(name)
        };
    }

    private ParsedProperty ParseProperty(string parentName, string propName, JsonSchemaProperty propDef)
    {
        var type = ParsePropertyType(parentName, propName, propDef);
        var csharpName = ToPascalCase(propName);

        return new ParsedProperty
        {
            Name = csharpName,
            JsonName = propName,
            Type = type,
            Description = propDef.Description,
            IsRequired = false, // Will be set later
            SoftEnumValues = propDef.SoftEnum,
            EnumDescriptions = propDef.EnumDescriptions
        };
    }

    private ParsedPropertyType ParsePropertyType(string parentName, string propName, JsonSchemaProperty propDef)
    {
        // Handle $ref
        if (propDef.Ref is { } refPath)
        {
            var refTypeName = ExtractRefName(refPath);
            return new ParsedPropertyType
            {
                Kind = PropertyTypeKind.Reference,
                TypeName = refTypeName
            };
        }

        // Handle inline object (body types)
        if (propName == "body" && propDef.Properties is not null && propDef.Ref is null)
        {
            var bodyTypeName = $"{parentName}Body";
            return new ParsedPropertyType
            {
                Kind = PropertyTypeKind.Reference,
                TypeName = bodyTypeName
            };
        }

        // Handle type element
        if (propDef.Type is { } typeElement)
        {
            if (typeElement.ValueKind == JsonValueKind.String)
            {
                var typeStr = typeElement.GetString()!;
                return ParseSimpleType(typeStr, propDef);
            }

            // Handle type array (union types)
            if (typeElement.ValueKind == JsonValueKind.Array)
            {
                return ParseUnionType(typeElement);
            }
        }

        // Handle additionalProperties for dictionary (without explicit type)
        if (propDef.AdditionalProperties is { } addProps)
        {
            return ParseDictionaryType(addProps);
        }

        // Default to any/object
        return new ParsedPropertyType
        {
            Kind = PropertyTypeKind.Any,
            TypeName = "object"
        };
    }

    private ParsedPropertyType ParseSimpleType(string typeStr, JsonSchemaProperty propDef)
    {
        switch (typeStr)
        {
            case "string":
                return new ParsedPropertyType
                {
                    Kind = PropertyTypeKind.Simple,
                    TypeName = "string"
                };
            case "integer":
                return new ParsedPropertyType
                {
                    Kind = PropertyTypeKind.Simple,
                    TypeName = "int"
                };
            case "number":
                return new ParsedPropertyType
                {
                    Kind = PropertyTypeKind.Simple,
                    TypeName = "double"
                };
            case "boolean":
                return new ParsedPropertyType
                {
                    Kind = PropertyTypeKind.Simple,
                    TypeName = "bool"
                };
            case "array":
                var elementType = propDef.Items is not null
                    ? ParsePropertyType("", "", propDef.Items)
                    : new ParsedPropertyType { Kind = PropertyTypeKind.Any, TypeName = "object" };
                return new ParsedPropertyType
                {
                    Kind = PropertyTypeKind.Array,
                    ElementType = elementType
                };
            case "object":
                if (propDef.AdditionalProperties is { } addProps)
                {
                    return ParseDictionaryType(addProps);
                }
                return new ParsedPropertyType
                {
                    Kind = PropertyTypeKind.Any,
                    TypeName = "object"
                };
            default:
                return new ParsedPropertyType
                {
                    Kind = PropertyTypeKind.Any,
                    TypeName = "object"
                };
        }
    }

    private static ParsedPropertyType ParseUnionType(JsonElement typeArray)
    {
        var types = new List<string>();
        bool hasNull = false;

        foreach (var element in typeArray.EnumerateArray())
        {
            var t = element.GetString();
            if (t == "null")
            {
                hasNull = true;
            }
            else if (t is not null)
            {
                types.Add(t);
            }
        }

        // If only one non-null type, treat as simple nullable
        if (types.Count == 1)
        {
            var simpleType = types[0] switch
            {
                "string" => "string",
                "integer" => "int",
                "number" => "double",
                "boolean" => "bool",
                _ => "object"
            };
            return new ParsedPropertyType
            {
                Kind = PropertyTypeKind.Simple,
                TypeName = simpleType,
                IsNullable = hasNull
            };
        }

        // Full union - use object
        return new ParsedPropertyType
        {
            Kind = PropertyTypeKind.Union,
            TypeName = "object",
            UnionTypes = types,
            IsNullable = hasNull
        };
    }

    private static ParsedPropertyType ParseDictionaryType(JsonElement addProps)
    {
        if (addProps.ValueKind == JsonValueKind.True)
        {
            // additionalProperties: true -> Dictionary<string, JsonElement>
            return new ParsedPropertyType
            {
                Kind = PropertyTypeKind.Dictionary,
                ValueType = new ParsedPropertyType
                {
                    Kind = PropertyTypeKind.Any,
                    TypeName = "JsonElement"
                }
            };
        }

        if (addProps.ValueKind == JsonValueKind.Object)
        {
            // Parse the value type
            if (addProps.TryGetProperty("type", out var valueType))
            {
                if (valueType.ValueKind == JsonValueKind.String)
                {
                    var valueTypeName = valueType.GetString() switch
                    {
                        "string" => "string",
                        "integer" => "int",
                        "number" => "double",
                        "boolean" => "bool",
                        _ => "object"
                    };
                    return new ParsedPropertyType
                    {
                        Kind = PropertyTypeKind.Dictionary,
                        ValueType = new ParsedPropertyType
                        {
                            Kind = PropertyTypeKind.Simple,
                            TypeName = valueTypeName
                        }
                    };
                }
                else if (valueType.ValueKind == JsonValueKind.Array)
                {
                    // Nullable value type like ["string", "null"]
                    var parsed = ParseUnionType(valueType);
                    return new ParsedPropertyType
                    {
                        Kind = PropertyTypeKind.Dictionary,
                        ValueType = parsed
                    };
                }
            }
        }

        return new ParsedPropertyType
        {
            Kind = PropertyTypeKind.Dictionary,
            ValueType = new ParsedPropertyType
            {
                Kind = PropertyTypeKind.Any,
                TypeName = "object"
            }
        };
    }

    private TypeKind ClassifyType(string name, JsonSchemaDefinition definition)
    {
        if (s_abstractBaseTypes.Contains(name))
        {
            return TypeKind.AbstractBase;
        }

        // Check for pure enum
        if (definition.Type is { } typeEl &&
            typeEl.ValueKind == JsonValueKind.String &&
            typeEl.GetString() == "string" &&
            definition.Enum is not null)
        {
            return TypeKind.Enum;
        }

        // Check inheritance chain via allOf
        var baseType = GetBaseTypeName(definition);

        if (baseType is not null)
        {
            // Direct inheritance from base types
            if (name.EndsWith("Request") && baseType == "Request")
            {
                return TypeKind.Request;
            }

            if (name.EndsWith("Response") && baseType == "Response")
            {
                return TypeKind.Response;
            }

            if (name.EndsWith("Event") && baseType == "Event")
            {
                return TypeKind.Event;
            }

            // Check for ErrorResponse which extends Response
            if (name.EndsWith("Response") && IsDescendantOf(baseType, "Response"))
            {
                return TypeKind.Response;
            }
        }

        if (name.EndsWith("Arguments"))
        {
            return TypeKind.Arguments;
        }

        return TypeKind.SimpleObject;
    }

    private bool IsDescendantOf(string typeName, string ancestorName)
    {
        if (typeName == ancestorName)
        {
            return true;
        }

        if (!_schema.Definitions.TryGetValue(typeName, out var definition))
        {
            return false;
        }

        var baseType = GetBaseTypeName(definition);
        if (baseType is null)
        {
            return false;
        }

        return IsDescendantOf(baseType, ancestorName);
    }

    private static string? GetBaseTypeName(JsonSchemaDefinition definition)
    {
        if (definition.AllOf is not { } allOf)
        {
            return null;
        }

        foreach (var entry in allOf)
        {
            if (entry.Ref is { } refPath)
            {
                return ExtractRefName(refPath);
            }
        }

        return null;
    }

    private string? GetDiscriminatorValue(string name, TypeKind kind, JsonSchemaDefinition definition)
    {
        if (kind is not (TypeKind.Request or TypeKind.Response or TypeKind.Event))
        {
            return null;
        }

        // Look for command or event enum value in allOf properties
        if (definition.AllOf is { } allOf)
        {
            foreach (var entry in allOf)
            {
                if (entry.Properties is null)
                {
                    continue;
                }

                if (kind == TypeKind.Event && entry.Properties.TryGetValue("event", out var eventProp))
                {
                    return eventProp.Enum?.FirstOrDefault();
                }

                if ((kind == TypeKind.Request || kind == TypeKind.Response) && entry.Properties.TryGetValue("command", out var commandProp))
                {
                    return commandProp.Enum?.FirstOrDefault();
                }
            }
        }

        // For responses without explicit command property, derive from name
        // e.g., InitializeResponse -> "initialize", SetBreakpointsResponse -> "setBreakpoints"
        if (kind == TypeKind.Response && name.EndsWith("Response"))
        {
            var baseName = name.Substring(0, name.Length - "Response".Length);
            // Convert PascalCase to camelCase
            if (!string.IsNullOrEmpty(baseName))
            {
                return char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);
            }
        }

        return null;
    }

    private static string ExtractRefName(string refPath)
    {
        const string prefix = "#/definitions/";
        return refPath.StartsWith(prefix) ? refPath.Substring(prefix.Length) : refPath;
    }

    /// <summary>
    /// Converts a name to PascalCase.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The PascalCase version of the name.</returns>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // Handle special case of __restart -> Restart
        if (name.StartsWith("__"))
        {
            return ToPascalCase(name.Substring(2));
        }

        // Handle underscores: convert snake_case to PascalCase
        if (name.Contains('_'))
        {
            var parts = name.Split('_');
            return string.Concat(parts.Select(p =>
                string.IsNullOrEmpty(p) ? "" : char.ToUpperInvariant(p[0]) + p.Substring(1)));
        }

        // Handle already PascalCase or single char
        if (char.IsUpper(name[0]))
        {
            return name;
        }

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    private static string? GetTitleFromAllOf(JsonSchemaDefinition definition)
    {
        return definition.AllOf?.FirstOrDefault(a => a.Title is not null)?.Title;
    }

    private static string? GetDescriptionFromAllOf(JsonSchemaDefinition definition)
    {
        return definition.AllOf?.FirstOrDefault(a => a.Description is not null)?.Description;
    }
}
