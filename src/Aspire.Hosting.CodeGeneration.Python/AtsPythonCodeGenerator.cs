// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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
    public AtsTypeRef? TargetType { get; init; }
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
public sealed class AtsPythonCodeGenerator : ICodeGenerator
{
    private PythonModuleBuilder _moduleBuilder = null!;

    // Mapping of typeId -> wrapper class name for all generated wrapper types
    // Used to resolve parameter types to wrapper classes instead of handle types
    private readonly Dictionary<string, string> _wrapperClassNames = new(StringComparer.Ordinal);

    // Set of type IDs that have Promise wrappers (types with chainable methods)
    // Used to determine return types for methods
    private readonly HashSet<string> _typesWithPromiseWrappers = new(StringComparer.Ordinal);

    // Mapping of enum type IDs to Python enum names
    private readonly Dictionary<string, string> _enumTypeNames = new(StringComparer.Ordinal);

    /// <summary>
    /// Checks if an AtsTypeRef represents a handle type.
    /// </summary>
    private static bool IsHandleType(AtsTypeRef? typeRef) =>
        typeRef != null && typeRef.Category == AtsTypeCategory.Handle;

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
        AtsConstants.CancellationToken => "AbortSignal",
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
            return "ABC";
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

    /// <summary>
    /// Gets the TypeId from a capability's return type.
    /// </summary>
    private static string? GetReturnTypeId(AtsCapabilityInfo capability) => capability.ReturnType?.TypeId;

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
        _moduleBuilder = new PythonModuleBuilder();

        var capabilities = context.Capabilities;
        var dtoTypes = context.DtoTypes;
        var enumTypes = context.EnumTypes;

        // Get builder models (flattened - each builder has all its applicable capabilities)
        var allBuilders = CreateBuilderModels(capabilities);
        var entryPoints = GetEntryPointCapabilities(capabilities);

        // All builders (no special filtering)
        var builders = allBuilders;

        // Collect all unique type IDs for handle type aliases
        // Exclude DTO types - they have their own interfaces, not handle aliases
        var dtoTypeIds = new HashSet<string>(dtoTypes.Select(d => d.TypeId));
        var typeIds = new HashSet<string>();
        foreach (var cap in capabilities)
        {
            if (!string.IsNullOrEmpty(cap.TargetTypeId) && !dtoTypeIds.Contains(cap.TargetTypeId))
            {
                typeIds.Add(cap.TargetTypeId);
            }
            if (IsHandleType(cap.ReturnType) && !dtoTypeIds.Contains(cap.ReturnType!.TypeId))
            {
                typeIds.Add(GetReturnTypeId(cap)!);
            }
            // Add parameter type IDs (for types like IResourceBuilder<IResource>)
            foreach (var param in cap.Parameters)
            {
                if (IsHandleType(param.Type) && !dtoTypeIds.Contains(param.Type!.TypeId))
                {
                    typeIds.Add(param.Type!.TypeId);
                }
                // Also collect callback parameter types
                if (param.IsCallback && param.CallbackParameters != null)
                {
                    foreach (var cbParam in param.CallbackParameters)
                    {
                        if (IsHandleType(cbParam.Type) && !dtoTypeIds.Contains(cbParam.Type.TypeId))
                        {
                            typeIds.Add(cbParam.Type.TypeId);
                        }
                    }
                }
            }
        }

        // Collect enum type names
        foreach (var enumType in enumTypes)
        {
            if (!_enumTypeNames.ContainsKey(enumType.TypeId))
            {
                _enumTypeNames[enumType.TypeId] = ExtractSimpleTypeName(enumType.TypeId);
            }
        }

        // Separate builders into categories:
        // 1. Resource builders: IResource*, ContainerResource, etc.
        // 2. Type classes: everything else (context types, wrapper types)
        var resourceBuilders = builders.Where(b => b.TargetType?.IsResourceBuilder == true).ToList();
        var interfaceClasses = builders.Where(b => b.IsInterface).ToList();
        var typeClasses = builders.Where(b => b.TargetType?.IsResourceBuilder != true).ToList();

        // Build wrapper class name mapping for type resolution BEFORE generating code
        // This allows parameter types to use wrapper class names instead of handle types
        _wrapperClassNames.Clear();
        _typesWithPromiseWrappers.Clear();

