// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;

namespace ConfigurationSchemaGenerator;

internal sealed class ConfigSchemaEmitter(SourceGenerationSpec spec, Compilation compilation)
{
    private readonly TypeIndex _typeIndex = new TypeIndex(spec.AllTypes);
    private readonly Compilation _compilation = compilation;
    private readonly Stack<TypeSpec> _visitedTypes = new();

    public string GenerateSchema()
    {
        if (spec == null || spec.ConfigurationTypes.Count == 0)
        {
            return string.Empty;
        }

        var root = new JsonObject();
        GenerateLogCategories(root);
        root["properties"] = GenerateGraph();
        root["type"] = "object";

        var options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            // ensure the properties are ordered correctly
            Converters = { SchemaOrderJsonNodeConverter.Instance }
        };
        return JsonSerializer.Serialize(root, options);
    }

    private void GenerateLogCategories(JsonObject parent)
    {
        var propertiesNode = new JsonObject();
        var categories = spec.LogCategories;
        for (var i = 0; i < categories.Length; i++)
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
        if (spec.ConfigurationTypes.Count != spec.ConfigurationPaths.Length)
        {
            throw new InvalidOperationException("Ensure Types and ConfigurationPaths are the same length.");
        }

        var root = new JsonObject();
        for (var i = 0; i < spec.ConfigurationPaths.Length; i++)
        {
            var type = spec.ConfigurationTypes[i];
            var path = spec.ConfigurationPaths[i];

            GenerateProperties(root, type, path);
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
                    if (_typeIndex.ShouldBindTo(property))
                    {
                        var propertyTypeSpec = _typeIndex.GetTypeSpec(property.TypeRef);

                        IPropertySymbol? propertySymbol = null;
                        if (_compilation.GetBestTypeByMetadataName(type.FullName) is { } declaringTypeSymbol)
                        {
                            propertySymbol = declaringTypeSymbol.GetMembers(property.Name).FirstOrDefault() as IPropertySymbol;
                        }

                        var propertyNode = new JsonObject();

                        AppendTypeNodes(propertyNode, propertyTypeSpec);

                        if (propertyTypeSpec is ComplexTypeSpec complexPropertyTypeSpec)
                        {
                            var innerPropertiesNode = new JsonObject();
                            GenerateProperties(innerPropertiesNode, complexPropertyTypeSpec);
                            if (innerPropertiesNode.Count > 0)
                            {
                                propertyNode["properties"] = innerPropertiesNode;
                            }
                        }

                        if (ShouldSkipProperty(propertyNode, property, propertyTypeSpec, propertySymbol))
                        {
                            continue;
                        }

                        var docComment = propertySymbol?.GetDocumentationCommentXml();
                        if (!string.IsNullOrEmpty(docComment))
                        {
                            GenerateDocCommentsProperties(propertyNode, docComment);
                        }

                        currentNode[property.Name] = propertyNode;
                    }
                }
            }
        }

        _visitedTypes.Pop();
    }

    private static bool ShouldSkipProperty(JsonObject propertyNode, PropertySpec property, TypeSpec propertyTypeSpec, IPropertySymbol? propertySymbol)
    {
        // skip simple properties that can't be set
        if (propertyTypeSpec is not ComplexTypeSpec &&
            !property.CanSet)
        {
            return true;
        }

        // skip empty objects
        if (propertyNode["type"] is JsonValue typeValue &&
            typeValue.TryGetValue<string>(out var typeValueString) &&
            typeValueString == "object" &&
            propertyNode["properties"] is null)
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
                builder.Append(value);
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
        "int" => "integer",
        "long" => "integer",
        "string" => "string",
        "Version" => "string",
        _ => throw new InvalidOperationException($"Unknown parsable type {parsable.DisplayString}")
    };

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
