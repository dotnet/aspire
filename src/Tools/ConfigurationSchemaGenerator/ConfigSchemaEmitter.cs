// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using InternalSymbolExtensions = Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions;

namespace ConfigurationSchemaGenerator;

internal sealed class ConfigSchemaEmitter(SchemaGenerationSpec spec, Compilation compilation)
{
    internal const string RootPathPrefix = "--empty--";
    private static readonly string[] s_lineBreaks = ["\r\n", "\r", "\n"];

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true,
        // ensure the properties are ordered correctly
        Converters = { SchemaOrderJsonNodeConverter.Instance },
        // prevent known escaped characters from being \uxxxx encoded
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly TypeIndex _typeIndex = new TypeIndex(spec.AllTypes);
    private readonly Compilation _compilation = compilation;
    private readonly Stack<TypeSpec> _visitedTypes = new();
    private readonly string[] _exclusionPaths = CreateExclusionPaths(spec.ExclusionPaths);

    public string GenerateSchema()
    {
        var root = new JsonObject();
        GenerateLogCategories(root);
        GenerateGraph(root);

        return JsonSerializer.Serialize(root, s_serializerOptions);
    }

    private void GenerateLogCategories(JsonObject parent)
    {
        var categories = spec.LogCategories;
        if (categories is null)
        {
            return;
        }

        var propertiesNode = new JsonObject();
        for (var i = 0; i < categories.Count; i++)
        {
            var catObj = new JsonObject();
            catObj["$ref"] = "#/definitions/logLevelThreshold";
            propertiesNode.Add(categories[i], catObj);
        }

        parent["definitions"] = new JsonObject
        {
            ["logLevel"] = new JsonObject
            {
                ["properties"] = propertiesNode
            }
        };
    }

    private void GenerateGraph(JsonObject rootNode)
    {
        if (spec.ConfigurationTypes.Count > 0)
        {
            if (spec.ConfigurationTypes.Count != spec.ConfigurationPaths?.Count)
            {
                throw new InvalidOperationException("Ensure Types and ConfigurationPaths are the same length.");
            }

            for (var i = 0; i < spec.ConfigurationPaths.Count; i++)
            {
                var type = spec.ConfigurationTypes[i];
                var path = spec.ConfigurationPaths[i];

                var pathSegments = new Queue<string>();
                foreach (var segment in path.Split(':').Where(segment => !segment.StartsWith(RootPathPrefix)))
                {
                    pathSegments.Enqueue(segment);
                }

                GenerateComponent(rootNode, type, pathSegments);
            }
        }
    }

    private bool GenerateComponent(JsonObject currentNode, TypeSpec type, Queue<string> pathSegments)
    {
        if (pathSegments.Count == 0)
        {
            return GenerateType(currentNode, type);
        }

        var pathSegment = pathSegments.Dequeue();

        var backupTypeNode = currentNode["type"];
        currentNode["type"] = "object";

        var ownsProperties = false;
        if (currentNode["properties"] is not JsonObject propertiesNode)
        {
            propertiesNode = new JsonObject();
            currentNode["properties"] = propertiesNode;
            ownsProperties = true;
        }

        var ownsComponent = false;
        if (propertiesNode[pathSegment] is not JsonObject componentNode)
        {
            componentNode = new JsonObject();
            propertiesNode[pathSegment] = componentNode;
            ownsComponent = true;
        }

        var hasGenerated = GenerateComponent(componentNode, type, pathSegments);
        if (!hasGenerated)
        {
            RestoreBackup(backupTypeNode, "type", currentNode);

            if (ownsProperties)
            {
                currentNode.Remove("properties");
            }
            else if (ownsComponent)
            {
                propertiesNode.Remove(pathSegment);
            }
        }

        return hasGenerated;
    }