        foreach (var builder in resourceBuilders)
        {
            _wrapperClassNames[builder.TypeId] = builder.BuilderClassName;
            // All resource builders get Promise wrappers
            _typesWithPromiseWrappers.Add(builder.TypeId);
        }
        foreach (var typeClass in typeClasses)
        {
            _wrapperClassNames[typeClass.TypeId] = DeriveClassName(typeClass.TypeId);
            // Type classes with methods get Promise wrappers
            if (HasChainableMethods(typeClass))
            {
                _typesWithPromiseWrappers.Add(typeClass.TypeId);
            }
        }
        // Add ReferenceExpression (defined in base.py, not generated)
        _wrapperClassNames[AtsConstants.ReferenceExpressionTypeId] = "ReferenceExpression";

        // Generate enum types
        GenerateEnumTypes(_moduleBuilder.Enums, enumTypes);

        // Generate DTO classes
        GenerateDtoClasses(_moduleBuilder.DtoClasses, dtoTypes);

        // Generate type classes (context types and wrapper types)
        foreach (var typeClass in typeClasses)
        {
            GenerateTypeClass(_moduleBuilder.TypeClasses, typeClass);
        }

        // Generate resource builder classes
        foreach (var builder in resourceBuilders)
        {
            GenerateBuilderClass(_moduleBuilder.ResourceBuilders, builder);
            _moduleBuilder.ResourceBuilders.AppendLine();
        }

        // Generate entry point functions
        GenerateEntryPointFunctions(_moduleBuilder.EntryPoints, entryPoints);

        // Generate connection helper
        GenerateConnectionHelper(_moduleBuilder.ConnectionHelper);

