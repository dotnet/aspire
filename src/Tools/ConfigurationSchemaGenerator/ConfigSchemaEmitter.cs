// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;

namespace ConfigurationSchemaGenerator;

internal sealed class ConfigSchemaEmitter(SourceGenerationSpec spec) : EmitterBase(tabString: "  ")
{
    private readonly TypeIndex _typeIndex = new TypeIndex(spec.AllTypes);

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
        OutLn($"\"{root.Name}\": {{");
        Indent();

        WriteChildren(root);

        OutCloseBrace(includeComma: true);
    }

    private void WriteChildren(ObjectNode parent)
    {
        var sortedChildren = GetSorted(parent.Children);
        for (int i = 0; i < sortedChildren.Count; i++)
        {
            var child = sortedChildren[i];
            var includeComma = i != sortedChildren.Count - 1;

            var property = $"\"{child.Name}\":";
            if (child is SimpleNode simple)
            {
                var value = $"\"{simple.Value}\"{(includeComma ? "," : null)}";
                OutLn($"{property} {value}");
            }
            else if (child is ObjectNode objectNode)
            {
                OutLn(property + " {");
                Indent();

                WriteChildren(objectNode);

                OutCloseBrace(includeComma);
            }
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

        var root = new ObjectNode() { Name = "properties" };

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
                var propertiesNode = new ObjectNode() { Name = "properties" };
                currentNode.Children.Add(new ObjectNode()
                {
                    Name = segment,
                    Children = [new SimpleNode() { Name = "type", Value = "object" }, propertiesNode]
                });
                currentNode = propertiesNode;
            }
        }

        foreach (var property in (type as ObjectSpec).Properties)
        {
            var propertyTypeSpec = _typeIndex.GetTypeSpec(property.TypeRef);

            var propertyNode = new ObjectNode()
            {
                Name = property.Name,
                Children = [new SimpleNode() { Name = "type", Value = GetTypeName(propertyTypeSpec) }]
            };

            if (GetDescription(property) is string description)
            {
                propertyNode.Children.Add(new SimpleNode() { Name = "description", Value = description });
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

        builder.Replace("\r\n", null)
            .Replace("\n", null);

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

    private string GetTypeName(TypeSpec typeSpec) => typeSpec switch
    {
        ObjectSpec => "object",
        EnumerableSpec => "object",
        ParsableFromStringSpec parsable => GetParsableTypeName(parsable),
        NullableSpec nullable => GetTypeName(_typeIndex.GetTypeSpec(nullable.EffectiveTypeRef)),
        _ => throw new InvalidOperationException($"Unknown type {typeSpec}")
    };

    private static string GetParsableTypeName(ParsableFromStringSpec parsable)
    {
        if (parsable.StringParsableTypeKind == StringParsableTypeKind.Enum)
        {
            return "enum";
        }

        return parsable.DisplayString switch
        {
            "bool" => "boolean",
            "int" => "integer",
            "long" => "integer",
            "string" => "string",
            "Version" => "string",
            "TimeSpan" => "string",
            _ => throw new InvalidOperationException($"Unknown parsable type {parsable.DisplayString}")
        };
    }

    private abstract class SchemaNode
    {
        public string Name { get; set; }
    }

    private sealed class SimpleNode : SchemaNode
    {
        public string Value { get; set; }
    }

    private sealed class ObjectNode : SchemaNode
    {
        public List<SchemaNode> Children { get; set; } = new();

        internal SchemaNode? GetChild(string name)
        {
            return Children.FirstOrDefault(c => c.Name == name);
        }
    }
}
