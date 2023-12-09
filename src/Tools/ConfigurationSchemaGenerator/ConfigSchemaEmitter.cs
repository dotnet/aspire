// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;

namespace ConfigurationSchemaGenerator;

internal sealed class ConfigSchemaEmitter(SourceGenerationSpec spec, Compilation compilation) : EmitterBase(tabString: "  ")
{
    private readonly TypeIndex _typeIndex = new TypeIndex(spec.AllTypes);
    private readonly Compilation _compilation = compilation;

    public string GenerateSchema()
    {
        if (spec == null || spec.ConfigurationTypes.Count == 0)
        {
            return string.Empty;
        }

        OutOpenBrace();

        GenerateLogs();
        GenerateProperties();

        OutLn("\"type\": \"object\"");
        OutCloseBrace();
        return Capture();
    }

    private void GenerateLogs()
    {
        OutLn("\"definitions\": {");
        Indent();

        OutLn("\"logLevel\": {");
        Indent();

        OutLn("\"properties\": {");
        Indent();

        var categories = spec.LogCategories;
        for (int i = 0; i < categories.Length; i++)
        {
            OutLn($"\"{categories[i]}\": {{");
            Indent();
            OutLn("\"$ref\": \"#/definitions/logLevelThreshold\"");
            OutCloseBrace(includeComma: i != categories.Length - 1);
        }

        OutCloseBrace();
        OutCloseBrace();
        OutCloseBrace(includeComma: true);
    }

    private void GenerateProperties()
    {
        var root = GenerateGraph();
        WriteNode(root, includeComma: true);
    }

    private void WriteNode(SchemaNode node, bool includeComma)
    {
        if (node is ValueNode value)
        {
            OutLn($"\"{value.Value}\"{(includeComma ? "," : null)}");
            return;
        }

        var property = $"\"{node.Name}\":";
        if (node is SimpleNode simple)
        {
            var simpleValue = $"\"{simple.Value}\"{(includeComma ? "," : null)}";
            OutLn($"{property} {simpleValue}");
        }
        else if (node is ArrayNode array)
        {
            OutLn(property + " [");
            Indent();

            for (int i = 0; i < array.Values.Count; i++)
            {
                var includeArrayComma = i != array.Values.Count - 1;
                WriteNode(array.Values[i], includeArrayComma);
            }

            Unindent();
            OutLn($"]{(includeComma ? "," : null)}");
        }
        else if (node is ObjectNode objectNode)
        {
            OutLn(property + " {");
            Indent();

            var sortedChildren = GetSorted(objectNode.Children);
            for (int i = 0; i < sortedChildren.Count; i++)
            {
                var includeChildComma = i != sortedChildren.Count - 1;
                WriteNode(sortedChildren[i], includeChildComma);
            }

            OutCloseBrace(includeComma);
        }
    }

    private static List<SchemaNode> GetSorted(List<SchemaNode> children)
    {
        var sorted = children.OrderBy(c => c.Name).ToList();
        if (sorted.FindIndex(c => c.Name == "type") is int typeIndex && typeIndex > 0)
        {
            var typeChild = sorted[typeIndex];
            sorted.RemoveAt(typeIndex);
            sorted.Insert(0, typeChild);
        }
        return sorted;
    }

    private ObjectNode GenerateGraph()
    {
        if (spec.ConfigurationTypes.Count != spec.ConfigurationPaths.Length)
        {
            throw new InvalidOperationException("Ensure Types and ConfigurationPaths are the same length.");
        }

        var root = new ObjectNode("properties");

        for (int i = 0; i < spec.ConfigurationPaths.Length; i++)
        {
            var type = spec.ConfigurationTypes[i];
            var path = spec.ConfigurationPaths[i];

            GenerateChildren(root, type, path);
        }

        return root;
    }

