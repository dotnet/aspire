// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.DebugAdapter.Generator.Schema;

namespace Aspire.DebugAdapter.Generator;

/// <summary>
/// Generates C# source code from parsed schema types.
/// Outputs consolidated files: Protocol.g.cs, Requests.g.cs, Responses.g.cs, Events.g.cs, Types.g.cs
/// </summary>
public sealed class CSharpGenerator
{
    private readonly Dictionary<string, ParsedType> _types;
    private readonly string _namespace;

    // Map schema names to C# class names (to avoid property name collisions)
    private static readonly Dictionary<string, string> s_classNameMap = new()
    {
        ["Request"] = "RequestMessage",
        ["Response"] = "ResponseMessage",
        ["Event"] = "EventMessage"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpGenerator"/> class.
    /// </summary>
    /// <param name="types">The parsed types from the schema.</param>
    /// <param name="namespace">The namespace for generated code.</param>
    public CSharpGenerator(Dictionary<string, ParsedType> types, string @namespace)
    {
        _types = types;
        _namespace = @namespace;
    }

    /// <summary>
    /// Generates all source files.
    /// </summary>
    public IEnumerable<(string HintName, string Source)> Generate()
    {
        yield return ("Protocol.g.cs", GenerateProtocolFile());
        yield return ("Requests.g.cs", GenerateRequestsFile());
        yield return ("Responses.g.cs", GenerateResponsesFile());
        yield return ("Events.g.cs", GenerateEventsFile());
        yield return ("Types.g.cs", GenerateTypesFile());
        yield return ("DebugAdapterSerializableTypes.g.cs", GenerateDebugAdapterJsonContextFile());
    }

    /// <summary>
    /// Gets the C# class name for a type, applying any name mappings.
    /// </summary>
    private static string GetClassName(string schemaName)
    {
        return s_classNameMap.TryGetValue(schemaName, out var mapped) ? mapped : schemaName;
    }

    private static string GetClassName(ParsedType type) => GetClassName(type.Name);

    #region File Generation

    private string GenerateProtocolFile()
    {
        var sb = new StringBuilder();
        WriteFileHeader(sb);

        // Generate abstract base types: ProtocolMessage, RequestMessage, ResponseMessage, EventMessage
        var baseTypes = _types.Values
            .Where(t => t.Kind == TypeKind.AbstractBase)
            .OrderBy(t => GetBaseTypeOrder(t.Name))
            .ToList();

        foreach (var type in baseTypes)
        {
            GenerateAbstractBase(sb, type);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateRequestsFile()
    {
        var sb = new StringBuilder();
        WriteFileHeader(sb);

        var requests = _types.Values
            .Where(t => t.Kind == TypeKind.Request)
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in requests)
        {
            GenerateClass(sb, type);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateResponsesFile()
    {
        var sb = new StringBuilder();
        WriteFileHeader(sb);

        var responses = _types.Values
            .Where(t => t.Kind == TypeKind.Response)
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in responses)
        {
            GenerateClass(sb, type);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateEventsFile()
    {
        var sb = new StringBuilder();
        WriteFileHeader(sb);

        var events = _types.Values
            .Where(t => t.Kind == TypeKind.Event)
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in events)
        {
            GenerateClass(sb, type);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateTypesFile()
    {
        var sb = new StringBuilder();
        WriteFileHeader(sb);

        // Include Arguments, SimpleObject, Body, and Enum types
        var types = _types.Values
            .Where(t => t.Kind is TypeKind.Arguments or TypeKind.SimpleObject or TypeKind.Body or TypeKind.Enum)
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in types)
        {
            if (type.Kind == TypeKind.Enum)
            {
                GenerateEnum(sb, type);
            }
            else
            {
                GenerateClass(sb, type);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static int GetBaseTypeOrder(string name) => name switch
    {
        "ProtocolMessage" => 0,
        "Request" => 1,
        "Response" => 2,
        "Event" => 3,
        _ => 99
    };

    #endregion

    #region Type Generation

    private void GenerateEnum(StringBuilder sb, ParsedType type)
    {
        WriteXmlDoc(sb, type.Description, "");

        sb.AppendLine($"[JsonConverter(typeof(JsonStringEnumConverter<{type.Name}>))]");
        sb.AppendLine($"public enum {type.Name}");
        sb.AppendLine("{");

        if (type.EnumValues is not null)
        {
            for (int i = 0; i < type.EnumValues.Count; i++)
            {
                var value = type.EnumValues[i];
                var enumName = ToEnumMemberName(value);
                var description = type.EnumDescriptions?.ElementAtOrDefault(i);

                if (!string.IsNullOrEmpty(description))
                {
                    WriteXmlDoc(sb, description, "    ");
                }

                sb.AppendLine($"    [JsonStringEnumMemberName(\"{value}\")]");
                sb.AppendLine($"    {enumName},");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");
    }

    private void GenerateAbstractBase(StringBuilder sb, ParsedType type)
    {
        WriteXmlDoc(sb, type.Description, "", type.Title);

        // Add polymorphic attributes
        WritePolymorphicAttributes(sb, type);

        var baseClause = type.BaseTypeName is not null ? $" : {GetClassName(type.BaseTypeName)}" : "";

        // Only ProtocolMessage is abstract - the others need to be concrete for fallback deserialization
        var abstractModifier = type.Name == "ProtocolMessage" ? "abstract " : "";
        sb.AppendLine($"public {abstractModifier}partial class {GetClassName(type)}{baseClause}");
        sb.AppendLine("{");

        WriteProperties(sb, type);

        // Add constructor to intermediate types that sets the Type property
        // This ensures derived types automatically have the correct Type value
        if (type.Name == "Request")
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>Initializes a new instance of the <see cref=\"RequestMessage\"/> class.</summary>");
            sb.AppendLine("    public RequestMessage() => Type = ProtocolMessage.TypeValues.Request;");
        }
        else if (type.Name == "Response")
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>Initializes a new instance of the <see cref=\"ResponseMessage\"/> class.</summary>");
            sb.AppendLine("    public ResponseMessage() => Type = ProtocolMessage.TypeValues.Response;");
        }
        else if (type.Name == "Event")
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>Initializes a new instance of the <see cref=\"EventMessage\"/> class.</summary>");
            sb.AppendLine("    public EventMessage() => Type = ProtocolMessage.TypeValues.Event;");
        }

        // Add JsonExtensionData to ProtocolMessage for round-trip fidelity of unknown properties
        if (type.Name == "ProtocolMessage")
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>Extension data for unknown properties (enables round-trip serialization).</summary>");
            sb.AppendLine("    [JsonExtensionData]");
            sb.AppendLine("    public Dictionary<string, JsonElement>? ExtensionData { get; set; }");
        }

        // Add virtual CommandName property to RequestMessage and ResponseMessage
        // Note: For unrecognized messages that fall back to the base type, the command
        // discriminator value goes to ExtensionData, so we read it from there as fallback.
        // JsonIgnore prevents this convenience property from being serialized in the wire format.
        if (type.Name is "Request" or "Response")
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>The command name for this request/response.</summary>");
            sb.AppendLine("    [JsonIgnore]");
            sb.AppendLine("    public virtual string? CommandName => ExtensionData?.TryGetValue(\"command\", out var cmd) == true && cmd.ValueKind == JsonValueKind.String ? cmd.GetString() : null;");
        }

        // Add virtual EventName property to EventMessage
        if (type.Name == "Event")
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>The event name for this event.</summary>");
            sb.AppendLine("    [JsonIgnore]");
            sb.AppendLine("    public virtual string? EventName => ExtensionData?.TryGetValue(\"event\", out var evt) == true && evt.ValueKind == JsonValueKind.String ? evt.GetString() : null;");
        }

        sb.AppendLine("}");
    }

    private void GenerateClass(StringBuilder sb, ParsedType type)
    {
        WriteXmlDoc(sb, type.Description, "", type.Title, type.IsReverseRequest);

        var baseClause = type.BaseTypeName is not null ? $" : {GetClassName(type.BaseTypeName)}" : "";
        sb.AppendLine($"public partial class {type.Name}{baseClause}");
        sb.AppendLine("{");

        // Add override CommandName property for requests and responses
        if (type.Kind is TypeKind.Request or TypeKind.Response && type.DiscriminatorValue is not null)
        {
            sb.AppendLine($"    /// <inheritdoc />");
            sb.AppendLine($"    [JsonIgnore]");
            sb.AppendLine($"    public override string CommandName => \"{type.DiscriminatorValue}\";");
            sb.AppendLine();
        }

        // Add override EventName property for events
        if (type.Kind == TypeKind.Event && type.DiscriminatorValue is not null)
        {
            sb.AppendLine($"    /// <inheritdoc />");
            sb.AppendLine($"    [JsonIgnore]");
            sb.AppendLine($"    public override string EventName => \"{type.DiscriminatorValue}\";");
            sb.AppendLine();
        }

        WriteProperties(sb, type);

        // Add [JsonExtensionData] to ALL argument types for middleware flexibility.
        // This allows middleware to override any property via ExtensionData, which is essential
        // for modifying init-only properties like AdapterID on InitializeRequestArguments.
        if (type.Kind == TypeKind.Arguments)
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>Extension data for additional properties not mapped to typed members.</summary>");
            sb.AppendLine("    [JsonExtensionData]");
            sb.AppendLine("    public Dictionary<string, JsonElement>? ExtensionData { get; set; }");
        }

        sb.AppendLine("}");
    }

