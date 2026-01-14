// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.Ats;

namespace Aspire.Hosting.CodeGeneration.Python;

/// <summary>
/// Represents a builder class to be generated with its capabilities.
/// Internal type replacing BuilderModel - used only within the generator.
/// </summary>
internal sealed class BuilderModel
{
    public required string TypeId { get; init; }
    public required string BuilderClassName { get; init; }
    public required List<AtsCapabilityInfo> Capabilities { get; init; }
    public bool IsInterface { get; init; }
}

/// <summary>
/// Generates a Python SDK using the ATS (Aspire Type System) capability-based API.
/// Produces typed builder classes with fluent methods that use invoke_capability().
/// </summary>
/// <remarks>
/// <para>
/// <b>ATS to Python Type Mapping</b>
/// </para>
/// <para>
/// The generator maps ATS types to Python types according to the following rules:
/// </para>
/// <para>
/// <b>Primitive Types:</b>
/// <list type="table">
///   <listheader>
///     <term>ATS Type</term>
///     <description>Python Type</description>
///   </listheader>
///   <item><term><c>string</c></term><description><c>str</c></description></item>
///   <item><term><c>number</c></term><description><c>int | float</c></description></item>
///   <item><term><c>boolean</c></term><description><c>bool</c></description></item>
///   <item><term><c>any</c></term><description><c>Any</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Handle Types:</b>
/// Type IDs use the format <c>{AssemblyName}/{TypeName}</c>.
/// <list type="table">
///   <listheader>
///     <term>ATS Type ID</term>
///     <description>Python Type</description>
///   </listheader>
///   <item><term><c>Aspire.Hosting/IDistributedApplicationBuilder</c></term><description><c>BuilderHandle</c></description></item>
///   <item><term><c>Aspire.Hosting/DistributedApplication</c></term><description><c>ApplicationHandle</c></description></item>
///   <item><term><c>Aspire.Hosting/DistributedApplicationExecutionContext</c></term><description><c>ExecutionContextHandle</c></description></item>
///   <item><term><c>Aspire.Hosting.Redis/RedisResource</c></term><description><c>RedisResourceBuilderHandle</c></description></item>
///   <item><term><c>Aspire.Hosting/ContainerResource</c></term><description><c>ContainerResourceBuilderHandle</c></description></item>
///   <item><term><c>Aspire.Hosting.ApplicationModel/IResource</c></term><description><c>IResourceHandle</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Handle Type Naming Rules:</b>
/// <list type="bullet">
///   <item><description>Core types: Use type name + "Handle"</description></item>
///   <item><description>Interface types: Use interface name + "Handle" (keep the I prefix)</description></item>
///   <item><description>Resource types: Use type name + "BuilderHandle"</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Special Types:</b>
/// <list type="table">
///   <listheader>
///     <term>ATS Type</term>
///     <description>Python Type</description>
///   </listheader>
///   <item><term><c>callback</c></term><description><c>Callable[[EnvironmentContextHandle], Awaitable[None]]</c></description></item>
///   <item><term><c>T[]</c> (array)</term><description><c>list[T]</c> (list of mapped type)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Builder Class Generation:</b>
/// <list type="bullet">
///   <item><description><c>Aspire.Hosting.Redis/RedisResource</c> → <c>RedisResourceBuilder</c> class</description></item>
///   <item><description><c>Aspire.Hosting.ApplicationModel/IResource</c> → <c>ResourceBuilderBase</c> abstract class (interface types get "BuilderBase" suffix)</description></item>
///   <item><description>Concrete builders extend interface builders based on type hierarchy</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Method Naming:</b>
/// <list type="bullet">
///   <item><description>Derived from capability ID: <c>Aspire.Hosting.Redis/addRedis</c> → <c>add_redis</c></description></item>
///   <item><description>Can be overridden via <c>[AspireExport(MethodName = "...")]</c></description></item>
///   <item><description>Python uses snake_case converted from camelCase</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class AtsPythonCodeGenerator : ICodeGenerator
{
    private TextWriter _writer = null!;

    // Mapping of typeId -> wrapper class name for all generated wrapper types
    // Used to resolve parameter types to wrapper classes instead of handle types
    private readonly Dictionary<string, string> _wrapperClassNames = new(StringComparer.Ordinal);

    // Set of generated type alias names to avoid duplicates
    // private readonly HashSet<string> _generatedTypeAliases = new(StringComparer.Ordinal);

    // Mapping of enum type IDs to Python enum names
    private readonly Dictionary<string, string> _enumTypeNames = new(StringComparer.Ordinal);

    /// <summary>
    /// Maps an AtsTypeRef to a Python type using category-based dispatch.
    /// This is the preferred method - uses type metadata rather than string parsing.
    /// </summary>
    private string MapTypeRefToPython(AtsTypeRef? typeRef)
    {
        if (typeRef == null)
        {
            return "Any";
        }

        // Check for wrapper class first (handles custom types like ReferenceExpression)
        if (_wrapperClassNames.TryGetValue(typeRef.TypeId, out var wrapperClassName))
        {
            return wrapperClassName;
        }

        return typeRef.Category switch
        {
            AtsTypeCategory.Primitive => MapPrimitiveType(typeRef.TypeId),
            AtsTypeCategory.Enum => MapEnumType(typeRef.TypeId),
            AtsTypeCategory.Handle => GetWrapperOrHandleName(typeRef.TypeId),
            AtsTypeCategory.Dto => GetDtoClassName(typeRef.TypeId),
            AtsTypeCategory.Callback => "Callable",  // Callbacks handled separately with full signature
            AtsTypeCategory.Array => $"Iterable[{MapTypeRefToPython(typeRef.ElementType)}]",
            AtsTypeCategory.List => $"AspireList[{MapTypeRefToPython(typeRef.ElementType)}]",
            AtsTypeCategory.Dict => typeRef.IsReadOnly
                ? $"Mapping[{MapTypeRefToPython(typeRef.KeyType)}, {MapTypeRefToPython(typeRef.ValueType)}]"
                : $"AspireDict[{MapTypeRefToPython(typeRef.KeyType)}, {MapTypeRefToPython(typeRef.ValueType)}]",
            AtsTypeCategory.Union => MapUnionTypeToPython(typeRef),
            AtsTypeCategory.Unknown => "Any",  // Unknown types use 'Any' since they're not in the ATS universe
            _ => "Any"  // Fallback for any unhandled categories
        };
    }

    /// <summary>
    /// Maps primitive type IDs to Python types.
    /// </summary>
    private static string MapPrimitiveType(string typeId) => typeId switch
    {
        AtsConstants.String or AtsConstants.Char => "str",
        AtsConstants.Number => "int | float",
        AtsConstants.Boolean => "bool",
        AtsConstants.Void => "None",
        AtsConstants.Any => "Any",
        AtsConstants.DateTime or AtsConstants.DateTimeOffset or
        AtsConstants.DateOnly or AtsConstants.TimeOnly => "str",
        AtsConstants.TimeSpan => "float",
        AtsConstants.Guid or AtsConstants.Uri => "str",
        _ => typeId
    };

    /// <summary>
    /// Maps an enum type ID to the generated Python enum name.
    /// Throws if the enum type wasn't collected during scanning.
    /// </summary>
    private string MapEnumType(string typeId)
    {
        if (!_enumTypeNames.TryGetValue(typeId, out var enumName))
        {
            throw new InvalidOperationException(
                $"Enum type '{typeId}' was not found in the scanned enum types. " +
                $"This indicates the enum type was not discovered during assembly scanning.");
        }
        return enumName;
    }

    /// <summary>
    /// Maps a union type to Python union syntax (T1 | T2 | ...).
    /// </summary>
    private string MapUnionTypeToPython(AtsTypeRef typeRef)
    {
        if (typeRef.UnionTypes == null || typeRef.UnionTypes.Count == 0)
        {
            return "Any";
        }

        var memberTypes = typeRef.UnionTypes
            .Select(MapTypeRefToPython)
            .Distinct();

        return string.Join(" | ", memberTypes);
    }

    /// <summary>
    /// Gets the wrapper class name or handle type name for a handle type ID.
    /// Prefers wrapper class if one exists, otherwise generates a handle type name.
    /// </summary>
    private string GetWrapperOrHandleName(string typeId)
    {
        if (_wrapperClassNames.TryGetValue(typeId, out var wrapperClassName))
        {
            return wrapperClassName;
        }
        return GetHandleTypeName(typeId);
    }

    /// <summary>
    /// Gets a Python class name for a DTO type.
    /// </summary>
    private static string GetDtoClassName(string typeId)
    {
        // Extract simple type name and use as class name
        var simpleTypeName = ExtractSimpleTypeName(typeId);
        return simpleTypeName;
    }

    /// <summary>
    /// Maps a parameter to its Python type, handling callbacks specially.
    /// For interface handle types, uses ResourceBuilderBase as the parameter type.
    /// </summary>
    private string MapParameterToPython(AtsParameterInfo param)
    {
        if (param.IsCallback)
        {
            return GenerateCallbackTypeSignature(param.CallbackParameters, param.CallbackReturnType);
        }

        var baseType = MapTypeRefToPython(param.Type);

        // For interface handle types, use ResourceBuilderBase as the parameter type
        // All wrapper classes extend ResourceBuilderBase
        if (IsInterfaceHandleType(param.Type))
        {
            return "ResourceBuilderBase";
        }

        return baseType;
    }

    /// <summary>
    /// Checks if a type reference is an interface handle type.
    /// Interface handles need base class types to accept wrapper classes.
    /// </summary>
    private static bool IsInterfaceHandleType(AtsTypeRef? typeRef)
    {
        if (typeRef == null)
        {
            return false;
        }
        return typeRef.Category == AtsTypeCategory.Handle && typeRef.IsInterface;
    }

    /// <inheritdoc />
    public string Language => "Python";

    /// <inheritdoc />
    public Dictionary<string, string> GenerateDistributedApplication(AtsContext context)
    {
        var files = new Dictionary<string, string>();

        // Add embedded resource files (transport.py, base.py, requirements.txt)
        files["_transport.py"] = GetEmbeddedResource("_transport.py");
        files["_base.py"] = GetEmbeddedResource("_base.py");

        // Generate the capability-based aspire.py SDK
        files["__init__.py"] = GenerateAspireSdk(context);

        return files;
    }

    private static string GetEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Aspire.Hosting.CodeGeneration.Python.Resources.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets a valid Python method name from a capability method name.
    /// Converts camelCase to snake_case.
    /// Handles dotted names like "EnvironmentContext.resource" by extracting just the final part.
    /// </summary>
    private static string GetPythonMethodName(string methodName)
    {
        // Extract last component if dotted (e.g., "Type.method" -> "method")
        var lastDot = methodName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            methodName = methodName[(lastDot + 1)..];
        }

        // Convert camelCase to snake_case
        return ToSnakeCase(methodName);
    }

    /// <summary>
    /// Generates the aspire.py SDK file with capability-based API.
    /// </summary>
    private string GenerateAspireSdk(AtsContext context)
    {
        using var writer = new StringWriter();
        _writer = writer;

        WriteLine("# aspire.py - Generated Aspire SDK for Python");
        WriteLine("# This file is auto-generated. Do not edit manually.");
        WriteLine();
        WriteLine("from __future__ import annotations");
        WriteLine();
        WriteLine("import os");
        WriteLine("import hashlib");
        WriteLine("import signal");
        WriteLine("import sys");
        WriteLine("import time");
        WriteLine("import logging");
        WriteLine("import subprocess");
        WriteLine("from abc import ABC, abstractmethod");
        WriteLine("from re import compile");
        WriteLine("from contextlib import contextmanager");
        WriteLine("from dataclasses import dataclass");
        WriteLine("from base64 import b64encode");
        WriteLine("from warnings import warn");
        WriteLine("from pathlib import Path");
        WriteLine("from collections.abc import Iterable, Mapping, Callable");
        WriteLine("from typing import (");
        WriteLine("    Any, Unpack, Self, Literal, TypedDict, Annotated, Required, Awaitable, Generic, TypeVar,");
        WriteLine("    TYPE_CHECKING, get_origin, get_args, get_type_hints, cast, overload, runtime_checkable");
        WriteLine(")");
        WriteLine();
        WriteLine("from ._base import (");
        WriteLine("    Handle,");
        WriteLine("    AspireClient,");
        WriteLine("    ResourceBuilderBase,");
        WriteLine("    ReferenceExpression,");
        WriteLine("    ref_expr,");
        WriteLine("    AspireList,");
        WriteLine("    AspireDict,");
        WriteLine("    register_callback,");
        WriteLine("    unregister_callback,");
        WriteLine("    CapabilityError,");
        WriteLine(")");
        WriteLine();
        WriteLine("if TYPE_CHECKING:");
        WriteLine("    from ._transport import MarshalledHandle");
        WriteLine();
        WriteLine("_VALID_NAME = compile(r'^[a-zA-Z0-9-]+$')");
        WriteLine("_LOG = logging.getLogger(\"aspyre\")");
        WriteLine();
        WriteLine();
        WriteLine("def _valid_var_name(name: str) -> str:");
        WriteLine("    if not _VALID_NAME.match(name):");
        WriteLine("        raise ValueError(f\"Invalid name '{name}'. Only alphanumeric characters and hyphens are allowed.\")");
        WriteLine("    return name.replace(\"-\", \"_\")");
        WriteLine();
        WriteLine();
        WriteLine("def _validate_type(arg: Any, expected_type: Any) -> bool:");
        WriteLine("    if get_origin(expected_type) is Iterable:");
        WriteLine("        if isinstance(arg, str):");
        WriteLine("            return False");
        WriteLine("        item_type = get_args(expected_type)[0]");
        WriteLine("        if not isinstance(arg, Iterable):");
        WriteLine("            return False");
        WriteLine("        for item in arg:");
        WriteLine("            if not _validate_type(item, item_type):");
        WriteLine("                return False");
        WriteLine("    elif get_origin(expected_type) is Mapping:");
        WriteLine("        key_type, value_type = get_args(expected_type)");
        WriteLine("        if not isinstance(arg, Mapping):");
        WriteLine("            return False");
        WriteLine("        for key, value in arg.items():");
        WriteLine("            if not _validate_type(key, key_type):");
        WriteLine("                return False");
        WriteLine("            if not _validate_type(value, value_type):");
        WriteLine("                return False");
        WriteLine("    elif get_origin(expected_type) is Callable:");
        WriteLine("        return callable(arg)");
        WriteLine("    elif isinstance(arg, (tuple, Mapping)):");
        WriteLine("        return False");
        WriteLine("    elif get_origin(expected_type) is Literal:");
        WriteLine("        if arg not in get_args(expected_type):");
        WriteLine("            return False");
        WriteLine("    elif expected_type is None:");
        WriteLine("        if arg is not None:");
        WriteLine("            return False");
        WriteLine("    elif subtypes := get_args(expected_type):");
        WriteLine("        # This is probably a Union type");
        WriteLine("        return any([_validate_type(arg, subtype) for subtype in subtypes])");
        WriteLine("    elif not isinstance(arg, expected_type):");
        WriteLine("        return False");
        WriteLine("    return True");
        WriteLine();
        WriteLine();
        WriteLine("def _validate_tuple_types(args: Any, arg_types: tuple[Any, ...]) -> bool:");
        WriteLine("    if not isinstance(args, tuple):");
        WriteLine("        return False");
        WriteLine("    if len(args) != len(arg_types):");
        WriteLine("        return False");
        WriteLine("    for arg, expected_type in zip(args, arg_types):");
        WriteLine("        if not _validate_type(arg, expected_type):");
        WriteLine("            return False");
        WriteLine("    return True");
        WriteLine();
        WriteLine();
        WriteLine("def _validate_dict_types(args: Any, arg_types: Any) -> bool:");
        WriteLine("    if not isinstance(args, Mapping):");
        WriteLine("        return False");
        WriteLine("    type_hints = get_type_hints(arg_types, include_extras=True)");
        WriteLine("    for key, expected_type in type_hints.items():");
        WriteLine("        if get_origin(expected_type) is Required:");
        WriteLine("            expected_type = get_args(expected_type)[0]");
        WriteLine("            if key not in args:");
        WriteLine("                return False");
        WriteLine("        if key not in args:");
        WriteLine("            continue");
        WriteLine("        value = args[key]");
        WriteLine("        if not _validate_type(value, expected_type):");
        WriteLine("            return False");
        WriteLine("    return True");
        WriteLine();
        WriteLine();
        WriteLine("def _default(value: Any, default: Any) -> Any:");
        WriteLine("    if value is None:");
        WriteLine("        return default");
        WriteLine("    return value");
        WriteLine();
        WriteLine();
        WriteLine("@dataclass");
        WriteLine("class Warnings:");
        WriteLine("    experimental: str | None");
        WriteLine();
        WriteLine();
        WriteLine("class AspyreExperimentalWarning(Warning):");
        WriteLine("    '''Custom warning for experimental features in Aspire.'''");
        WriteLine();
        WriteLine();
        WriteLine("class AspyreOperationError(Exception):");
        WriteLine("    '''Error in constructing an Aspire resource.'''");
        WriteLine();
        WriteLine();
        WriteLine("@contextmanager");
        WriteLine("def _experimental(app: DistributedApplication, arg_name: str, func_or_cls: str | type, code: str):");
        WriteLine("    if isinstance(func_or_cls, str):");
        WriteLine("        warn(");
        WriteLine("            f\"The '{arg_name}' option in '{func_or_cls}' is for evaluation purposes only and is subject \"");
        WriteLine("            f\"to change or removal in future updates. (Code: {code})\",");
        WriteLine("            category=AspyreExperimentalWarning,");
        WriteLine("        )");
        WriteLine("        app.send(\"pragma\", {\"type\": \"warning disable\", \"value\": code})");
        WriteLine("        yield");
        WriteLine("        app.send(\"pragma\", {\"type\": \"warning restore\", \"value\": code})");
        WriteLine("    else:");
        WriteLine("        warn(");
        WriteLine("            f\"The '{arg_name}' method of '{func_or_cls.__name__}' is for evaluation purposes only and is subject \"");
        WriteLine("            f\"to change or removal in future updates. (Code: {code})\",");
        WriteLine("            category=AspyreExperimentalWarning,");
        WriteLine("        )");
        WriteLine("        app.send(\"pragma\", {\"type\": \"warning disable\", \"value\": code})");
        WriteLine("        yield");
        WriteLine("        app.send(\"pragma\", {\"type\": \"warning restore\", \"value\": code})");
        WriteLine();
        WriteLine();
        WriteLine("@contextmanager");
        WriteLine("def _check_warnings(app: DistributedApplication, kwargs: Mapping[str, Any], annotations: Any, func_name: str):");
        WriteLine("    type_hints = get_type_hints(annotations, include_extras=True)");
        WriteLine("    for key in kwargs.keys():");
        WriteLine("        if get_origin(type_hint := type_hints.get(key)) is Annotated:");
        WriteLine("            annotated_warnings = cast(Warnings, get_args(type_hint)[1])");
        WriteLine("            if annotated_warnings.experimental:");
        WriteLine("                warn(");
        WriteLine("                    f\"The '{key}' option in '{func_name}' is for evaluation purposes only and is subject to change\"");
        WriteLine("                    f\"or removal in future updates. (Code: {annotated_warnings.experimental})\",");
        WriteLine("                    category=AspyreExperimentalWarning,");
        WriteLine("                )");
        WriteLine("                app.send(\"pragma\", {\"type\": \"warning disable\", \"value\": annotated_warnings.experimental})");
        WriteLine("                yield");
        WriteLine("                app.send(\"pragma\", {\"type\": \"warning restore\", \"value\": annotated_warnings.experimental})");
        WriteLine("                return");
        WriteLine("    yield");
        
        // Scan the context to:
        // 1. Discover all builder types and their capabilities
        // 2. Collect enum types
        // 3. Collect DTO types
        var buildersByTypeId = new Dictionary<string, BuilderModel>(StringComparer.Ordinal);
        var enumTypes = new List<AtsEnumTypeInfo>();
        var dtoTypes = new List<AtsDtoTypeInfo>();

        // Get enum types and DTO types from context
        foreach (var enumType in context.EnumTypes)
        {
            if (!_enumTypeNames.ContainsKey(enumType.TypeId))
            {
                _enumTypeNames[enumType.TypeId] = ExtractSimpleTypeName(enumType.TypeId);
                enumTypes.Add(enumType);
            }
        }

        foreach (var dtoType in context.DtoTypes)
        {
            dtoTypes.Add(dtoType);
        }

        foreach (var capability in context.Capabilities)
        {
            // Determine the type this capability belongs to
            var typeId = capability.TargetType?.TypeId;
            if (string.IsNullOrEmpty(typeId))
            {
                // Static method - skip for now
                continue;
            }
            if (capability.TargetType?.Category != AtsTypeCategory.Handle)
            {
                // Not a handle type - skip
                continue;
            }

            // Get or create builder model for this type
            if (!buildersByTypeId.TryGetValue(typeId, out var builder))
            {
                var builderClassName = GetBuilderClassName(typeId);
                builder = new BuilderModel
                {
                    TypeId = typeId,
                    BuilderClassName = builderClassName,
                    Capabilities = new List<AtsCapabilityInfo>(),
                    IsInterface = capability.TargetType?.IsInterface ?? false
                };
                buildersByTypeId[typeId] = builder;
                _wrapperClassNames[typeId] = builderClassName;
            }

            builder.Capabilities.Add(capability);
        }

        // Generate handle type aliases
        // var allTypeIds = new HashSet<string>(StringComparer.Ordinal);
        // foreach (var capability in context.Capabilities)
        // {
        //     CollectTypeIds(capability, allTypeIds);
        // }
        // GenerateHandleTypeAliases(allTypeIds);

        // Generate enum types
        GenerateEnumTypes(enumTypes);

        // Generate DTO classes
        GenerateDtoClasses(dtoTypes);

        WriteLine();
        WriteLine("# ============================================================================");
        WriteLine("# Builder Classes");
        WriteLine("# ============================================================================");
        WriteLine();

        // Sort builders by dependency order (interfaces before concrete types)
        var sortedBuilders = TopologicalSortBuilders(buildersByTypeId.Values.ToList());

        foreach (var builder in sortedBuilders)
        {
            if (builder.IsInterface)
            {
                GenerateBuilderClass(builder);
                WriteLine();
            }
            else
            {
                GenerateBuilderClass(builder);
                WriteLine();
            }
        }

        return writer.ToString();
    }

    private void WriteLine(string? text = null)
    {
        if (text != null)
        {
            _writer.WriteLine(text);
        }
        else
        {
            _writer.WriteLine();
        }
    }

    private void Write(string text)
    {
        _writer.Write(text);
    }

    // private void GenerateHandleTypeAliases(HashSet<string> typeIds)
    // {
    //     WriteLine("# ============================================================================");
    //     WriteLine("# Handle Type Aliases");
    //     WriteLine("# ============================================================================");
    //     WriteLine();

    //     foreach (var typeId in typeIds.OrderBy(id => id))
    //     {
    //         if (!_wrapperClassNames.ContainsKey(typeId))
    //         {
    //             var handleTypeName = GetHandleTypeName(typeId);
    //             if (_generatedTypeAliases.Add(handleTypeName))
    //             {
    //                 WriteLine($"{handleTypeName} = Handle  # {GetTypeDescription(typeId)}");
    //             }
    //         }
    //     }
    //     WriteLine();
    // }

    /// <summary>
    /// Generates Python enums from discovered enum types.
    /// </summary>
    private void GenerateEnumTypes(IReadOnlyList<AtsEnumTypeInfo> enumTypes)
    {
        if (enumTypes.Count == 0)
        {
            return;
        }

        WriteLine("# ============================================================================");
        WriteLine("# Enum Types");
        WriteLine("# ============================================================================");
        WriteLine();

        foreach (var enumType in enumTypes)
        {
            var enumName = _enumTypeNames[enumType.TypeId];
            WriteLine($"{enumName} = Literal[{string.Join(", ", enumType.Values.Select(v => $"\"{v}\""))}]");
            WriteLine();
        }
    }

    /// <summary>
    /// Generates Python classes for DTO types marked with [AspireDto].
    /// </summary>
    private void GenerateDtoClasses(IReadOnlyList<AtsDtoTypeInfo> dtoTypes)
    {
        if (dtoTypes.Count == 0)
        {
            return;
        }

        WriteLine("# ============================================================================");
        WriteLine("# DTO Classes (Data Transfer Objects)");
        WriteLine("# ============================================================================");
        WriteLine();

        foreach (var dtoType in dtoTypes)
        {
            var className = GetDtoClassName(dtoType.TypeId);

            WriteLine($"class {className}:");
            WriteLine();

            // Generate __init__ method
            if (dtoType.Properties.Count == 0)
            {
                WriteLine("    def __init__(self) -> None:");
                WriteLine("        pass");
            }
            else
            {
                WriteLine("    def __init__(");
                WriteLine("        self,");
                WriteLine("        *,");
                foreach (var prop in dtoType.Properties)
                {
                    var propName = ToSnakeCase(prop.Name);
                    var propType = MapTypeRefToPython(prop.Type);
                    // TODO: This isn't properly checking for optionality
                    WriteLine($"        {propName}: {propType} | None = None,");
                }
                WriteLine("    ) -> None:");
                foreach (var prop in dtoType.Properties)
                {
                    var propName = ToSnakeCase(prop.Name);
                    WriteLine($"        self.{propName} = {propName}");
                }
            }

            WriteLine();
        }
    }

    /// <summary>
    /// Converts a camelCase name to snake_case.
    /// </summary>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(name[0]));

        for (int i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    private void GenerateBuilderClass(BuilderModel builder)
    {
        var baseClass = builder.IsInterface ? "ResourceBuilderBase" : "ResourceBuilderBase";

        WriteLine($"class {builder.BuilderClassName}({baseClass}):");
        WriteLine();

        WriteLine("    def __init__(self, handle: Handle, client: AspireClient) -> None:");
        WriteLine("        super().__init__(handle, client)");
        WriteLine();

        // Generate methods for each capability
        foreach (var capability in builder.Capabilities)
        {
            GenerateBuilderMethod(capability);
            WriteLine();
        }
    }

    private void GenerateBuilderMethod(AtsCapabilityInfo capability)
    {
        var methodName = GetPythonMethodName(capability.MethodName);
        var parameters = capability.Parameters.ToList();
        var returnType = MapTypeRefToPython(capability.ReturnType);

        // Generate method signature
        Write($"    async def {methodName}(self");

        foreach (var param in parameters)
        {
            var paramName = ToSnakeCase(param.Name);
            var paramType = MapParameterToPython(param);

            if (param.IsOptional || param.IsNullable)
            {
                Write($", {paramName}: {paramType} | None = None");
            }
            else
            {
                Write($", {paramName}: {paramType}");
            }
        }

        WriteLine($") -> {returnType}:");

        // Generate docstring
        WriteLine($"        \"\"\"");
        WriteLine($"        {capability.MethodName}");
        WriteLine($"        \"\"\"");

        // Generate method body
        WriteLine($"        result = await self._client.invoke_capability(");
        WriteLine($"            '{capability.CapabilityId}',");
        WriteLine($"            {{");
        WriteLine($"                'target': self._handle,");

        foreach (var param in parameters)
        {
            var paramName = ToSnakeCase(param.Name);
            WriteLine($"                '{param.Name}': {paramName},");
        }

        WriteLine($"            }}");
        WriteLine($"        )");

        // Return the result (wrapped if needed)
        if (returnType != "None")
        {
            WriteLine($"        return result  # type: ignore");
        }
    }

    /// <summary>
    /// Collects all type IDs referenced in a capability for handle type alias generation.
    /// </summary>
    // private static void CollectTypeIds(AtsCapabilityInfo capability, HashSet<string> typeIds)
    // {
    //     if (capability.TargetType != null)
    //     {
    //         CollectTypeIdsFromTypeRef(capability.TargetType, typeIds);
    //     }

    //     if (capability.ReturnType != null)
    //     {
    //         CollectTypeIdsFromTypeRef(capability.ReturnType, typeIds);
    //     }

    //     foreach (var param in capability.Parameters)
    //     {
    //         if (param.Type != null)
    //         {
    //             CollectTypeIdsFromTypeRef(param.Type, typeIds);
    //         }
    //     }
    // }

    private static void CollectTypeIdsFromTypeRef(AtsTypeRef typeRef, HashSet<string> typeIds)
    {
        if (typeRef.Category == AtsTypeCategory.Handle)
        {
            typeIds.Add(typeRef.TypeId);
        }

        if (typeRef.ElementType != null)
        {
            CollectTypeIdsFromTypeRef(typeRef.ElementType, typeIds);
        }

        if (typeRef.KeyType != null)
        {
            CollectTypeIdsFromTypeRef(typeRef.KeyType, typeIds);
        }

        if (typeRef.ValueType != null)
        {
            CollectTypeIdsFromTypeRef(typeRef.ValueType, typeIds);
        }

        if (typeRef.UnionTypes != null)
        {
            foreach (var unionType in typeRef.UnionTypes)
            {
                CollectTypeIdsFromTypeRef(unionType, typeIds);
            }
        }
    }

    /// <summary>
    /// Topologically sorts builders so base classes are generated before derived classes.
    /// </summary>
    private static List<BuilderModel> TopologicalSortBuilders(List<BuilderModel> builders)
    {
        // Simple sort: interfaces first, then concrete types
        return builders.OrderBy(b => b.IsInterface ? 0 : 1).ThenBy(b => b.BuilderClassName).ToList();
    }

    /// <summary>
    /// Gets the builder class name for a type ID.
    /// </summary>
    private static string GetBuilderClassName(string typeId)
    {
        var simpleTypeName = ExtractSimpleTypeName(typeId);

        // For interfaces, remove the "I" prefix
        if (simpleTypeName.StartsWith("I") && simpleTypeName.Length > 1 && char.IsUpper(simpleTypeName[1]))
        {
            return simpleTypeName.Substring(1);
        }

        return simpleTypeName;
    }

    /// <summary>
    /// Gets the handle type name for a type ID.
    /// </summary>
    private static string GetHandleTypeName(string typeId)
    {
        var simpleTypeName = ExtractSimpleTypeName(typeId);

        // For interfaces, remove the "I" prefix
        if (simpleTypeName.StartsWith("I") && simpleTypeName.Length > 1 && char.IsUpper(simpleTypeName[1]))
        {
            return simpleTypeName.Substring(1);
        }

        return simpleTypeName;
    }

    /// <summary>
    /// Extracts the simple type name from a type ID (e.g., "Aspire.Hosting/RedisResource" -> "RedisResource").
    /// </summary>
    private static string ExtractSimpleTypeName(string typeId)
    {
        var lastSlash = typeId.LastIndexOf('/');
        return (lastSlash >= 0 ? typeId[(lastSlash + 1)..] : typeId).Split('.').Last();
    }

    /// <summary>
    /// Generates a callback type signature for Python.
    /// </summary>
    private string GenerateCallbackTypeSignature(IReadOnlyList<AtsCallbackParameterInfo>? parameters, AtsTypeRef? returnType)
    {
        var paramTypes = parameters?.Select(p => MapTypeRefToPython(p.Type)).ToList() ?? new List<string>();
        var returnTypeStr = returnType != null ? MapTypeRefToPython(returnType) : "None";

        if (paramTypes.Count == 0)
        {
            return $"Callable[[], Awaitable[{returnTypeStr}]]";
        }

        return $"Callable[[{string.Join(", ", paramTypes)}], Awaitable[{returnTypeStr}]]";
    }
}
