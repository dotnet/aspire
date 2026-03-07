// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

// These types intentionally mirror the ATS metadata models in Aspire.Hosting.Ats.
// RemoteHost is shipped by the CLI, while the app restores Aspire.Hosting and its extensions.
// To keep the JSON-RPC/process boundary isolated, RemoteHost reflects over Hosting-side ATS
// objects and projects them into these local shapes rather than taking a static assembly dependency.
namespace Aspire.Hosting.RemoteHost.Ats;

internal enum AtsTypeCategory
{
    Primitive,
    Enum,
    Handle,
    Dto,
    Callback,
    Array,
    List,
    Dict,
    Union,
    Unknown
}

internal enum AtsCapabilityKind
{
    Method,
    PropertyGetter,
    PropertySetter,
    InstanceMethod
}

internal enum AtsDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

internal sealed class AtsTypeRef
{
    public required string TypeId { get; init; }
    public AtsTypeCategory Category { get; set; }
    public bool IsInterface { get; init; }
    public AtsTypeRef? ElementType { get; init; }
    public AtsTypeRef? KeyType { get; init; }
    public AtsTypeRef? ValueType { get; init; }
    public bool IsReadOnly { get; init; }
    public List<AtsTypeRef>? UnionTypes { get; init; }
}

internal sealed class AtsDiagnostic
{
    public AtsDiagnosticSeverity Severity { get; init; }
    public required string Message { get; init; }
    public string? Location { get; init; }

    public static AtsDiagnostic Error(string message, string? location = null) =>
        new() { Severity = AtsDiagnosticSeverity.Error, Message = message, Location = location };

    public static AtsDiagnostic Warning(string message, string? location = null) =>
        new() { Severity = AtsDiagnosticSeverity.Warning, Message = message, Location = location };

    public static AtsDiagnostic Info(string message, string? location = null) =>
        new() { Severity = AtsDiagnosticSeverity.Info, Message = message, Location = location };
}

internal sealed class AtsCapabilityInfo
{
    public required string CapabilityId { get; init; }
    public required string MethodName { get; init; }
    public string? OwningTypeName { get; init; }
    public string QualifiedMethodName => OwningTypeName is not null ? $"{OwningTypeName}.{MethodName}" : MethodName;
    public string? Description { get; init; }
    public required List<AtsParameterInfo> Parameters { get; init; }
    public required AtsTypeRef ReturnType { get; init; }
    public string? TargetTypeId { get; init; }
    public AtsTypeRef? TargetType { get; init; }
    public string? TargetParameterName { get; init; }
    public List<AtsTypeRef> ExpandedTargetTypes { get; set; } = [];
    public bool ReturnsBuilder { get; init; }
    public AtsCapabilityKind CapabilityKind { get; init; }
    public string? SourceLocation { get; init; }
}

internal sealed class AtsParameterInfo
{
    public required string Name { get; init; }
    public AtsTypeRef? Type { get; init; }
    public bool IsOptional { get; init; }
    public bool IsNullable { get; init; }
    public bool IsCallback { get; init; }
    public List<AtsCallbackParameterInfo>? CallbackParameters { get; init; }
    public AtsTypeRef? CallbackReturnType { get; init; }
    public object? DefaultValue { get; init; }
}

internal sealed class AtsCallbackParameterInfo
{
    public required string Name { get; init; }
    public required AtsTypeRef Type { get; init; }
}

internal sealed class AtsTypeInfo
{
    public required string AtsTypeId { get; init; }
    public bool IsInterface { get; init; }
    public List<AtsTypeRef> ImplementedInterfaces { get; init; } = [];
    public List<AtsTypeRef> BaseTypeHierarchy { get; init; } = [];
    public bool HasExposeProperties { get; init; }
    public bool HasExposeMethods { get; init; }
}

internal sealed class AtsDtoTypeInfo
{
    public required string TypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required List<AtsDtoPropertyInfo> Properties { get; init; }
}

internal sealed class AtsDtoPropertyInfo
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required AtsTypeRef Type { get; init; }
    public bool IsOptional { get; init; }
}

internal sealed class AtsEnumTypeInfo
{
    public required string TypeId { get; init; }
    public required string Name { get; init; }
    public required List<string> Values { get; init; }
}

internal sealed class AtsContext
{
    public required List<AtsCapabilityInfo> Capabilities { get; init; }
    public required List<AtsTypeInfo> HandleTypes { get; init; }
    public required List<AtsDtoTypeInfo> DtoTypes { get; init; }
    public required List<AtsEnumTypeInfo> EnumTypes { get; init; }
    public List<AtsDiagnostic> Diagnostics { get; init; } = [];
}

internal sealed class AtsError
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("capability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Capability { get; init; }

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AtsErrorDetails? Details { get; init; }

    public JsonObject ToJsonObject()
    {
        var obj = new JsonObject
        {
            ["code"] = Code,
            ["message"] = Message
        };

        if (Capability is not null)
        {
            obj["capability"] = Capability;
        }

        if (Details is not null)
        {
            obj["details"] = Details.ToJsonObject();
        }

        return obj;
    }
}

internal sealed class AtsErrorDetails
{
    [JsonPropertyName("parameter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Parameter { get; init; }

    [JsonPropertyName("expected")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Expected { get; init; }

    [JsonPropertyName("actual")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Actual { get; init; }

    public JsonObject ToJsonObject()
    {
        var obj = new JsonObject();

        if (Parameter is not null)
        {
            obj["parameter"] = Parameter;
        }

        if (Expected is not null)
        {
            obj["expected"] = Expected;
        }

        if (Actual is not null)
        {
            obj["actual"] = Actual;
        }

        return obj;
    }
}

internal static class AtsErrorCodes
{
    public const string CapabilityNotFound = "CAPABILITY_NOT_FOUND";
    public const string HandleNotFound = "HANDLE_NOT_FOUND";
    public const string TypeMismatch = "TYPE_MISMATCH";
    public const string InvalidArgument = "INVALID_ARGUMENT";
    public const string ArgumentOutOfRange = "ARGUMENT_OUT_OF_RANGE";
    public const string CallbackError = "CALLBACK_ERROR";
    public const string InternalError = "INTERNAL_ERROR";
}

internal sealed class ScaffoldRequest
{
    public required string TargetPath { get; init; }
    public string? ProjectName { get; init; }
    public int? PortSeed { get; init; }
}

internal sealed class DetectionResult
{
    public bool IsValid { get; init; }
    public string? Language { get; init; }
    public string? AppHostFile { get; init; }

    public static DetectionResult NotFound => new() { IsValid = false };

    public static DetectionResult Found(string language, string appHostFile) => new()
    {
        IsValid = true,
        Language = language,
        AppHostFile = appHostFile
    };
}

internal sealed class RuntimeSpec
{
    public required string Language { get; init; }
    public required string DisplayName { get; init; }
    public required string CodeGenLanguage { get; init; }
    public required string[] DetectionPatterns { get; init; }
    public CommandSpec? InstallDependencies { get; init; }
    public required CommandSpec Execute { get; init; }
    public CommandSpec? WatchExecute { get; init; }
    public CommandSpec? PublishExecute { get; init; }
}

internal sealed class CommandSpec
{
    public required string Command { get; init; }
    public required string[] Args { get; init; }
    public Dictionary<string, string>? EnvironmentVariables { get; init; }
}
