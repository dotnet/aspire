// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;

namespace ConfigurationSchemaGenerator;

internal sealed partial class ConfigSchemaEmitter(SchemaGenerationSpec spec, Compilation compilation)
{
    private readonly TypeIndex _typeIndex = new TypeIndex(spec.AllTypes);
    private readonly Compilation _compilation = compilation;
    private readonly Stack<TypeSpec> _visitedTypes = new();
    private readonly string[] _exclusionPaths = CreateExclusionPaths(spec.ExclusionPaths);

    [GeneratedRegex(@"( *)\r?\n( *)")]
    private static partial Regex Indentation();

    public string GenerateSchema()
    {
        var root = new JsonObject();
        GenerateLogCategories(root);
        root["properties"] = GenerateGraph();
        root["type"] = "object";

        var options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            // ensure the properties are ordered correctly
            Converters = { SchemaOrderJsonNodeConverter.Instance },
            // prevent known escaped characters from being \uxxxx encoded
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        return JsonSerializer.Serialize(root, options);
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
            ["logLevel"] = new JsonObject()
            {
                ["properties"] = propertiesNode
            }
        };
    }

    private JsonObject GenerateGraph()
    {
        var root = new JsonObject();
        if (spec.ConfigurationTypes.Count > 0)
        {
            if (spec.ConfigurationTypes.Count != spec.ConfigurationPaths.Count)
            {
                throw new InvalidOperationException("Ensure Types and ConfigurationPaths are the same length.");
            }

            for (var i = 0; i < spec.ConfigurationPaths.Count; i++)
            {
                var type = spec.ConfigurationTypes[i];
                var path = spec.ConfigurationPaths[i];

                GenerateProperties(root, type, path);
            }
        }

        return root;
    }

    private void GenerateProperties(JsonObject parent, TypeSpec type, string path)
    {
        var pathSegments = path.Split(':');
        var currentNode = parent;
        foreach (var segment in pathSegments)
        {
            if (currentNode[segment] is JsonObject child)
            {
                currentNode = child["properties"] as JsonObject;
                Debug.Assert(currentNode is not null, "Didn't find a 'properties' child.");
            }
            else
            {
                var propertiesNode = new JsonObject();
                currentNode[segment] = new JsonObject()
                {
                    ["type"] = "object",
                    ["properties"] = propertiesNode
                };
                currentNode = propertiesNode;
            }
        }

        GenerateProperties(currentNode, type);
        GenerateTypeDocCommentsProperties(currentNode.Parent as JsonObject, type);
    }

    private void GenerateProperties(JsonObject currentNode, TypeSpec type)
    {
        if (_visitedTypes.Contains(type))
        {
            return;
        }
        _visitedTypes.Push(type);

        if (type is ObjectSpec objectSpec)
        {
            var properties = objectSpec.Properties;
            if (properties is not null)
            {
                foreach (var property in properties)
                {
                    if (_typeIndex.ShouldBindTo(property) && !IsExcluded(currentNode, property))
                    {
                        var propertyTypeSpec = _typeIndex.GetTypeSpec(property.TypeRef);
                        var propertySymbol = GetPropertySymbol(type, property);

                        var propertyNode = new JsonObject();
                        currentNode[property.Name] = propertyNode;

                        AppendTypeNodes(propertyNode, propertyTypeSpec);

                        if (propertyTypeSpec is ComplexTypeSpec complexPropertyTypeSpec)
                        {
                            var innerPropertiesNode = new JsonObject();
                            propertyNode["properties"] = innerPropertiesNode;

                            GenerateProperties(innerPropertiesNode, complexPropertyTypeSpec);
                            if (innerPropertiesNode.Count == 0)
                            {
                                propertyNode.Remove("properties");
                            }
                        }

                        if (ShouldSkipProperty(propertyNode, property, propertyTypeSpec, propertySymbol))
                        {
                            currentNode.Remove(property.Name);
                            continue;
                        }

                        var docComment = propertySymbol?.GetDocumentationCommentXml();
                        if (!string.IsNullOrEmpty(docComment))
                        {
                            GenerateDocCommentsProperties(propertyNode, docComment);
                        }
                    }
                }
            }
        }

        _visitedTypes.Pop();
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

    private static bool ShouldSkipProperty(JsonObject propertyNode, PropertySpec property, TypeSpec propertyTypeSpec, IPropertySymbol? propertySymbol)
    {
        // skip simple properties that can't be set
        // TODO: this should allow for init properties set through the constructor. Need to figure out the correct rule here.
        if (propertyTypeSpec is not ComplexTypeSpec &&
            !property.CanSet)
        {
            return true;
        }

        // skip empty objects
        if (propertyNode.Count == 0 ||
            (propertyNode["type"] is JsonValue typeValue &&
                typeValue.TryGetValue<string>(out var typeValueString) &&
                typeValueString == "object" &&
                propertyNode["properties"] is null))
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

    private static void GenerateDocCommentsProperties(JsonObject propertyNode, string docComment)
    {
        var doc = XDocument.Parse(docComment);
        var memberRoot = doc.Element("member");
        var summary = memberRoot.Element("summary");
        if (summary is not null)
        {
            var builder = new StringBuilder();
            foreach (var node in StripXmlElements(summary))
            {
                var value = node.ToString().Trim();
                AppendSpaceIfNecessary(builder, value);
                AppendUnindentedValue(builder, value);
            }

            propertyNode["description"] = builder.ToString();
        }

        if (propertyNode["type"]?.GetValue<string>() == "boolean")
        {
            var value = memberRoot.Element("value")?.ToString();
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

    private void GenerateTypeDocCommentsProperties(JsonObject? currentNode, TypeSpec type)
    {
        if (currentNode is not null && currentNode["description"] is null)
        {
            var typeSymbol = _compilation.GetBestTypeByMetadataName(type.FullName);
            if (typeSymbol is not null)
            {
                var docComment = typeSymbol.GetDocumentationCommentXml();
                if (!string.IsNullOrEmpty(docComment))
                {
                    GenerateDocCommentsProperties(currentNode, docComment);
                }
            }
        }
    }

    private static IEnumerable<XNode> StripXmlElements(XContainer container)
    {
        return container.Nodes().SelectMany(n => n switch
        {
            XText => [n],
            XElement e => StripXmlElements(e),
            _ => Enumerable.Empty<XNode>()
        });
    }

    private static IEnumerable<XNode> StripXmlElements(XElement element)
    {
        if (element.Nodes().Any())
        {
            return StripXmlElements((XContainer)element);
        }
        else if (element.HasAttributes)
        {
            // just get the first attribute value
            // ex. <see cref="System.Diagnostics.Debug.Assert(bool)"/>
            // ex. <see langword="true"/>
            return [new XText(element.FirstAttribute.Value)];
        }

        return Enumerable.Empty<XNode>();
    }

    /// <summary>
    /// Add a space between nodes except if the next node starts with a period to end the previous sentence
    /// </summary>
    private static void AppendSpaceIfNecessary(StringBuilder builder, string value)
    {
        if (builder.Length > 0)
        {
            var nextNodeFinishesPreviousSentence =
                // previous node didn't end with a period
                builder[^1] != '.' &&
                // next node starts with a period
                (value == "." || value.StartsWith(". "));

            if (!nextNodeFinishesPreviousSentence)
            {
                builder.Append(' ');
            }
        }
    }

    internal static void AppendUnindentedValue(StringBuilder builder, string value)
    {
        var index = 0;

        foreach (var match in Indentation().EnumerateMatches(value))
        {
            if (match.Index > index)
            {
                builder.Append(value, index, match.Index - index);
            }

            builder.Append('\n');
            index = match.Index + match.Length;
        }

        var remaining = value.Length - index;

        if (remaining > 0)
        {
            builder.Append(value, index, remaining);
        }
    }

    private void AppendTypeNodes(JsonObject propertyNode, TypeSpec propertyTypeSpec)
    {
        if (propertyTypeSpec is ParsableFromStringSpec parsable)
        {
            AppendParsableFromString(propertyNode, parsable);
        }
        else if (propertyTypeSpec is ObjectSpec)
        {
            propertyNode["type"] = "object";
        }
        else if (propertyTypeSpec is EnumerableSpec)
        {
            // TODO: support enumerables correctly
            propertyNode["type"] = "object";
        }
        else if (propertyTypeSpec is NullableSpec nullable)
        {
            AppendTypeNodes(propertyNode, _typeIndex.GetTypeSpec(nullable.EffectiveTypeRef));
        }
        else if (propertyTypeSpec is UnsupportedTypeSpec unsupported &&
            unsupported.NotSupportedReason == NotSupportedReason.CollectionNotSupported)
        {
            // skip unsupported collections
        }
        else
        {
            throw new InvalidOperationException($"Unknown type {propertyTypeSpec}");
        }
    }

    private void AppendParsableFromString(JsonObject propertyNode, ParsableFromStringSpec parsable)
    {
        if (parsable.DisplayString == "TimeSpan")
        {
            propertyNode["type"] = "string";
            propertyNode["format"] = "duration";
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
        else
        {
            propertyNode["type"] = GetParsableTypeName(parsable);
        }
    }

    private static string GetParsableTypeName(ParsableFromStringSpec parsable) => parsable.DisplayString switch
    {
        "bool" => "boolean",
        "short" => "integer",
        "ushort" => "integer",
        "int" => "integer",
        "uint" => "integer",
        "long" => "integer",
        "string" => "string",
        "Version" => "string",
        _ => throw new InvalidOperationException($"Unknown parsable type {parsable.DisplayString}")
    };

    private bool IsExcluded(JsonObject currentNode, PropertySpec property)
    {
        var currentPath = currentNode.GetPath();
        foreach (var excludedPath in _exclusionPaths)
        {
            if (excludedPath.StartsWith(currentPath) && excludedPath.EndsWith(property.Name))
            {
                var fullPath = $"{currentPath}.{property.Name}";
                if (excludedPath == fullPath)
                {
                    return true;
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
            result[i] = $"$.{exclusionPaths[i].Replace(":", ".properties.", StringComparison.Ordinal)}";
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