    private void GenerateChildren(ObjectNode parent, TypeSpec type, string path)
    {
        var pathSegments = path.Split(':');
        var currentNode = parent;
        foreach (var segment in pathSegments)
        {
            if (currentNode.GetChild(segment) is ObjectNode child)
            {
                currentNode = child.GetChild("properties") as ObjectNode;
                Debug.Assert(currentNode is not null, "Didn't find a 'properties' child.");
            }
            else
            {
                var propertiesNode = new ObjectNode("properties");
                currentNode.Children.Add(new ObjectNode(segment)
                {
                    Children = [new SimpleNode("type", "object"), propertiesNode]
                });
                currentNode = propertiesNode;
            }
        }

        foreach (var property in (type as ObjectSpec).Properties)
        {
            var propertyTypeSpec = _typeIndex.GetTypeSpec(property.TypeRef);

            var propertyNode = new ObjectNode(property.Name);

            AppendTypeNodes(propertyNode, propertyTypeSpec);

            if (GetDescription(property) is string description)
            {
                propertyNode.Children.Add(new SimpleNode("description", description));
            }

            // skip empty objects
            if (propertyNode.GetChild("type") is SimpleNode { Value: "object" } && propertyNode.GetChild("properties") is null)
            {
                continue;
            }

            currentNode.Children.Add(propertyNode);
        }
    }

    private static string? GetDescription(PropertySpec property)
    {
        string docComment = property.DocumentationCommentXml;
        if (string.IsNullOrEmpty(docComment))
        {
            return null;
        }

        var doc = XDocument.Parse(docComment);
        var summary = doc.Element("member").Element("summary");
        if (summary is null)
        {
            return null;
        }

        var builder = new StringBuilder();
        foreach (var node in StripXmlElements(summary))
        {
            var value = node.ToString().Trim();
            AppendSpaceIfNecessary(builder, value);
            builder.Append(value);
        }

        return JsonEncodedText.Encode(builder.ToString()).Value;
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

    private void AppendTypeNodes(ObjectNode propertyNode, TypeSpec propertyTypeSpec)
    {
        if (propertyTypeSpec is ParsableFromStringSpec parsable)
        {
            AppendParsableFromString(propertyNode, parsable);
        }
        else if (propertyTypeSpec is ObjectSpec)
        {
            propertyNode.Children.Add(new SimpleNode("type", "object"));
        }
        else if (propertyTypeSpec is EnumerableSpec)
        {
            // TODO: support enumerables correctly
            propertyNode.Children.Add(new SimpleNode("type", "object"));
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

    private void AppendParsableFromString(ObjectNode propertyNode, ParsableFromStringSpec parsable)
    {
        if (parsable.DisplayString == "TimeSpan")
        {
            propertyNode.Children.Add(new SimpleNode("type", "string"));
            propertyNode.Children.Add(new SimpleNode("format", "duration"));
        }
        else if (parsable.StringParsableTypeKind == StringParsableTypeKind.Enum)
        {
            var enumNode = new ArrayNode("enum");
            var enumTypeSpec = _typeIndex.GetTypeSpec(parsable.TypeRef);
            if (_compilation.GetBestTypeByMetadataName(enumTypeSpec.FullName) is { } enumType)
            {
                foreach (var member in enumType.MemberNames)
                {
                    if (member != WellKnownMemberNames.InstanceConstructorName && member != WellKnownMemberNames.EnumBackingFieldName)
                    {
                        enumNode.Values.Add(new ValueNode(member));
                    }
                }
            }
            propertyNode.Children.Add(enumNode);
        }
        else
        {
            propertyNode.Children.Add(new SimpleNode("type", GetParsableTypeName(parsable)));
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

    private abstract class SchemaNode(string name)
    {
        public string Name { get; set; } = name;
    }

    private sealed class SimpleNode(string name, string value) : SchemaNode(name)
    {
        public string Value { get; set; } = value;
    }

    private sealed class ObjectNode(string name) : SchemaNode(name)
    {
        public List<SchemaNode> Children { get; set; } = new();

        internal SchemaNode? GetChild(string name)
        {
            return Children.FirstOrDefault(c => c.Name == name);
        }
    }

    private sealed class ArrayNode(string name) : SchemaNode(name)
    {
        public List<SchemaNode> Values { get; set; } = new();
    }

    private sealed class ValueNode(string value) : SchemaNode(string.Empty)
    {
        public string Value { get; set; } = value;
    }
}