    private bool GenerateType(JsonObject currentNode, TypeSpec type)
    {
        bool hasGenerated;

        if (type is ParsableFromStringSpec parsable)
        {
            GenerateParsableFromString(currentNode, parsable);
            hasGenerated = true;
        }
        else if (type is NullableSpec nullable)
        {
            var effectiveType = _typeIndex.GetTypeSpec(nullable.EffectiveTypeRef);
            hasGenerated = GenerateType(currentNode, effectiveType);
        }
        else if (type is ObjectSpec objectSpec)
        {
            hasGenerated = GenerateObject(currentNode, objectSpec);
        }
        else if (type is DictionarySpec dictionary)
        {
            hasGenerated = GenerateCollection(currentNode, dictionary, "object", "additionalProperties");
        }
        else if (type is CollectionSpec collection)
        {
            hasGenerated = GenerateCollection(currentNode, collection, "array", "items");
        }
        else if (type is UnsupportedTypeSpec)
        {
            // skip unsupported types
            hasGenerated = false;
        }
        else
        {
            throw new InvalidOperationException($"Unknown type {type}");
        }

        if (hasGenerated)
        {
            GenerateDescriptionForType(currentNode, type);
        }

        return hasGenerated;
    }

    private bool GenerateObject(JsonObject currentNode, ObjectSpec objectSpec)
    {
        if (_visitedTypes.Contains(objectSpec))
        {
            // Infinite recursion: keep all parent nodes, but suppress IntelliSense from here by not setting any type.
            return true;
        }
        _visitedTypes.Push(objectSpec);

        var hasGenerated = false;

        var properties = objectSpec.Properties;
        if (properties?.Count > 0)
        {
            var backupTypeNode = currentNode["type"];
            currentNode["type"] = "object";

            var ownsProperties = false;
            if (currentNode["properties"] is not JsonObject propertiesNode)
            {
                propertiesNode = new JsonObject();
                currentNode["properties"] = propertiesNode;
                ownsProperties = true;
            }

            foreach (var property in properties)
            {
                if (_typeIndex.ShouldBindTo(property) && !IsExcluded(propertiesNode, property))
                {
                    var propertySymbol = GetPropertySymbol(objectSpec, property);
                    hasGenerated |= GenerateProperty(propertiesNode, property, propertySymbol);
                }
            }

            if (!hasGenerated)
            {
                RestoreBackup(backupTypeNode, "type", currentNode);

                if (ownsProperties)
                {
                    currentNode.Remove("properties");
                }
            }
        }

        _visitedTypes.Pop();
        return hasGenerated;
    }

    private bool GenerateProperty(JsonObject currentNode, PropertySpec property, IPropertySymbol? propertySymbol)
    {
        var propertyType = _typeIndex.GetTypeSpec(property.TypeRef);

        if (ShouldSkipProperty(property, propertyType, propertySymbol))
        {
            return false;
        }

        var backupPropertyNode = currentNode[property.ConfigurationKeyName];

        var propertyNode = new JsonObject();
        currentNode[property.ConfigurationKeyName] = propertyNode;

        var hasGenerated = GenerateType(propertyNode, propertyType);
        if (hasGenerated)
        {
            var docComment = GetDocComment(propertySymbol);
            if (!string.IsNullOrEmpty(docComment))
            {
                GenerateDescriptionFromDocComment(propertyNode, docComment);
            }
        }
        else
        {
            RestoreBackup(backupPropertyNode, property.ConfigurationKeyName, currentNode);
        }

        return hasGenerated;
    }

    private bool GenerateCollection(JsonObject currentNode, CollectionSpec collection, string typeName, string containerName)
    {
        if (collection.TypeRef.Equals(collection.ElementTypeRef) || _visitedTypes.Contains(collection))
        {
            // Infinite recursion: keep all parent nodes, but suppress IntelliSense from here by not setting any type.
            return true;
        }
        _visitedTypes.Push(collection);

        var backupTypeNode = currentNode["type"];
        var backupContainerNode = currentNode[containerName];

        currentNode["type"] = typeName;
        var containerNode = new JsonObject();
        currentNode[containerName] = containerNode;

        var elementType = _typeIndex.GetTypeSpec(collection.ElementTypeRef);
        var hasGenerated = GenerateType(containerNode, elementType);
        if (!hasGenerated)
        {
            RestoreBackup(backupTypeNode, "type", currentNode);
            RestoreBackup(backupContainerNode, containerName, currentNode);
        }

        _visitedTypes.Pop();
        return hasGenerated;
    }