        return _moduleBuilder.Write();
    }

    /// <summary>
    /// Generates Python enums from discovered enum types.
    /// </summary>
    private void GenerateEnumTypes(System.Text.StringBuilder sb, IReadOnlyList<AtsEnumTypeInfo> enumTypes)
    {
        if (enumTypes.Count == 0)
        {
            return;
        }

        foreach (var enumType in enumTypes.OrderBy(e => e.Name))
        {
            var enumName = _enumTypeNames[enumType.TypeId];
            sb.AppendLine(CultureInfo.InvariantCulture, $"{enumName} = Literal[{string.Join(", ", enumType.Values.Select(v => $"\"{v}\""))}]");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates Python classes for DTO types marked with [AspireDto].
    /// </summary>
    private void GenerateDtoClasses(System.Text.StringBuilder sb, IReadOnlyList<AtsDtoTypeInfo> dtoTypes)
    {
        if (dtoTypes.Count == 0)
        {
            return;
        }

        foreach (var dtoType in dtoTypes.OrderBy(d => d.Name))
        {
            var className = GetDtoClassName(dtoType.TypeId);

            sb.AppendLine(CultureInfo.InvariantCulture, $"class {className}:");
            sb.AppendLine();

            // Generate __init__ method
            if (dtoType.Properties.Count == 0)
            {
                sb.AppendLine("    def __init__(self) -> None:");
                sb.AppendLine("        pass");
            }
            else
            {
                sb.AppendLine("    def __init__(");
                sb.AppendLine("        self,");
                sb.AppendLine("        *,");
                foreach (var prop in dtoType.Properties)
                {
                    var propName = ToSnakeCase(prop.Name);
                    var propType = MapTypeRefToPython(prop.Type);
                    // All DTO properties are optional in Python to allow partial objects
                    sb.AppendLine(CultureInfo.InvariantCulture, $"        {propName}: {propType} | None = None,");
                }
                sb.AppendLine("    ) -> None:");
                foreach (var prop in dtoType.Properties)
                {
                    var propName = ToSnakeCase(prop.Name);
                    sb.AppendLine(CultureInfo.InvariantCulture, $"        self.{propName} = {propName}");
                }
            }

            sb.AppendLine();
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

    /// <summary>
    /// Generates a type class (context type or wrapper type).
    /// Uses property-like pattern for exposed properties.
    /// </summary>
    private void GenerateTypeClass(System.Text.StringBuilder sb, BuilderModel model)
    {
        var className = DeriveClassName(model.TypeId);

        sb.AppendLine();

        // Separate capabilities by type using CapabilityKind enum
        var getters = model.Capabilities.Where(c => c.CapabilityKind == AtsCapabilityKind.PropertyGetter).ToList();
        var setters = model.Capabilities.Where(c => c.CapabilityKind == AtsCapabilityKind.PropertySetter).ToList();
        var contextMethods = model.Capabilities.Where(c => c.CapabilityKind == AtsCapabilityKind.InstanceMethod).ToList();
        var otherMethods = model.Capabilities.Where(c => c.CapabilityKind == AtsCapabilityKind.Method).ToList();

        // Combine methods
        var allMethods = contextMethods.Concat(otherMethods).ToList();

        sb.AppendLine(CultureInfo.InvariantCulture, $"class {className}:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    '''Type class for {className}.'''");
        sb.AppendLine();
        sb.AppendLine("    def __init__(self, handle: Handle, client: AspireClient) -> None:");
        sb.AppendLine("        self._handle = handle");
        sb.AppendLine("        self._client = client");
        sb.AppendLine();

        // Group getters and setters by property name to create properties
        var properties = GroupPropertiesByName(getters, setters);

        // Generate properties
        foreach (var prop in properties)
        {
            GeneratePropertyMethods(sb, prop.PropertyName, prop.Getter, prop.Setter);
        }

        // Generate methods
        foreach (var method in allMethods)
        {
            GenerateTypeClassMethod(sb, method);
        }

        // Handle edge case: empty class
        if (properties.Count == 0 && allMethods.Count == 0)
        {
            sb.AppendLine("    pass");
        }
    }

    /// <summary>
    /// Groups getters and setters by property name.
    /// </summary>
    private static List<(string PropertyName, AtsCapabilityInfo? Getter, AtsCapabilityInfo? Setter)> GroupPropertiesByName(
        List<AtsCapabilityInfo> getters, List<AtsCapabilityInfo> setters)
    {
        var result = new List<(string PropertyName, AtsCapabilityInfo? Getter, AtsCapabilityInfo? Setter)>();
        var processedNames = new HashSet<string>();

        // Process getters
        foreach (var getter in getters)
        {
            var propName = ExtractPropertyName(getter.MethodName);
            if (processedNames.Contains(propName))
            {
                continue;
            }
            processedNames.Add(propName);

            // Find matching setter (setPropertyName for propertyName)
            var setterName = "set" + char.ToUpperInvariant(propName[0]) + propName[1..];
            var setter = setters.FirstOrDefault(s => ExtractPropertyName(s.MethodName).Equals(setterName, StringComparison.OrdinalIgnoreCase));

            result.Add((propName, getter, setter));
        }

        // Process any setters without matching getters
        foreach (var setter in setters)
        {
            var setterMethodName = ExtractPropertyName(setter.MethodName);
            // setPropertyName -> propertyName
            if (setterMethodName.StartsWith("set", StringComparison.OrdinalIgnoreCase) && setterMethodName.Length > 3)
            {
                var propName = char.ToLowerInvariant(setterMethodName[3]) + setterMethodName[4..];
                if (!processedNames.Contains(propName))
                {
                    processedNames.Add(propName);
                    result.Add((propName, null, setter));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts the property name from a method name like "ClassName.propertyName" or "setPropertyName".
    /// </summary>
    private static string ExtractPropertyName(string methodName)
    {
        // Handle "ClassName.propertyName" format
        if (methodName.Contains('.'))
        {
            return methodName[(methodName.LastIndexOf('.') + 1)..];
        }
        return methodName;
    }

    /// <summary>
    /// Generates getter and setter methods for a property.
    /// </summary>
    private void GeneratePropertyMethods(System.Text.StringBuilder sb, string propertyName, AtsCapabilityInfo? getter, AtsCapabilityInfo? setter)
    {
        var snakeName = ToSnakeCase(propertyName);

        // Generate getter
        if (getter != null)
        {
            var returnType = MapTypeRefToPython(getter.ReturnType);

            if (!string.IsNullOrEmpty(getter.Description))
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    async def get_{snakeName}(self) -> {returnType}:");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        '''{getter.Description}'''");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    async def get_{snakeName}(self) -> {returnType}:");
            }

            sb.AppendLine(CultureInfo.InvariantCulture, $"        result = await self._client.invoke_capability(");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            '{getter.CapabilityId}',");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            {{'context': self._handle}}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        )");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        return result  # type: ignore");
            sb.AppendLine();
        }

        // Generate setter
        if (setter != null)
        {
            var valueParam = setter.Parameters.FirstOrDefault(p => p.Name == "value");
            if (valueParam != null)
            {
                var valueType = MapTypeRefToPython(valueParam.Type);

                if (!string.IsNullOrEmpty(setter.Description))
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    async def set_{snakeName}(self, value: {valueType}) -> None:");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"        '''{setter.Description}'''");
                }
                else
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    async def set_{snakeName}(self, value: {valueType}) -> None:");
                }

                sb.AppendLine(CultureInfo.InvariantCulture, $"        await self._client.invoke_capability(");
                sb.AppendLine(CultureInfo.InvariantCulture, $"            '{setter.CapabilityId}',");
                sb.AppendLine(CultureInfo.InvariantCulture, $"            {{'context': self._handle, 'value': value}}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        )");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Generates a method on a type class.
    /// </summary>
    private void GenerateTypeClassMethod(System.Text.StringBuilder sb, AtsCapabilityInfo capability)
    {
        // Use OwningTypeName if available to extract method name, otherwise parse from MethodName
        var methodName = !string.IsNullOrEmpty(capability.OwningTypeName) && capability.MethodName.Contains('.')
            ? capability.MethodName[(capability.MethodName.LastIndexOf('.') + 1)..]
            : capability.MethodName;

        var pythonMethodName = ToSnakeCase(methodName);

        // Filter out target parameter
        var targetParamName = capability.TargetParameterName ?? "context";
        var userParams = capability.Parameters.Where(p => p.Name != targetParamName).ToList();

        // Determine return type
        var returnType = GetReturnTypeId(capability) != null
            ? MapTypeRefToPython(capability.ReturnType)
            : "None";

        // Generate method signature
        sb.Append(CultureInfo.InvariantCulture, $"    async def {pythonMethodName}(self");

        foreach (var param in userParams)
        {
            var paramName = ToSnakeCase(param.Name);
            var paramType = MapParameterToPython(param);

            if (param.IsOptional || param.IsNullable)
            {
                sb.Append(CultureInfo.InvariantCulture, $", {paramName}: {paramType} | None = None");
            }
            else
            {
                sb.Append(CultureInfo.InvariantCulture, $", {paramName}: {paramType}");
            }
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $") -> {returnType}:");

        // Generate docstring
        if (!string.IsNullOrEmpty(capability.Description))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        '''{capability.Description}'''");
        }

        // Build args dict
        sb.AppendLine(CultureInfo.InvariantCulture, $"        rpc_args: dict[str, Any] = {{'{targetParamName}': self._handle}}");
        foreach (var param in userParams)
        {
            var paramName = ToSnakeCase(param.Name);
            if (param.IsOptional || param.IsNullable)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"        if {paramName} is not None:");
                sb.AppendLine(CultureInfo.InvariantCulture, $"            rpc_args['{param.Name}'] = {paramName}");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"        rpc_args['{param.Name}'] = {paramName}");
            }
        }

        // Invoke capability
        if (returnType == "None")
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        await self._client.invoke_capability(");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            '{capability.CapabilityId}',");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            rpc_args");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        )");
        }
        else
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        result = await self._client.invoke_capability(");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            '{capability.CapabilityId}',");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            rpc_args");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        )");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        return result  # type: ignore");
        }
        sb.AppendLine();
    }

    private void GenerateBuilderClass(System.Text.StringBuilder sb, BuilderModel builder)
    {
        sb.AppendLine(CultureInfo.InvariantCulture, $"class {builder.BuilderClassName}(ResourceBuilderBase):");
        sb.AppendLine();

        sb.AppendLine("    def __init__(self, handle: Handle, client: AspireClient) -> None:");
        sb.AppendLine("        super().__init__(handle, client)");
        sb.AppendLine();

        // Generate methods for each capability
        // Filter out property getters and setters - they are not methods
        var methods = builder.Capabilities.Where(c =>
            c.CapabilityKind != AtsCapabilityKind.PropertyGetter &&
            c.CapabilityKind != AtsCapabilityKind.PropertySetter).ToList();

        foreach (var capability in methods)
        {
            GenerateBuilderMethod(sb, builder, capability);
            sb.AppendLine();
        }

        // Handle edge case: empty class
        if (methods.Count == 0)
        {
            sb.AppendLine("    pass");
        }
    }

    private void GenerateBuilderMethod(System.Text.StringBuilder sb, BuilderModel builder, AtsCapabilityInfo capability)
    {
        var methodName = GetPythonMethodName(capability.MethodName);
        var parameters = capability.Parameters.ToList();
        var returnType = MapTypeRefToPython(capability.ReturnType);

        // Use the actual target parameter name from the capability
        var targetParamName = capability.TargetParameterName ?? "builder";

        // Filter out target parameter from user-facing params
        var userParams = parameters.Where(p => p.Name != targetParamName).ToList();

        // Determine return type - use the builder's own type for fluent methods
        var returnsBuilder = capability.ReturnsBuilder;

        // Generate method signature
        sb.Append(CultureInfo.InvariantCulture, $"    async def {methodName}(self");

        foreach (var param in userParams)
        {
            var paramName = ToSnakeCase(param.Name);
            var paramType = MapParameterToPython(param);

            if (param.IsOptional || param.IsNullable)
            {
                sb.Append(CultureInfo.InvariantCulture, $", {paramName}: {paramType} | None = None");
            }
            else
            {
                sb.Append(CultureInfo.InvariantCulture, $", {paramName}: {paramType}");
            }
        }

        // Return type
        if (returnsBuilder)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $") -> Self:");
        }
        else if (returnType == "None")
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $") -> None:");
        }
        else
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $") -> {returnType}:");
        }

        // Generate docstring
        if (!string.IsNullOrEmpty(capability.Description))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        '''{capability.Description}'''");
        }

        // Build args dict
        sb.AppendLine(CultureInfo.InvariantCulture, $"        rpc_args: dict[str, Any] = {{'{targetParamName}': self._handle}}");
        foreach (var param in userParams)
        {
            var paramName = ToSnakeCase(param.Name);
            if (param.IsOptional || param.IsNullable)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"        if {paramName} is not None:");
                sb.AppendLine(CultureInfo.InvariantCulture, $"            rpc_args['{param.Name}'] = {paramName}");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"        rpc_args['{param.Name}'] = {paramName}");
            }
        }

        // Invoke capability and return
        if (returnsBuilder)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        result = await self._client.invoke_capability(");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            '{capability.CapabilityId}',");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            rpc_args");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        )");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        return {builder.BuilderClassName}(result, self._client)  # type: ignore");
        }
        else if (returnType == "None")
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        await self._client.invoke_capability(");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            '{capability.CapabilityId}',");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            rpc_args");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        )");
        }
        else
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        result = await self._client.invoke_capability(");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            '{capability.CapabilityId}',");
            sb.AppendLine(CultureInfo.InvariantCulture, $"            rpc_args");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        )");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        return result  # type: ignore");
        }
    }

    /// <summary>
    /// Generates entry point functions.
    /// </summary>
    private void GenerateEntryPointFunctions(System.Text.StringBuilder sb, List<AtsCapabilityInfo> entryPoints)
    {
        if (entryPoints.Count == 0)
        {
            return;
        }

        foreach (var capability in entryPoints)
        {
            GenerateEntryPointFunction(sb, capability);
        }
    }

    private void GenerateEntryPointFunction(System.Text.StringBuilder sb, AtsCapabilityInfo capability)
    {
        var methodName = GetPythonMethodName(capability.MethodName);

        // Build parameter list
        var paramDefs = new List<string> { "client: AspireClient" };
        var paramArgs = new List<string>();

        foreach (var param in capability.Parameters)
        {
            var paramName = ToSnakeCase(param.Name);
            var paramType = MapParameterToPython(param);
            var optional = param.IsOptional || param.IsNullable ? " | None = None" : "";
            paramDefs.Add($"{paramName}: {paramType}{optional}");
            paramArgs.Add(param.Name);
        }

        var paramsString = string.Join(", ", paramDefs);

        // Determine return type
        var capReturnTypeId = GetReturnTypeId(capability);
        var returnType = !string.IsNullOrEmpty(capReturnTypeId)
            ? MapTypeRefToPython(capability.ReturnType)
            : "None";

        // Generate JSDoc equivalent
        if (!string.IsNullOrEmpty(capability.Description))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"async def {methodName}({paramsString}) -> {returnType}:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    '''{capability.Description}'''");
        }
        else
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"async def {methodName}({paramsString}) -> {returnType}:");
        }

        // Build args dict
        sb.AppendLine(CultureInfo.InvariantCulture, $"    rpc_args: dict[str, Any] = {{}}");
        foreach (var param in capability.Parameters)
        {
            var paramName = ToSnakeCase(param.Name);
            if (param.IsOptional || param.IsNullable)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    if {paramName} is not None:");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        rpc_args['{param.Name}'] = {paramName}");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    rpc_args['{param.Name}'] = {paramName}");
            }
        }

        // Invoke capability
        if (returnType == "None")
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"    await client.invoke_capability(");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        '{capability.CapabilityId}',");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        rpc_args");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    )");
        }
        else
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"    result = await client.invoke_capability(");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        '{capability.CapabilityId}',");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        rpc_args");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    )");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    return result  # type: ignore");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Generates the connection helper function.
    /// </summary>
    private static void GenerateConnectionHelper(System.Text.StringBuilder sb)
    {
        sb.AppendLine("async def connect() -> AspireClient:");
        sb.AppendLine("    '''");
        sb.AppendLine("    Creates and connects to the Aspire AppHost.");
        sb.AppendLine("    Reads connection info from environment variables set by `aspire run`.");
        sb.AppendLine("    '''");
        sb.AppendLine("    socket_path = os.environ.get('REMOTE_APP_HOST_SOCKET_PATH')");
        sb.AppendLine("    if not socket_path:");
        sb.AppendLine("        raise RuntimeError(");
        sb.AppendLine("            'REMOTE_APP_HOST_SOCKET_PATH environment variable not set. '");
        sb.AppendLine("            'Run this application using `aspire run`.'");
        sb.AppendLine("        )");
        sb.AppendLine();
        sb.AppendLine("    client = AspireClient(socket_path)");
        sb.AppendLine("    await client.connect()");
        sb.AppendLine("    return client");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("async def create_builder(**options: Any) -> DistributedApplicationBuilder:");
        sb.AppendLine("    '''");
        sb.AppendLine("    Creates a new distributed application builder.");
        sb.AppendLine("    This is the entry point for building Aspire applications.");
        sb.AppendLine();
        sb.AppendLine("    Args:");
        sb.AppendLine("        **options: Optional configuration options for the builder");
        sb.AppendLine();
        sb.AppendLine("    Returns:");
        sb.AppendLine("        A DistributedApplicationBuilder instance");
        sb.AppendLine("    '''");
        sb.AppendLine("    client = await connect()");
        sb.AppendLine();
        sb.AppendLine("    # Default args and project_directory if not provided");
        sb.AppendLine("    effective_options = {");
        sb.AppendLine("        **options,");
        sb.AppendLine("        'args': options.get('args', sys.argv[1:]),");
        sb.AppendLine("        'projectDirectory': options.get('projectDirectory', os.environ.get('ASPIRE_PROJECT_DIRECTORY', os.getcwd())),");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    handle = await client.invoke_capability(");
        sb.AppendLine("        'Aspire.Hosting/createBuilderWithOptions',");
        sb.AppendLine("        {'options': effective_options}");
        sb.AppendLine("    )");
        sb.AppendLine("    return DistributedApplicationBuilder(handle, client)");
    }

    // ============================================================================
    // Builder Model Helpers
    // ============================================================================

    /// <summary>
    /// Groups capabilities by ExpandedTargetTypes to create builder models.
    /// Uses expansion to map interface targets to their concrete implementations.
    /// Also creates builders for interface types (for use as return type wrappers).
    /// </summary>
    private static List<BuilderModel> CreateBuilderModels(IReadOnlyList<AtsCapabilityInfo> capabilities)
    {
        // Group capabilities by expanded target type IDs
        var capabilitiesByTypeId = new Dictionary<string, List<AtsCapabilityInfo>>();

        // Track the AtsTypeRef for each typeId
        var typeRefsByTypeId = new Dictionary<string, AtsTypeRef>();

        // Also track interface types and their capabilities
        var interfaceCapabilities = new Dictionary<string, List<AtsCapabilityInfo>>();

        foreach (var cap in capabilities)
        {
            var targetTypeRef = cap.TargetType;
            var targetTypeId = cap.TargetTypeId;
            if (targetTypeRef == null || string.IsNullOrEmpty(targetTypeId))
            {
                // Entry point methods - handled separately
                continue;
            }

            if (targetTypeRef.Category != AtsTypeCategory.Handle)
            {
                continue;
            }

            // Use expanded types if available, otherwise fall back to the original target
            var expandedTypes = cap.ExpandedTargetTypes;
            if (expandedTypes is { Count: > 0 })
            {
                // Flatten to concrete types
                foreach (var expandedType in expandedTypes)
                {
                    if (!capabilitiesByTypeId.TryGetValue(expandedType.TypeId, out var list))
                    {
                        list = [];
                        capabilitiesByTypeId[expandedType.TypeId] = list;
                        typeRefsByTypeId[expandedType.TypeId] = expandedType;
                    }
                    list.Add(cap);
                }

                // Also track the original interface type for wrapper class generation
                if (targetTypeRef.IsInterface)
                {
                    if (!interfaceCapabilities.TryGetValue(targetTypeId, out var interfaceList))
                    {
                        interfaceList = [];
                        interfaceCapabilities[targetTypeId] = interfaceList;
                        typeRefsByTypeId[targetTypeId] = targetTypeRef;
                    }
                    interfaceList.Add(cap);
                }
            }
            else
            {
                // No expansion - use original target (concrete type)
                if (!capabilitiesByTypeId.TryGetValue(targetTypeId, out var list))
                {
                    list = [];
                    capabilitiesByTypeId[targetTypeId] = list;
                    typeRefsByTypeId[targetTypeId] = targetTypeRef;
                }
                list.Add(cap);
            }
        }

        // Create a builder for each concrete type with its specific capabilities
        var builders = new List<BuilderModel>();
        foreach (var (typeId, typeCapabilities) in capabilitiesByTypeId)
        {
            var builderClassName = DeriveClassName(typeId);
            var typeRef = typeRefsByTypeId.GetValueOrDefault(typeId);

            // Deduplicate capabilities by CapabilityId
            var uniqueCapabilities = typeCapabilities
                .GroupBy(c => c.CapabilityId)
                .Select(g => g.First())
                .ToList();

            var builder = new BuilderModel
            {
                TypeId = typeId,
                BuilderClassName = builderClassName,
                Capabilities = uniqueCapabilities,
                IsInterface = typeRef?.IsInterface ?? false,
                TargetType = typeRef
            };

            builders.Add(builder);
        }

        // Also create builders for interface types
        foreach (var (interfaceTypeId, caps) in interfaceCapabilities)
        {
            if (capabilitiesByTypeId.ContainsKey(interfaceTypeId))
            {
                continue;
            }

            var builderClassName = DeriveClassName(interfaceTypeId);
            var typeRef = typeRefsByTypeId.GetValueOrDefault(interfaceTypeId);

            var uniqueCapabilities = caps
                .GroupBy(c => c.CapabilityId)
                .Select(g => g.First())
                .ToList();

            var builder = new BuilderModel
            {
                TypeId = interfaceTypeId,
                BuilderClassName = builderClassName,
                Capabilities = uniqueCapabilities,
                IsInterface = true,
                TargetType = typeRef
            };

            builders.Add(builder);
        }

        // Also create builders for resource types referenced anywhere in capabilities
        var allReferencedTypeRefs = CollectAllReferencedTypes(capabilities);
        var existingBuilderTypeIds = new HashSet<string>(capabilitiesByTypeId.Keys);
        foreach (var (interfaceTypeId, _) in interfaceCapabilities)
        {
            existingBuilderTypeIds.Add(interfaceTypeId);
        }

        foreach (var (typeId, typeRef) in allReferencedTypeRefs)
        {
            if (existingBuilderTypeIds.Contains(typeId))
            {
                continue;
            }

            if (!typeRef.IsResourceBuilder)
            {
                continue;
            }

            var builderClassName = DeriveClassName(typeId);
            var builder = new BuilderModel
            {
                TypeId = typeId,
                BuilderClassName = builderClassName,
                Capabilities = [],
                IsInterface = typeRef.IsInterface,
                TargetType = typeRef
            };
            builders.Add(builder);
        }

        // Sort: concrete types first, then interfaces
        return builders
            .OrderBy(b => b.IsInterface)
            .ThenBy(b => b.BuilderClassName)
            .ToList();
    }

    /// <summary>
    /// Collects all type refs referenced in capabilities.
    /// </summary>
    private static Dictionary<string, AtsTypeRef> CollectAllReferencedTypes(IReadOnlyList<AtsCapabilityInfo> capabilities)
    {
        var typeRefs = new Dictionary<string, AtsTypeRef>();

        void CollectFromTypeRef(AtsTypeRef? typeRef)
        {
            if (typeRef == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(typeRef.TypeId) && typeRef.Category == AtsTypeCategory.Handle)
            {
                typeRefs.TryAdd(typeRef.TypeId, typeRef);
            }

            CollectFromTypeRef(typeRef.ElementType);
            CollectFromTypeRef(typeRef.KeyType);
            CollectFromTypeRef(typeRef.ValueType);
            if (typeRef.UnionTypes != null)
            {
                foreach (var unionType in typeRef.UnionTypes)
                {
                    CollectFromTypeRef(unionType);
                }
            }
        }

        foreach (var cap in capabilities)
        {
            CollectFromTypeRef(cap.ReturnType);

            foreach (var param in cap.Parameters)
            {
                CollectFromTypeRef(param.Type);

                if (param.IsCallback)
                {
                    if (param.CallbackParameters != null)
                    {
                        foreach (var cbParam in param.CallbackParameters)
                        {
                            CollectFromTypeRef(cbParam.Type);
                        }
                    }
                    CollectFromTypeRef(param.CallbackReturnType);
                }
            }
        }

        return typeRefs;
    }

    /// <summary>
    /// Gets entry point capabilities (those without TargetTypeId).
    /// </summary>
    private static List<AtsCapabilityInfo> GetEntryPointCapabilities(IReadOnlyList<AtsCapabilityInfo> capabilities)
    {
        return capabilities.Where(c => string.IsNullOrEmpty(c.TargetTypeId)).ToList();
    }

    /// <summary>
    /// Derives the class name from an ATS type ID.
    /// For interfaces like IResource, strips the leading 'I'.
    /// </summary>
    private static string DeriveClassName(string typeId)
    {
        var typeName = ExtractSimpleTypeName(typeId);

        // Strip leading 'I' from interface types
        if (typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]))
        {
            return typeName[1..];
        }

        return typeName;
    }

    /// <summary>
    /// Gets the handle type alias name for a type ID.
    /// </summary>
    private static string GetHandleTypeName(string typeId)
    {
        var typeName = ExtractSimpleTypeName(typeId);

        // Sanitize generic types like "Dict<String,Object>" -> "DictStringObject"
        typeName = typeName
            .Replace("[]", "Array", StringComparison.Ordinal)
            .Replace("<", "", StringComparison.Ordinal)
            .Replace(">", "", StringComparison.Ordinal)
            .Replace(",", "", StringComparison.Ordinal);

        return $"{typeName}Handle";
    }

    /// <summary>
    /// Extracts the simple type name from a type ID (e.g., "Aspire.Hosting/RedisResource" -> "RedisResource").
    /// </summary>
    private static string ExtractSimpleTypeName(string typeId)
    {
        var slashIndex = typeId.LastIndexOf('/');
        var fullTypeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;

        var dotIndex = fullTypeName.LastIndexOf('.');
        return dotIndex >= 0 ? fullTypeName[(dotIndex + 1)..] : fullTypeName;
    }

    /// <summary>
    /// Determines if a type has chainable methods and should have a Promise wrapper.
    /// Types with instance methods or wrapper methods get Promise wrappers.
    /// </summary>
    private static bool HasChainableMethods(BuilderModel model)
    {
        return model.Capabilities.Any(c =>
            c.CapabilityKind == AtsCapabilityKind.InstanceMethod ||
            c.CapabilityKind == AtsCapabilityKind.Method);
    }

    /// <summary>
    /// Generates a callback type signature for Python.
    /// </summary>
    private string GenerateCallbackTypeSignature(IReadOnlyList<AtsCallbackParameterInfo>? parameters, AtsTypeRef? returnType)
    {
        var paramTypes = parameters?.Select(p => MapTypeRefToPython(p.Type)).ToList() ?? [];
        var returnTypeStr = returnType != null ? MapTypeRefToPython(returnType) : "None";

        if (paramTypes.Count == 0)
        {
            return $"Callable[[], Awaitable[{returnTypeStr}]]";
        }

        return $"Callable[[{string.Join(", ", paramTypes)}], Awaitable[{returnTypeStr}]]";
    }
}