    #endregion

    #region Helpers

    private void WriteFileHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();
    }

    private void WritePolymorphicAttributes(StringBuilder sb, ParsedType type)
    {
        if (type.Name == "ProtocolMessage")
        {
            // NO polymorphism on ProtocolMessage. System.Text.Json doesn't support nested discriminators,
            // so we handle the two-level DAP polymorphism (type + command/event) by:
            // 1. Adding explicit Type property to intermediate types (RequestMessage, ResponseMessage, EventMessage)
            // 2. Using JsonPolymorphic only on those intermediate types for command/event
            // 3. Manual two-pass deserialization (peek at "type", deserialize as intermediate type)
            // See StreamMessageTransport.DeserializeWithNestedPolymorphism()
        }
        else if (type.Name == "Request")
        {
            // Request-level polymorphism for when deserializing directly as RequestMessage
            sb.AppendLine("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"command\", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType, IgnoreUnrecognizedTypeDiscriminators = true)]");
            foreach (var t in _types.Values
                .Where(t => t.Kind == TypeKind.Request && t.DiscriminatorValue is not null)
                .OrderBy(t => t.DiscriminatorValue))
            {
                sb.AppendLine($"[JsonDerivedType(typeof({t.Name}), \"{t.DiscriminatorValue}\")]");
            }
        }
        else if (type.Name == "Response")
        {
            sb.AppendLine("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"command\", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType, IgnoreUnrecognizedTypeDiscriminators = true)]");
            foreach (var t in _types.Values
                .Where(t => t.Kind == TypeKind.Response && t.DiscriminatorValue is not null)
                .OrderBy(t => t.DiscriminatorValue))
            {
                sb.AppendLine($"[JsonDerivedType(typeof({t.Name}), \"{t.DiscriminatorValue}\")]");
            }
        }
        else if (type.Name == "Event")
        {
            sb.AppendLine("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"event\", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType, IgnoreUnrecognizedTypeDiscriminators = true)]");
            foreach (var t in _types.Values
                .Where(t => t.Kind == TypeKind.Event && t.DiscriminatorValue is not null)
                .OrderBy(t => t.DiscriminatorValue))
            {
                sb.AppendLine($"[JsonDerivedType(typeof({t.Name}), \"{t.DiscriminatorValue}\")]");
            }
        }
    }

    private void WriteProperties(StringBuilder sb, ParsedType type)
    {
        foreach (var prop in type.Properties)
        {
            // Skip properties that are defined in base class with same type
            if (IsPropertyInBaseClass(type, prop.JsonName) && !HasDifferentTypeFromBase(type, prop))
            {
                continue;
            }

            // Skip discriminator properties - they conflict with [JsonPolymorphic] metadata
            // System.Text.Json reserves these property names for polymorphic type discrimination
            if (IsDiscriminatorProperty(type, prop.JsonName))
            {
                continue;
            }

            WriteXmlDoc(sb, prop.Description, "    ");

            sb.AppendLine($"    [JsonPropertyName(\"{prop.JsonName}\")]");

            var csharpType = GetCSharpType(prop.Type, prop.IsRequired);

            // Handle property naming: avoid collision with class name
            var propertyName = prop.Name;
            var className = GetClassName(type);
            if (propertyName == className)
            {
                propertyName = prop.Name + "Value";
            }

            // In abstract base classes, properties that may be overridden should be nullable
            var isInAbstractBase = type.Kind == TypeKind.AbstractBase && IsOverrideProperty(prop.JsonName);

            // Determine if we're hiding a base property
            var isHidingBase = IsPropertyInBaseClass(type, prop.JsonName);

            var requiredModifier = "";
            // Don't mark "type" as required on ProtocolMessage - it's set by intermediate type constructors
            var isTypePropertyOnBase = type.Name == "ProtocolMessage" && prop.JsonName == "type";
            if (prop.IsRequired && !IsValueType(prop.Type) && !isInAbstractBase && !isTypePropertyOnBase)
            {
                requiredModifier = "required ";
            }

            // Make abstract base override properties nullable
            if (isInAbstractBase && !IsValueType(prop.Type))
            {
                csharpType = EnsureNullable(csharpType);
            }

            var newModifier = isHidingBase ? "new " : "";

            // Type property on ProtocolMessage needs a default to suppress nullable warning
            // It's always set by intermediate type constructors (RequestMessage, ResponseMessage, EventMessage)
            var defaultValue = isTypePropertyOnBase ? " = default!;" : "";

            // Use { get; set; } for all properties to allow middleware to modify messages
            sb.AppendLine($"    public {newModifier}{requiredModifier}{csharpType} {propertyName} {{ get; set; }}{defaultValue}");

            // Generate nested constants class for _enum properties
            if (prop.SoftEnumValues is { Count: > 0 })
            {
                sb.AppendLine();
                sb.AppendLine($"    /// <summary>Known values for <see cref=\"{propertyName}\"/>.</summary>");
                sb.AppendLine($"    public static class {propertyName}Values");
                sb.AppendLine("    {");
                for (int i = 0; i < prop.SoftEnumValues.Count; i++)
                {
                    var value = prop.SoftEnumValues[i];
                    var constName = ToEnumMemberName(value);
                    var desc = prop.EnumDescriptions?.ElementAtOrDefault(i);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        WriteXmlDoc(sb, desc, "        ");
                    }
                    sb.AppendLine($"        public const string {constName} = \"{value}\";");
                }
                sb.AppendLine("    }");
            }

            sb.AppendLine();
        }
    }

    private static string EnsureNullable(string csharpType)
    {
        if (csharpType.EndsWith("?"))
        {
            return csharpType;
        }

        return csharpType + "?";
    }

    private bool HasDifferentTypeFromBase(ParsedType type, ParsedProperty prop)
    {
        var baseProp = GetPropertyFromBase(type, prop.JsonName);
        if (baseProp is null)
        {
            return false;
        }

        var derivedType = GetCSharpType(prop.Type, prop.IsRequired);
        var baseType = GetCSharpType(baseProp.Type, baseProp.IsRequired);

        return derivedType != baseType;
    }

    private ParsedProperty? GetPropertyFromBase(ParsedType type, string propJsonName)
    {
        if (type.BaseTypeName is null)
        {
            return null;
        }

        if (!_types.TryGetValue(type.BaseTypeName, out var baseType))
        {
            return null;
        }

        var baseProp = baseType.Properties.FirstOrDefault(p => p.JsonName == propJsonName);
        if (baseProp is not null)
        {
            return baseProp;
        }

        return GetPropertyFromBase(baseType, propJsonName);
    }

    private static bool IsOverrideProperty(string propJsonName)
    {
        return propJsonName is "arguments" or "body";
    }

    /// <summary>
    /// Checks if a property is a discriminator property inherited from an ancestor.
    /// Discriminator properties must be skipped on types that use [JsonPolymorphic] with that property name.
    /// ProtocolMessage has no polymorphism (type is a regular property), but Request/Response/Event
    /// use command/event as discriminators via [JsonPolymorphic].
    /// Unknown properties are captured via [JsonExtensionData] for round-trip fidelity.
    /// </summary>
    private bool IsDiscriminatorProperty(ParsedType type, string propJsonName)
    {
        // "type" on ProtocolMessage is NOT a discriminator - it's a regular property.
        // We removed polymorphism from ProtocolMessage; two-pass deserialization reads "type" manually.
        // Only command/event are discriminators (on Request/Response/Event types).
        var ownDiscriminator = type.Name switch
        {
            "Request" => "command",
            "Response" => "command",
            "Event" => "event",
            _ => (string?)null
        };

        if (ownDiscriminator == propJsonName)
        {
            return true;  // Skip - this type's discriminator
        }

        // Check if any ANCESTOR defines this as a discriminator
        if (type.BaseTypeName == null)
        {
            return false;
        }

        var currentTypeName = type.BaseTypeName;
        while (currentTypeName != null)
        {
            // Only command/event are discriminators (not type - it's a regular property on ProtocolMessage)
            var discriminatorProp = currentTypeName switch
            {
                "Request" => "command",
                "Response" => "command",
                "Event" => "event",
                _ => (string?)null
            };

            if (discriminatorProp == propJsonName)
            {
                return true;  // Skip - ancestor defines this discriminator
            }

            // Move to next ancestor
            if (_types.TryGetValue(currentTypeName, out var baseType) && baseType.BaseTypeName != null)
            {
                currentTypeName = baseType.BaseTypeName;
            }
            else
            {
                break;
            }
        }

        return false;
    }

    private bool IsPropertyInBaseClass(ParsedType type, string propJsonName)
    {
        if (type.BaseTypeName is null)
        {
            return false;
        }

        if (!_types.TryGetValue(type.BaseTypeName, out var baseType))
        {
            return false;
        }

        if (baseType.Properties.Any(p => p.JsonName == propJsonName))
        {
            return true;
        }

        return IsPropertyInBaseClass(baseType, propJsonName);
    }

    private string GetCSharpType(ParsedPropertyType propType, bool isRequired)
    {
        var nullable = !isRequired || propType.IsNullable;

        var baseType = propType.Kind switch
        {
            PropertyTypeKind.Simple => propType.TypeName ?? "object",
            PropertyTypeKind.Reference => propType.TypeName ?? "object",
            PropertyTypeKind.Array => $"List<{GetCSharpType(propType.ElementType!, true)}>",
            PropertyTypeKind.Dictionary => $"Dictionary<string, {GetCSharpType(propType.ValueType!, true)}>",
            PropertyTypeKind.Union => "object",
            PropertyTypeKind.Any => "object",
            _ => "object"
        };

        if (nullable)
        {
            return baseType + "?";
        }

        return baseType;
    }

    private static bool IsValueType(ParsedPropertyType propType)
    {
        if (propType.Kind != PropertyTypeKind.Simple)
        {
            return false;
        }

        return propType.TypeName is "int" or "double" or "bool";
    }

    private static void WriteXmlDoc(StringBuilder sb, string? description, string indent, string? title = null, bool isReverseRequest = false)
    {
        if (string.IsNullOrEmpty(description) && string.IsNullOrEmpty(title) && !isReverseRequest)
        {
            return;
        }

        sb.AppendLine($"{indent}/// <summary>");

        if (!string.IsNullOrEmpty(title))
        {
            sb.AppendLine($"{indent}/// <b>{EscapeXml(title)}</b><br/>");
        }

        if (!string.IsNullOrEmpty(description))
        {
            var lines = description.Split('\n');
            foreach (var line in lines)
            {
                sb.AppendLine($"{indent}/// {EscapeXml(line.Trim())}");
            }
        }

        sb.AppendLine($"{indent}/// </summary>");

        if (isReverseRequest)
        {
            sb.AppendLine($"{indent}/// <remarks>This is a reverse request (sent from debug adapter to client).</remarks>");
        }
    }

    private static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string ToEnumMemberName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "_";
        }

        var sb = new StringBuilder();
        bool capitalizeNext = true;

        foreach (var c in value)
        {
            if (c == ' ' || c == '-' || c == '_')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                sb.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();

        if (result.Length > 0 && char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return result;
    }

    #endregion

    #region DebugAdapterJsonContext Generation

    private string GenerateDebugAdapterJsonContextFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// This file provides helpers for AOT-compatible JSON serialization.");
        sb.AppendLine("//");
        sb.AppendLine("// For AOT support, create a partial JsonSerializerContext in your project like this:");
        sb.AppendLine("//");
        sb.AppendLine("//   [JsonSerializable(typeof(ProtocolMessage))]");
        sb.AppendLine("//   [JsonSourceGenerationOptions(");
        sb.AppendLine("//       PropertyNameCaseInsensitive = true,");
        sb.AppendLine("//       DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,");
        sb.AppendLine("//       PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,");
        sb.AppendLine("//       AllowOutOfOrderMetadataProperties = true)]");
        sb.AppendLine("//   internal partial class MyDebugAdapterJsonContext : JsonSerializerContext { }");
        sb.AppendLine("//");
        sb.AppendLine("// Then pass MyDebugAdapterJsonContext.Default to StreamMessageTransport or DebugAdapterJsonOptions.Create().");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();

        // Generate a static helper class listing all types
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Lists all Debug Adapter Protocol types for JSON serialization.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("/// <para>");
        sb.AppendLine("/// For AOT-compatible serialization, create a <see cref=\"System.Text.Json.Serialization.JsonSerializerContext\"/>");
        sb.AppendLine("/// with <c>[JsonSerializable(typeof(ProtocolMessage))]</c> attribute. The polymorphic type hierarchy will");
        sb.AppendLine("/// automatically include all derived request, response, and event types.");
        sb.AppendLine("/// </para>");
        sb.AppendLine("/// </remarks>");
        sb.AppendLine("public static class DebugAdapterSerializableTypes");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>All Debug Adapter Protocol types that are serializable.</summary>");
        sb.AppendLine("    public static readonly Type[] All = [");

        var sortedTypes = _types.Values.OrderBy(t => t.Name).ToList();
        for (int i = 0; i < sortedTypes.Count; i++)
        {
            var type = sortedTypes[i];
            var className = GetClassName(type);
            var comma = i < sortedTypes.Count - 1 ? "," : "";
            sb.AppendLine($"        typeof({className}){comma}");
        }

        sb.AppendLine("    ];");
        sb.AppendLine("}");

        return sb.ToString();
    }

    #endregion
}