    private static void RestoreBackup(JsonNode? backupNode, string name, JsonObject parentNode)
    {
        if (backupNode == null)
        {
            parentNode.Remove(name);
        }
        else
        {
            parentNode[name] = backupNode;
        }
    }

    private string? GetDocComment(ISymbol? symbol)
    {
        if (symbol != null)
        {
            var comment = InternalSymbolExtensions.GetDocumentationComment(symbol, _compilation, expandIncludes: true, expandInheritdoc: true);
            var xml = comment.FullXmlFragment;

            if (!string.IsNullOrEmpty(xml) && xml != "<doc />")
            {
                return XElement.Parse(xml).ToString(SaveOptions.None);
            }
        }

        return null;
    }

    private IPropertySymbol? GetPropertySymbol(TypeSpec type, PropertySpec property)
    {
        IPropertySymbol? propertySymbol = null;
        var typeSymbol = _compilation.GetBestTypeByMetadataName(type.FullName) as ITypeSymbol;
        while (propertySymbol is null && typeSymbol is not null)
        {
            propertySymbol = typeSymbol.GetMembers(property.Name).FirstOrDefault() as IPropertySymbol;
            typeSymbol = typeSymbol.BaseType;
        }

        return propertySymbol;
    }

    private static bool ShouldSkipProperty(PropertySpec property, TypeSpec propertyType, IPropertySymbol? propertySymbol)
    {
        if (propertyType is UnsupportedTypeSpec)
        {
            return true;
        }

        if (property.IsStatic && !property.CanSet)
        {
            return true;
        }

        // skip simple properties that can't be set
        // TODO: this should allow for init properties set through the constructor. Need to figure out the correct rule here.
        if (propertyType is not ComplexTypeSpec &&
            !property.CanSet)
        {
            return true;
        }

        // skip [Obsolete] or [EditorBrowsable(EditorBrowsableState.Never)]
        var attributes = propertySymbol?.GetAttributes();
        if (attributes is not null)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute")
                {
                    return true;
                }
                else if (attribute.AttributeClass?.ToDisplayString() == "System.ComponentModel.EditorBrowsableAttribute" &&
                    attribute.ConstructorArguments.Length == 1 &&
                    attribute.ConstructorArguments[0].Value is int value &&
                    value == 1) // EditorBrowsableState.Never
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void GenerateDescriptionFromDocComment(JsonObject propertyNode, string docComment)
    {
        var doc = XDocument.Parse(docComment);
        var memberRoot = doc.Element("member") ?? doc.Element("doc");
        var summary = memberRoot?.Element("summary");
        if (summary is not null)
        {
            var description = FormatDescription(summary);

            if (description.Length > 0)
            {
                propertyNode["description"] = description;
            }
        }

        var propertyNodeType = propertyNode["type"];
        if (propertyNodeType?.GetValueKind() == JsonValueKind.String && propertyNodeType.GetValue<string>() == "boolean")
        {
            var value = memberRoot?.Element("value")?.ToString();
            if (value?.Contains("default value is", StringComparison.OrdinalIgnoreCase) == true)
            {
                var containsTrue = value.Contains("true", StringComparison.OrdinalIgnoreCase);
                var containsFalse = value.Contains("false", StringComparison.OrdinalIgnoreCase);
                if (containsTrue && !containsFalse)
                {
                    propertyNode["default"] = true;
                }
                else if (!containsTrue && containsFalse)
                {
                    propertyNode["default"] = false;
                }
            }
        }
    }

    internal static string FormatDescription(XElement element)
    {
        var text = string.Join(string.Empty, element.Nodes().Select(GetNodeText));
        var lines = text.Split(s_lineBreaks, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', lines)
            .Replace(" <br/> ", "\n")
            .Replace(" <br/>", "\n")
            .Replace("<br/> ", "\n")
            .Replace("<br/>", "\n")
            .Replace("\n\n\n\n", "\n\n")
            .Trim('\n');
    }

    private void GenerateDescriptionForType(JsonObject? currentNode, TypeSpec type)
    {
        if (currentNode is not null && currentNode["description"] is null)
        {
            var typeSymbol = _compilation.GetBestTypeByMetadataName(type.FullName);
            if (typeSymbol is not null)
            {
                var docComment = GetDocComment(typeSymbol);
                if (!string.IsNullOrEmpty(docComment))
                {
                    GenerateDescriptionFromDocComment(currentNode, docComment);
                }
            }
        }
    }

    private static string GetNodeText(XNode node)
    {
        return node switch
        {
            XText text => text.Value,
            XElement element => GetElementText(element),
            _ => string.Empty
        };
    }

    private static string GetElementText(XElement element)
    {
        if (element.Name == "para" || element.Name == "p")
        {
            return $"<br/><br/>{element.Value}<br/><br/>";
        }

        if( element.Name == "br")
        {
            return "<br/>";
        }

        if (element.HasAttributes && !element.Nodes().Any())
        {
            // just get the first attribute value
            // ex. <see cref="System.Diagnostics.Debug.Assert(bool)"/>
            // ex. <see langword="true"/>
            var attributeValue = element.FirstAttribute!.Value;

            // format the attribute value if it is an "ID string" representing a type or member
            // by stripping the prefix.
            // See https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/#id-strings
            return attributeValue switch
            {
                var s when
                    s.StartsWith("T:", StringComparison.Ordinal) ||
                    s.StartsWith("P:", StringComparison.Ordinal) ||
                    s.StartsWith("M:", StringComparison.Ordinal) ||
                    s.StartsWith("F:", StringComparison.Ordinal) ||
                    s.StartsWith("N:", StringComparison.Ordinal) ||
                    s.StartsWith("E:", StringComparison.Ordinal) => $"'{s.AsSpan(2)}'",
                _ => attributeValue
            };
        }

        return element.Value;
    }

    const string NegativeOption = @"-?";
    const string DaysAlone = @"\d{1,7}";
    const string DaysPrefixOption = @$"({DaysAlone}[\.:])?";
    const string MinutesOrSeconds = @"[0-5]?\d";
    const string HourMinute = @$"([01]?\d|2[0-3]):{MinutesOrSeconds}";
    const string HourMinuteSecond = HourMinute + $":{MinutesOrSeconds}";
    const string SecondsFractionOption = @"(\.\d{1,7})?";
    internal const string TimeSpanRegex = $"^{NegativeOption}({DaysAlone}|({DaysPrefixOption}({HourMinute}|{HourMinuteSecond}){SecondsFractionOption}))$";

    private void GenerateParsableFromString(JsonObject propertyNode, ParsableFromStringSpec parsable)
    {
        if (parsable.DisplayString == "TimeSpan")
        {
            propertyNode["type"] = "string";
            propertyNode["pattern"] = TimeSpanRegex;
        }
        else if (parsable.StringParsableTypeKind == StringParsableTypeKind.Enum)
        {
            var enumNode = new JsonArray();
            var enumTypeSpec = _typeIndex.GetTypeSpec(parsable.TypeRef);
            if (_compilation.GetBestTypeByMetadataName(enumTypeSpec.FullName) is { } enumType)
            {
                foreach (var member in enumType.MemberNames)
                {
                    if (member != WellKnownMemberNames.InstanceConstructorName && member != WellKnownMemberNames.EnumBackingFieldName)
                    {
                        enumNode.Add(member);
                    }
                }
            }
            propertyNode["enum"] = enumNode;
        }
        else if (parsable.StringParsableTypeKind == StringParsableTypeKind.Uri)
        {
            propertyNode["type"] = "string";
            propertyNode["format"] = "uri";
        }
        else if (parsable.DisplayString == "float" ||
            parsable.DisplayString == "double" ||
            parsable.DisplayString == "decimal" ||
            parsable.DisplayString == "Half")
        {
            propertyNode["type"] = new JsonArray { "number", "string" };
        }
        else if (parsable.DisplayString == "Guid")
        {
            propertyNode["type"] = "string";
            propertyNode["format"] = "uuid";
        }
        else if (parsable.DisplayString == "byte[]")
        {
            // ConfigurationBinder supports base64-encoded string
            propertyNode["oneOf"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = "^[-A-Za-z0-9+/]*={0,3}$"
                },
                new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "integer"
                    }
                }
            };
        }
        else
        {
            propertyNode["type"] = GetParsableTypeName(parsable);
        }
    }

    private static string GetParsableTypeName(ParsableFromStringSpec parsable) => parsable.DisplayString switch
    {
        "bool" => "boolean",
        "byte" => "integer",
        "sbyte" => "integer",
        "char" => "integer",
        "short" => "integer",
        "ushort" => "integer",
        "int" => "integer",
        "uint" => "integer",
        "long" => "integer",
        "ulong" => "integer",
        "Int128" => "integer",
        "UInt128" => "integer",
        "string" => "string",
        "Version" => "string",
        "DateTime" => "string",
        "DateTimeOffset" => "string",
        "DateOnly" => "string",
        "TimeOnly" => "string",
        "object" => "object",
        "CultureInfo" => "string",
        _ => throw new InvalidOperationException($"Unknown parsable type {parsable.DisplayString}")
    };

    private bool IsExcluded(JsonObject currentNode, PropertySpec property)
    {
        if (_exclusionPaths.Length > 0)
        {
            var currentPath = currentNode.GetPath()
                .Replace(".properties", "")
                .Replace(".items", "")
                .Replace(".additionalProperties", "");

            foreach (var excludedPath in _exclusionPaths)
            {
                if (excludedPath.StartsWith(currentPath) && excludedPath.EndsWith(property.ConfigurationKeyName))
                {
                    var fullPath = $"{currentPath}.{property.ConfigurationKeyName}";
                    if (excludedPath == fullPath)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string[] CreateExclusionPaths(List<string>? exclusionPaths)
    {
        if (exclusionPaths is null)
        {
            return [];
        }

        var result = new string[exclusionPaths.Count];
        for (var i = 0; i < exclusionPaths.Count; i++)
        {
            result[i] = $"$.{exclusionPaths[i].Replace(':', '.')}";
        }
        return result;
    }

    private sealed class SchemaOrderJsonNodeConverter : JsonConverter<JsonNode>
    {
        public static SchemaOrderJsonNodeConverter Instance { get; } = new SchemaOrderJsonNodeConverter();

        public override bool CanConvert(Type typeToConvert) => typeof(JsonNode).IsAssignableFrom(typeToConvert) && typeToConvert != typeof(JsonValue);

        public override void Write(Utf8JsonWriter writer, JsonNode? value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case JsonObject obj:
                    writer.WriteStartObject();
                    // ensure the children of a "properties" node are written in alphabetical order
                    IEnumerable<KeyValuePair<string, JsonNode>> properties =
                        obj.Parent is JsonObject && obj.GetPropertyName() == "properties" ?
                            obj.OrderBy(p => p.Key, StringComparer.Ordinal) :
                            obj;

                    foreach (var pair in properties)
                    {
                        writer.WritePropertyName(pair.Key);
                        Write(writer, pair.Value, options);
                    }
                    writer.WriteEndObject();
                    break;
                case JsonArray array:
                    writer.WriteStartArray();
                    foreach (var item in array)
                    {
                        Write(writer, item, options);
                    }
                    writer.WriteEndArray();
                    break;
                case null:
                    writer.WriteNullValue();
                    break;
                default: // JsonValue
                    value.WriteTo(writer, options);
                    break;
            }
        }

        public override JsonNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
