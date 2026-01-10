// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using Aspire.Hosting.Ats;
using Aspire.Hosting.CodeGeneration.Models.Ats;

namespace Aspire.Hosting.CodeGeneration.TypeScript;

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
/// Generates a TypeScript SDK using the ATS (Aspire Type System) capability-based API.
/// Produces typed builder classes with fluent methods that use invokeCapability().
/// </summary>
/// <remarks>
/// <para>
/// <b>ATS to TypeScript Type Mapping</b>
/// </para>
/// <para>
/// The generator maps ATS types to TypeScript types according to the following rules:
/// </para>
/// <para>
/// <b>Primitive Types:</b>
/// <list type="table">
///   <listheader>
///     <term>ATS Type</term>
///     <description>TypeScript Type</description>
///   </listheader>
///   <item><term><c>string</c></term><description><c>string</c></description></item>
///   <item><term><c>number</c></term><description><c>number</c></description></item>
///   <item><term><c>boolean</c></term><description><c>boolean</c></description></item>
///   <item><term><c>any</c></term><description><c>unknown</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Handle Types:</b>
/// Type IDs use the format <c>{AssemblyName}/{TypeName}</c>.
/// <list type="table">
///   <listheader>
///     <term>ATS Type ID</term>
///     <description>TypeScript Type</description>
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
///     <description>TypeScript Type</description>
///   </listheader>
///   <item><term><c>callback</c></term><description><c>(context: EnvironmentContextHandle) =&gt; Promise&lt;void&gt;</c></description></item>
///   <item><term><c>T[]</c> (array)</term><description><c>T[]</c> (array of mapped type)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Builder Class Generation:</b>
/// <list type="bullet">
///   <item><description><c>Aspire.Hosting.Redis/RedisResource</c> → <c>RedisResourceBuilder</c> class with <c>RedisResourceBuilderPromise</c> thenable wrapper</description></item>
///   <item><description><c>Aspire.Hosting.ApplicationModel/IResource</c> → <c>ResourceBuilderBase</c> abstract class (interface types get "BuilderBase" suffix)</description></item>
///   <item><description>Concrete builders extend interface builders based on type hierarchy</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Method Naming:</b>
/// <list type="bullet">
///   <item><description>Derived from capability ID: <c>Aspire.Hosting.Redis/addRedis</c> → <c>addRedis</c></description></item>
///   <item><description>Can be overridden via <c>[AspireExport(MethodName = "...")]</c></description></item>
///   <item><description>TypeScript uses camelCase (the canonical form from capability IDs)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AtsTypeScriptCodeGenerator : ICodeGenerator
{
    private TextWriter _writer = null!;

    // Mapping of typeId -> wrapper class name for all generated wrapper types
    // Used to resolve parameter types to wrapper classes instead of handle types
    private readonly Dictionary<string, string> _wrapperClassNames = new(StringComparer.Ordinal);

    // Well-known type IDs - use AtsConstants for canonical values
    private const string TypeId_Builder = AtsConstants.BuilderTypeId;
    private const string TypeId_Application = AtsConstants.ApplicationTypeId;

    /// <summary>
    /// Checks if an AtsTypeRef represents a handle type.
    /// </summary>
    private static bool IsHandleType(AtsTypeRef? typeRef) =>
        typeRef != null && typeRef.Category == AtsTypeCategory.Handle;

    /// <summary>
    /// Maps an AtsTypeRef to a TypeScript type using category-based dispatch.
    /// This is the preferred method - uses type metadata rather than string parsing.
    /// </summary>
    private string MapTypeRefToTypeScript(AtsTypeRef? typeRef)
    {
        if (typeRef == null)
        {
            return "unknown";
        }

        // Check for wrapper class first (handles custom types like ReferenceExpression)
        if (_wrapperClassNames.TryGetValue(typeRef.TypeId, out var wrapperClassName))
        {
            return wrapperClassName;
        }

        return typeRef.Category switch
        {
            AtsTypeCategory.Primitive => MapPrimitiveType(typeRef.TypeId),
            AtsTypeCategory.Handle => GetWrapperOrHandleName(typeRef.TypeId),
            AtsTypeCategory.Dto => GetDtoInterfaceName(typeRef.TypeId),
            AtsTypeCategory.Callback => "Function",  // Callbacks handled separately with full signature
            AtsTypeCategory.Array => $"{MapTypeRefToTypeScript(typeRef.ElementType)}[]",
            AtsTypeCategory.List => $"AspireList<{MapTypeRefToTypeScript(typeRef.ElementType)}>",
            AtsTypeCategory.Dict => typeRef.IsReadOnly
                ? $"Record<{MapTypeRefToTypeScript(typeRef.KeyType)}, {MapTypeRefToTypeScript(typeRef.ValueType)}>"
                : $"AspireDict<{MapTypeRefToTypeScript(typeRef.KeyType)}, {MapTypeRefToTypeScript(typeRef.ValueType)}>",
            AtsTypeCategory.Union => MapUnionTypeToTypeScript(typeRef),
            _ => typeRef.TypeId  // Fallback
        };
    }

    /// <summary>
    /// Maps primitive type IDs to TypeScript types.
    /// </summary>
    private static string MapPrimitiveType(string typeId) => typeId switch
    {
        AtsConstants.String or AtsConstants.Char => "string",
        AtsConstants.Number => "number",
        AtsConstants.Boolean => "boolean",
        AtsConstants.Void => "void",
        AtsConstants.Any => "any",
        AtsConstants.DateTime or AtsConstants.DateTimeOffset or
        AtsConstants.DateOnly or AtsConstants.TimeOnly => "string",
        AtsConstants.TimeSpan => "number",
        AtsConstants.Guid or AtsConstants.Uri => "string",
        _ when AtsConstants.IsEnum(typeId) => "string",  // Enums serialize as strings
        _ => typeId
    };

    /// <summary>
    /// Maps a union type to TypeScript union syntax (T1 | T2 | ...).
    /// </summary>
    private string MapUnionTypeToTypeScript(AtsTypeRef typeRef)
    {
        if (typeRef.UnionTypes == null || typeRef.UnionTypes.Count == 0)
        {
            return "unknown";
        }

        var memberTypes = typeRef.UnionTypes
            .Select(MapTypeRefToTypeScript)
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
    /// Gets a TypeScript interface name for a DTO type.
    /// </summary>
    private static string GetDtoInterfaceName(string typeId)
    {
        // Extract simple type name and use as interface name
        var simpleTypeName = ExtractSimpleTypeName(typeId);
        return simpleTypeName;
    }

    /// <summary>
    /// Maps a parameter to its TypeScript type, handling callbacks specially.
    /// For interface handle types, generates union types to accept both handles and wrapper classes.
    /// </summary>
    private string MapParameterToTypeScript(AtsParameterInfo param)
    {
        if (param.IsCallback)
        {
            return GenerateCallbackTypeSignature(param.CallbackParameters, param.CallbackReturnType);
        }

        var baseType = MapTypeRefToTypeScript(param.Type);

        // For interface handle types, generate union type to accept wrapper classes
        // This allows users to pass wrapper class instances directly (e.g., `.waitFor(redis)`)
        // The wrapper classes serialize correctly via toJSON()
        if (IsInterfaceHandleType(param.Type))
        {
            return $"{baseType} | ResourceBuilderBase";
        }

        return baseType;
    }

    /// <summary>
    /// Checks if a type reference is an interface handle type.
    /// Interface handles need union types to accept wrapper classes.
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
    public string Language => "TypeScript";

    /// <inheritdoc />
    public Dictionary<string, string> GenerateDistributedApplication(IReadOnlyList<AtsCapabilityInfo> capabilities)
    {
        var files = new Dictionary<string, string>();

        // Add embedded resource files (transport.ts, base.ts)
        files["transport.ts"] = GetEmbeddedResource("transport.ts");
        files["base.ts"] = GetEmbeddedResource("base.ts");

        // Generate the capability-based aspire.ts SDK
        files["aspire.ts"] = GenerateAspireSdk(capabilities.ToList());

        return files;
    }

    private static string GetEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Aspire.Hosting.CodeGeneration.TypeScript.Resources.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets a valid TypeScript method name from a capability method name.
    /// Handles dotted names like "EnvironmentContext.resource" by extracting just the final part.
    /// </summary>
    private static string GetTypeScriptMethodName(string methodName)
    {
        var dotIndex = methodName.LastIndexOf('.');
        return dotIndex >= 0 ? methodName[(dotIndex + 1)..] : methodName;
    }

    /// <summary>
    /// Generates the aspire.ts SDK file with capability-based API.
    /// </summary>
    private string GenerateAspireSdk(List<AtsCapabilityInfo> capabilities)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        _writer = stringWriter;

        // Header
        WriteLine("""
            // aspire.ts - Capability-based Aspire SDK
            // This SDK uses the ATS (Aspire Type System) capability API.
            // Capabilities are endpoints like 'Aspire.Hosting/createBuilder'.
            //
            // GENERATED CODE - DO NOT EDIT

            import {
                AspireClient as AspireClientRpc,
                Handle,
                MarshalledHandle,
                CapabilityError,
                registerCallback,
                wrapIfHandle,
                registerHandleWrapper
            } from './transport.js';

            import {
                DistributedApplicationBuilderBase,
                DistributedApplicationBase,
                ResourceBuilderBase,
                ReferenceExpression,
                refExpr,
                AspireDict,
                AspireList
            } from './base.js';
            """);
        WriteLine();

        // Get builder models (flattened - each builder has all its applicable capabilities)
        var allBuilders = CreateBuilderModels(capabilities);
        var entryPoints = GetEntryPointCapabilities(capabilities);

        // Extract the DistributedApplicationBuilder's capabilities
        var distributedAppBuilder = allBuilders.FirstOrDefault(b => b.TypeId == TypeId_Builder);
        var builderMethods = distributedAppBuilder?.Capabilities ?? [];

        // Resource builders are all other builders (not the main builder or application)
        var builders = allBuilders
            .Where(b => b.TypeId != TypeId_Builder &&
                        b.TypeId != TypeId_Application)
            .ToList();

        // Entry point methods that don't extend any type go on AspireClient
        var clientMethods = entryPoints
            .Where(c => string.IsNullOrEmpty(c.TargetTypeId))
            .ToList();

        // Collect all unique type IDs for handle type aliases
        var typeIds = new HashSet<string>();
        foreach (var cap in capabilities)
        {
            if (!string.IsNullOrEmpty(cap.TargetTypeId))
            {
                typeIds.Add(cap.TargetTypeId);
            }
            if (IsHandleType(cap.ReturnType))
            {
                typeIds.Add(GetReturnTypeId(cap)!);
            }
            // Add parameter type IDs (for types like IResourceBuilder<IResource>)
            foreach (var param in cap.Parameters)
            {
                if (IsHandleType(param.Type))
                {
                    typeIds.Add(param.Type!.TypeId);
                }
                // Also collect callback parameter types
                if (param.IsCallback && param.CallbackParameters != null)
                {
                    foreach (var cbParam in param.CallbackParameters)
                    {
                        if (IsHandleType(cbParam.Type))
                        {
                            typeIds.Add(cbParam.Type.TypeId);
                        }
                    }
                }
            }
        }

        // Add core type IDs (these are always needed even if no capabilities target them)
        typeIds.Add(TypeId_Builder);
        typeIds.Add(TypeId_Application);

        // Generate handle type aliases
        GenerateHandleTypeAliases(typeIds);

        // Separate builders into categories:
        // 1. Resource builders: IResource*, ContainerResource, etc.
        // 2. Type classes: everything else (context types, wrapper types)
        var resourceBuilders = builders.Where(b => AtsTypeMapping.IsResourceBuilderType(b.TypeId)).ToList();
        var typeClasses = builders.Where(b => !AtsTypeMapping.IsResourceBuilderType(b.TypeId)).ToList();

        // Build wrapper class name mapping for type resolution
        // This allows parameter types to use wrapper class names instead of handle types
        _wrapperClassNames.Clear();
        foreach (var builder in resourceBuilders)
        {
            _wrapperClassNames[builder.TypeId] = builder.BuilderClassName;
        }
        foreach (var typeClass in typeClasses)
        {
            _wrapperClassNames[typeClass.TypeId] = DeriveClassName(typeClass.TypeId);
        }
        // Add ReferenceExpression (defined in base.ts, not generated)
        _wrapperClassNames[AtsConstants.ReferenceExpressionTypeId] = "ReferenceExpression";

        // Generate type classes (context types and wrapper types)
        foreach (var typeClass in typeClasses)
        {
            GenerateTypeClass(typeClass);
        }

        // Generate DistributedApplicationBuilder class
        GenerateDistributedApplicationBuilder(builderMethods, resourceBuilders, typeClasses);

        // Generate resource builder classes
        foreach (var builder in resourceBuilders)
        {
            GenerateBuilderClass(builder);
        }

        // Generate AspireClient with remaining entry point methods
        GenerateAspireClient(clientMethods, builders);

        // Generate connection helper
        GenerateConnectionHelper();

        // Generate global error handling
        GenerateGlobalErrorHandling();

        // Generate handle wrapper registrations (after all classes are defined)
        GenerateHandleWrapperRegistrations(typeClasses, resourceBuilders);

        return stringWriter.ToString();
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

    private void GenerateHandleTypeAliases(HashSet<string> typeIds)
    {
        WriteLine("// ============================================================================");
        WriteLine("// Handle Type Aliases (Internal - not exported to users)");
        WriteLine("// ============================================================================");
        WriteLine();

        foreach (var typeId in typeIds.OrderBy(t => t))
        {
            var handleName = GetHandleTypeName(typeId);
            var description = GetTypeDescription(typeId);
            WriteLine($"/** {description} */");
            // Internal type alias - not exported (users work with wrapper classes)
            WriteLine($"type {handleName} = Handle<'{typeId}'>;");
            WriteLine();
        }
    }

    private static string GetTypeDescription(string typeId)
    {
        var typeName = ExtractSimpleTypeName(typeId);
        return $"Handle to {typeName}";
    }

    private void GenerateDistributedApplicationBuilder(List<AtsCapabilityInfo> methods, List<BuilderModel> resourceBuilders, List<BuilderModel> typeClasses)
    {
        WriteLine("// ============================================================================");
        WriteLine("// DistributedApplicationBuilder");
        WriteLine("// ============================================================================");
        WriteLine();

        // First generate DistributedApplication class for build() return type
        var applicationHandle = GetHandleTypeName(TypeId_Application);
        var builderHandle = GetHandleTypeName(TypeId_Builder);

        WriteLine($$"""
            /**
             * Represents a built distributed application ready to run.
             */
            export class DistributedApplication extends DistributedApplicationBase {
                constructor(handle: {{applicationHandle}}, client: AspireClientRpc) {
                    super(handle, client);
                }
            }

            /**
             * Thenable wrapper for DistributedApplication enabling fluent chaining.
             * Allows: await builder.build().run()
             */
            export class DistributedApplicationPromise implements PromiseLike<DistributedApplication> {
                constructor(private _promise: Promise<DistributedApplication>) {}

                then<TResult1 = DistributedApplication, TResult2 = never>(
                    onfulfilled?: ((value: DistributedApplication) => TResult1 | PromiseLike<TResult1>) | null,
                    onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null
                ): PromiseLike<TResult1 | TResult2> {
                    return this._promise.then(onfulfilled, onrejected);
                }

                /**
                 * Runs the distributed application, starting all configured resources.
                 * Chains through the promise for fluent chaining: await builder.build().run()
                 */
                run(): Promise<void> {
                    return this._promise.then(app => app.run());
                }
            }
            """);
        WriteLine();

        // Now generate DistributedApplicationBuilder
        WriteLine($$"""
            /**
             * Builder for creating distributed applications.
             * Use createBuilder() to get an instance.
             */
            export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {
                constructor(handle: {{builderHandle}}, client: AspireClientRpc) {
                    super(handle, client);
                }

                /** @internal - actual async implementation */
                async _buildInternal(): Promise<DistributedApplication> {
                    const handle = await this._client.invokeCapability<{{applicationHandle}}>(
                        '{{AtsConstants.BuildCapability}}',
                        { builder: this._handle }
                    );
                    return new DistributedApplication(handle, this._client);
                }

                /**
                 * Builds the distributed application from the configured builder.
                 * Returns a thenable for fluent chaining: await builder.build().run()
                 */
                build(): DistributedApplicationPromise {
                    return new DistributedApplicationPromise(this._buildInternal());
                }
            """);

        // Separate methods into:
        // - Property getters (return wrapper types like Configuration, Environment)
        // - Factory methods (return resource builders)
        // - Other methods (return void or primitives)
        var propertyMethods = methods
            .Where(m => !string.IsNullOrEmpty(GetReturnTypeId(m)) &&
                        _wrapperClassNames.ContainsKey(GetReturnTypeId(m)!) &&
                        m.Parameters.Count == 0)  // Property getters have no additional params
            .ToList();

        // Exclude methods that are already hardcoded in the template (build) or in base class (run)
        var excludedMethods = new HashSet<string> { "build", "run" };
        var otherMethods = methods
            .Except(propertyMethods)
            .Where(m => !excludedMethods.Contains(m.MethodName))
            .ToList();

        // Generate property-style getters for wrapper types
        foreach (var capability in propertyMethods)
        {
            GenerateBuilderPropertyGetter(capability);
        }

        // Generate methods that extend IDistributedApplicationBuilder
        foreach (var capability in otherMethods)
        {
            GenerateDistributedApplicationBuilderMethod(capability, resourceBuilders, typeClasses);
        }

        WriteLine("}");
        WriteLine();
    }

    private void GenerateBuilderPropertyGetter(AtsCapabilityInfo capability)
    {
        var returnTypeId = GetReturnTypeId(capability)!;
        var wrapperClassName = DeriveClassName(returnTypeId);
        var handleType = GetHandleTypeName(returnTypeId);
        var targetParamName = capability.TargetParameterName ?? "builder";

        // Derive property name from method name (getConfiguration -> configuration)
        var propertyName = capability.MethodName;
        if (propertyName.StartsWith("get", StringComparison.Ordinal) && propertyName.Length > 3)
        {
            propertyName = char.ToLowerInvariant(propertyName[3]) + propertyName[4..];
        }

        WriteLine();
        if (!string.IsNullOrEmpty(capability.Description))
        {
            WriteLine($"    /** {capability.Description} */");
        }

        // Generate property-style getter returning a Promise<WrapperClass>
        WriteLine($"    get {propertyName}(): Promise<{wrapperClassName}> {{");
        WriteLine($"        return this._client.invokeCapability<{handleType}>(");
        WriteLine($"            '{capability.CapabilityId}',");
        WriteLine($"            {{ {targetParamName}: this._handle }}");
        WriteLine($"        ).then(handle => new {wrapperClassName}(handle, this._client));");
        WriteLine("    }");
    }

    private void GenerateDistributedApplicationBuilderMethod(AtsCapabilityInfo capability, List<BuilderModel> resourceBuilders, List<BuilderModel> typeClasses)
    {
        var methodName = capability.MethodName;
        var targetParamName = capability.TargetParameterName ?? "builder";

        // Build parameter list (target param is implicit via this._handle)
        var paramDefs = new List<string>();
        var paramArgs = new List<string> { $"{targetParamName}: this._handle" };

        foreach (var param in capability.Parameters)
        {
            var tsType = MapParameterToTypeScript(param);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";
            paramDefs.Add($"{param.Name}{optional}: {tsType}");
            paramArgs.Add(param.Name);
        }

        var paramsString = string.Join(", ", paramDefs);
        var argsObject = $"{{ {string.Join(", ", paramArgs)} }}";

        // Check if return type is a wrapper type
        var returnTypeId = GetReturnTypeId(capability);
        BuilderModel? wrapperInfo = null;
        if (!string.IsNullOrEmpty(returnTypeId) && _wrapperClassNames.ContainsKey(returnTypeId))
        {
            wrapperInfo = typeClasses.FirstOrDefault(w => w.TypeId == returnTypeId);
        }

        // Determine return type for resource builders
        BuilderModel? builderInfo = null;
        if (wrapperInfo == null && !string.IsNullOrEmpty(returnTypeId) && capability.ReturnsBuilder)
        {
            builderInfo = resourceBuilders.FirstOrDefault(b => b.TypeId == returnTypeId && !b.IsInterface);
        }

        // Generate JSDoc
        WriteLine();
        if (!string.IsNullOrEmpty(capability.Description))
        {
            WriteLine($"    /**");
            WriteLine($"     * {capability.Description}");
            WriteLine($"     */");
        }

        // Generate method
        if (builderInfo != null)
        {
            // Returns a resource builder promise
            var handleType = GetHandleTypeName(returnTypeId!);
            Write($"    {methodName}(");
            Write(paramsString);
            WriteLine($"): {builderInfo.BuilderClassName}Promise {{");
            WriteLine($"        const promise = this._client.invokeCapability<{handleType}>(");
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsObject}");
            WriteLine($"        ).then(handle => new {builderInfo.BuilderClassName}(handle, this._client));");
            WriteLine($"        return new {builderInfo.BuilderClassName}Promise(promise);");
            WriteLine("    }");
        }
        else if (!string.IsNullOrEmpty(returnTypeId))
        {
            // Returns raw handle or value
            var returnType = MapTypeRefToTypeScript(capability.ReturnType);
            Write($"    async {methodName}(");
            Write(paramsString);
            WriteLine($"): Promise<{returnType}> {{");
            WriteLine($"        return await this._client.invokeCapability<{returnType}>(");
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsObject}");
            WriteLine("        );");
            WriteLine("    }");
        }
        else
        {
            // Returns void
            Write($"    async {methodName}(");
            Write(paramsString);
            WriteLine("): Promise<void> {");
            WriteLine($"        await this._client.invokeCapability<void>(");
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsObject}");
            WriteLine("        );");
            WriteLine("    }");
        }
    }

    private void GenerateBuilderClass(BuilderModel builder)
    {
        WriteLine("// ============================================================================");
        WriteLine($"// {builder.BuilderClassName}");
        WriteLine("// ============================================================================");
        WriteLine();

        var handleType = GetHandleTypeName(builder.TypeId);

        // Generate builder class extending ResourceBuilderBase
        WriteLine($"export class {builder.BuilderClassName} extends ResourceBuilderBase<{handleType}> {{");

        // Constructor
        WriteLine($"    constructor(handle: {handleType}, client: AspireClientRpc) {{");
        WriteLine($"        super(handle, client);");
        WriteLine("    }");
        WriteLine();

        // Generate internal methods and public fluent methods
        // Capabilities are already flattened - no need to collect from parents
        foreach (var capability in builder.Capabilities)
        {
            GenerateBuilderMethod(builder, capability);
        }

        WriteLine("}");
        WriteLine();

        // Generate thenable wrapper class
        GenerateThenableClass(builder);
    }

    private void GenerateBuilderMethod(BuilderModel builder, AtsCapabilityInfo capability)
    {
        var methodName = capability.MethodName;
        var internalMethodName = $"_{methodName}Internal";

        // Generate JSDoc
        if (!string.IsNullOrEmpty(capability.Description))
        {
            WriteLine($"    /** {capability.Description} */");
        }

        // Build parameter list
        var paramDefs = new List<string>();
        var paramArgs = new List<string>();

        foreach (var param in capability.Parameters)
        {
            var tsType = MapParameterToTypeScript(param);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";

            paramDefs.Add($"{param.Name}{optional}: {tsType}");

            if (param.IsCallback)
            {
                // Callbacks need to be wrapped with registerCallback
                paramArgs.Add($"callback: {param.Name}Id");
            }
            else if (param.Type?.TypeId == AtsConstants.ReferenceExpressionTypeId)
            {
                // ReferenceExpression serializes via toJSON(), pass directly
                paramArgs.Add($"{param.Name}");
            }
            else if (param.Type?.TypeId != null && _wrapperClassNames.ContainsKey(param.Type.TypeId))
            {
                // Parameter is a wrapper type - extract its handle for the capability call
                paramArgs.Add($"{param.Name}: {param.Name}._handle");
            }
            else
            {
                paramArgs.Add($"{param.Name}");
            }
        }

        var paramsString = string.Join(", ", paramDefs);
        // Use the actual target parameter name from the capability (e.g., "resource" for withReference)
        var targetParamName = capability.TargetParameterName ?? "builder";
        var argsString = paramArgs.Count > 0 ? $"{{ {targetParamName}: this._handle, {string.Join(", ", paramArgs)} }}" : $"{{ {targetParamName}: this._handle }}";

        // Determine return type - use the builder's own type for fluent methods
        // The capability may return an interface type (e.g., IResourceWithEnvironment) but
        // we're generating for a concrete builder (e.g., Container), so use the builder's type
        var returnHandle = capability.ReturnsBuilder
            ? GetHandleTypeName(builder.TypeId)
            : "void";
        var returnsBuilder = capability.ReturnsBuilder;

        // Check if this method returns a non-builder, non-void type (e.g., getEndpoint returns EndpointReference)
        var hasNonBuilderReturn = !returnsBuilder && capability.ReturnType != null;
        if (hasNonBuilderReturn)
        {
            // Generate a simple async method that returns the actual type
            var returnType = MapTypeRefToTypeScript(capability.ReturnType);

            if (!string.IsNullOrEmpty(capability.Description))
            {
                WriteLine($"    /** {capability.Description} */");
            }
            Write($"    async {methodName}(");
            Write(paramsString);
            WriteLine($"): Promise<{returnType}> {{");

            // Handle callback registration if any
            var callbackParams2 = capability.Parameters.Where(p => p.IsCallback).ToList();
            foreach (var callbackParam in callbackParams2)
            {
                GenerateCallbackRegistration(callbackParam);
            }

            WriteLine($"        return await this._client.invokeCapability<{returnType}>(");
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsString}");
            WriteLine("        );");
            WriteLine("    }");
            WriteLine();
            return;
        }

        // Generate internal async method for fluent builder methods
        WriteLine($"    /** @internal */");
        Write($"    async {internalMethodName}(");
        Write(paramsString);
        Write($"): Promise<{builder.BuilderClassName}> {{");
        WriteLine();

        // Handle callback registration if any
        var callbackParams = capability.Parameters.Where(p => p.IsCallback).ToList();
        foreach (var callbackParam in callbackParams)
        {
            GenerateCallbackRegistration(callbackParam);
        }

        if (returnsBuilder)
        {
            WriteLine($"        const result = await this._client.invokeCapability<{returnHandle}>(");
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsString}");
            WriteLine("        );");
            WriteLine($"        return new {builder.BuilderClassName}(result, this._client);");
        }
        else
        {
            WriteLine($"        await this._client.invokeCapability<void>(");
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsString}");
            WriteLine("        );");
            WriteLine($"        return this;");
        }
        WriteLine("    }");
        WriteLine();

        // Generate public fluent method (returns thenable wrapper for concrete builders)
        if (!builder.IsInterface)
        {
            var promiseClass = $"{builder.BuilderClassName}Promise";
            Write($"    {methodName}(");
            Write(paramsString);
            Write($"): {promiseClass} {{");
            WriteLine();
            Write($"        return new {promiseClass}(this.{internalMethodName}(");
            Write(string.Join(", ", capability.Parameters.Select(p => p.Name)));
            WriteLine("));");
            WriteLine("    }");
            WriteLine();
        }
    }

    private void GenerateThenableClass(BuilderModel builder)
    {
        var promiseClass = $"{builder.BuilderClassName}Promise";

        WriteLine($"/**");
        WriteLine($" * Thenable wrapper for {builder.BuilderClassName} that enables fluent chaining.");
        WriteLine($" * @example");
        WriteLine($" * await builder.addSomething().withX().withY();");
        WriteLine($" */");
        WriteLine($"export class {promiseClass} implements PromiseLike<{builder.BuilderClassName}> {{");
        WriteLine($"    constructor(private _promise: Promise<{builder.BuilderClassName}>) {{}}");
        WriteLine();

        // Generate then() for PromiseLike interface
        WriteLine($"    then<TResult1 = {builder.BuilderClassName}, TResult2 = never>(");
        WriteLine($"        onfulfilled?: ((value: {builder.BuilderClassName}) => TResult1 | PromiseLike<TResult1>) | null,");
        WriteLine("        onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null");
        WriteLine("    ): PromiseLike<TResult1 | TResult2> {");
        WriteLine("        return this._promise.then(onfulfilled, onrejected);");
        WriteLine("    }");
        WriteLine();

        // Generate fluent methods that chain via .then()
        // Capabilities are already flattened - no need to collect from parents
        foreach (var capability in builder.Capabilities)
        {
            var methodName = capability.MethodName;
            var internalMethodName = $"_{methodName}Internal";

            var paramDefs = capability.Parameters.Select(p =>
            {
                var tsType = MapParameterToTypeScript(p);
                var optional = p.IsOptional || p.IsNullable ? "?" : "";
                return $"{p.Name}{optional}: {tsType}";
            });

            var paramsString = string.Join(", ", paramDefs);
            var argsString = string.Join(", ", capability.Parameters.Select(p => p.Name));

            // Check if this method returns a non-builder type
            var hasNonBuilderReturn = !capability.ReturnsBuilder && capability.ReturnType != null;

            if (!string.IsNullOrEmpty(capability.Description))
            {
                WriteLine($"    /** {capability.Description} */");
            }

            if (hasNonBuilderReturn)
            {
                // For non-builder returns, call the async method directly and return its result
                var returnType = MapTypeRefToTypeScript(capability.ReturnType);
                Write($"    {methodName}(");
                Write(paramsString);
                WriteLine($"): Promise<{returnType}> {{");
                Write($"        return this._promise.then(b => b.{methodName}(");
                Write(argsString);
                WriteLine("));");
                WriteLine("    }");
            }
            else
            {
                // For fluent builder methods, chain to the internal method
                Write($"    {methodName}(");
                Write(paramsString);
                Write($"): {promiseClass} {{");
                WriteLine();
                WriteLine($"        return new {promiseClass}(");
                Write($"            this._promise.then(b => b.{internalMethodName}(");
                Write(argsString);
                WriteLine("))");
                WriteLine("        );");
                WriteLine("    }");
            }
            WriteLine();
        }

        WriteLine("}");
        WriteLine();
    }

    private void GenerateAspireClient(List<AtsCapabilityInfo> entryPoints, List<BuilderModel> builders)
    {
        // Entry point methods (capabilities with no TargetTypeId) are generated as standalone functions
        // They're generated in GenerateConnectionHelper after the createBuilder() function
        // This method now only handles the comment header
        if (entryPoints.Count > 0)
        {
            WriteLine("// ============================================================================");
            WriteLine("// Entry Point Functions");
            WriteLine("// ============================================================================");
            WriteLine();

            foreach (var capability in entryPoints)
            {
                GenerateEntryPointFunction(capability, builders);
            }
        }
    }

    private void GenerateEntryPointFunction(AtsCapabilityInfo capability, List<BuilderModel> builders)
    {
        var methodName = capability.MethodName;

        // Build parameter list
        var paramDefs = new List<string> { "client: AspireClientRpc" };
        var paramArgs = new List<string>();

        foreach (var param in capability.Parameters)
        {
            var tsType = MapParameterToTypeScript(param);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";
            paramDefs.Add($"{param.Name}{optional}: {tsType}");
            paramArgs.Add(param.Name);
        }

        var paramsString = string.Join(", ", paramDefs);
        var argsObject = paramArgs.Count > 0
            ? $"{{ {string.Join(", ", paramArgs)} }}"
            : "{}";

        // Determine return type
        var capReturnTypeId = GetReturnTypeId(capability);
        string returnType;
        BuilderModel? builderInfo = null;

        if (!string.IsNullOrEmpty(capReturnTypeId))
        {
            if (capability.ReturnsBuilder)
            {
                builderInfo = builders.FirstOrDefault(b => b.TypeId == capReturnTypeId && !b.IsInterface);
                if (builderInfo != null)
                {
                    returnType = $"{builderInfo.BuilderClassName}Promise";
                }
                else
                {
                    returnType = GetHandleTypeName(capReturnTypeId);
                }
            }
            else
            {
                returnType = MapTypeRefToTypeScript(capability.ReturnType);
            }
        }
        else
        {
            returnType = "void";
        }

        // Generate JSDoc
        if (!string.IsNullOrEmpty(capability.Description))
        {
            WriteLine($"/**");
            WriteLine($" * {capability.Description}");
            WriteLine($" */");
        }

        // Generate function
        if (builderInfo != null)
        {
            // Returns a thenable wrapper
            var handleType = GetHandleTypeName(capReturnTypeId!);
            Write($"export function {methodName}(");
            Write(paramsString);
            WriteLine($"): {returnType} {{");
            WriteLine($"    const promise = client.invokeCapability<{handleType}>(");
            WriteLine($"        '{capability.CapabilityId}',");
            WriteLine($"        {argsObject}");
            WriteLine($"    ).then(handle => new {builderInfo.BuilderClassName}(handle, client));");
            WriteLine($"    return new {builderInfo.BuilderClassName}Promise(promise);");
            WriteLine("}");
        }
        else
        {
            // Returns raw value
            Write($"export async function {methodName}(");
            Write(paramsString);
            WriteLine($"): Promise<{returnType}> {{");
            if (returnType == "void")
            {
                WriteLine($"    await client.invokeCapability<void>(");
            }
            else
            {
                WriteLine($"    return await client.invokeCapability<{returnType}>(");
            }
            WriteLine($"        '{capability.CapabilityId}',");
            WriteLine($"        {argsObject}");
            WriteLine("    );");
            WriteLine("}");
        }
        WriteLine();
    }

    private string GenerateCallbackTypeSignature(IReadOnlyList<AtsCallbackParameterInfo>? callbackParameters, AtsTypeRef? callbackReturnType)
    {
        // Build parameter list
        var paramList = new List<string>();
        if (callbackParameters is not null)
        {
            foreach (var param in callbackParameters)
            {
                var tsType = MapTypeRefToTypeScript(param.Type);
                paramList.Add($"{param.Name}: {tsType}");
            }
        }

        var paramsString = paramList.Count > 0 ? string.Join(", ", paramList) : "";

        // Determine return type
        var returnType = callbackReturnType == null || callbackReturnType.TypeId == AtsConstants.Void
            ? "void"
            : MapTypeRefToTypeScript(callbackReturnType);

        // Callbacks are always async in TypeScript
        return $"({paramsString}) => Promise<{returnType}>";
    }

    private void GenerateCallbackRegistration(AtsParameterInfo callbackParam)
    {
        var callbackParameters = callbackParam.CallbackParameters;
        var isOptional = callbackParam.IsOptional || callbackParam.IsNullable;
        var callbackName = callbackParam.Name;

        // Determine parameter signature for registerCallback
        string paramSignature;
        if (callbackParameters is null || callbackParameters.Count == 0)
        {
            paramSignature = "";
        }
        else if (callbackParameters.Count == 1)
        {
            paramSignature = $"{callbackParameters[0].Name}Data: unknown";
        }
        else
        {
            paramSignature = "argsData: unknown";
        }

        // For optional callbacks, wrap the registration in a conditional
        if (isOptional)
        {
            WriteLine($"        const {callbackName}Id = {callbackName} ? registerCallback(async ({paramSignature}) => {{");
        }
        else
        {
            WriteLine($"        const {callbackName}Id = registerCallback(async ({paramSignature}) => {{");
        }

        // Generate the callback body
        GenerateCallbackBody(callbackParam, callbackParameters);

        // Close the callback registration
        if (isOptional)
        {
            WriteLine("        }) : undefined;");
        }
        else
        {
            WriteLine("        });");
        }
    }

    /// <summary>
    /// Generates the body of a callback function.
    /// </summary>
    private void GenerateCallbackBody(AtsParameterInfo callbackParam, IReadOnlyList<AtsCallbackParameterInfo>? callbackParameters)
    {
        var callbackName = callbackParam.Name;

        if (callbackParameters is null || callbackParameters.Count == 0)
        {
            // No parameters - just call the callback
            WriteLine($"            await {callbackName}();");
        }
        else if (callbackParameters.Count == 1)
        {
            // Single parameter callback
            var cbParam = callbackParameters[0];
            var tsType = MapTypeRefToTypeScript(cbParam.Type);
            var cbTypeId = cbParam.Type.TypeId;

            if (_wrapperClassNames.TryGetValue(cbTypeId, out var wrapperClassName))
            {
                // For types with wrapper classes, create an instance of the wrapper
                var handleType = GetHandleTypeName(cbTypeId);
                WriteLine($"            const {cbParam.Name}Handle = wrapIfHandle({cbParam.Name}Data) as {handleType};");
                WriteLine($"            const {cbParam.Name} = new {wrapperClassName}({cbParam.Name}Handle, this._client);");
            }
            else
            {
                // For raw handle types, just wrap and cast
                WriteLine($"            const {cbParam.Name} = wrapIfHandle({cbParam.Name}Data) as {tsType};");
            }

            WriteLine($"            await {callbackName}({cbParam.Name});");
        }
        else
        {
            // Multi-parameter callback - .NET sends as { p0, p1, ... }
            var paramNames = callbackParameters.Select((p, i) => $"p{i}").ToList();
            var destructure = string.Join(", ", paramNames);

            WriteLine($"            const args = argsData as {{ {destructure}: unknown }};");

            var callArgs = new List<string>();
            for (var i = 0; i < callbackParameters.Count; i++)
            {
                var cbParam = callbackParameters[i];
                var tsType = MapTypeRefToTypeScript(cbParam.Type);
                var cbTypeId = cbParam.Type.TypeId;

                if (_wrapperClassNames.TryGetValue(cbTypeId, out var wrapperClassName))
                {
                    // For types with wrapper classes, create an instance of the wrapper
                    var handleType = GetHandleTypeName(cbTypeId);
                    WriteLine($"            const {cbParam.Name}Handle = wrapIfHandle(args.p{i}) as {handleType};");
                    WriteLine($"            const {cbParam.Name} = new {wrapperClassName}({cbParam.Name}Handle, this._client);");
                }
                else
                {
                    // For raw handle types, just wrap and cast
                    WriteLine($"            const {cbParam.Name} = wrapIfHandle(args.p{i}) as {tsType};");
                }
                callArgs.Add(cbParam.Name);
            }

            WriteLine($"            await {callbackName}({string.Join(", ", callArgs)});");
        }
    }

    private void GenerateConnectionHelper()
    {
        var builderHandle = GetHandleTypeName(TypeId_Builder);

        WriteLine($$"""
            // ============================================================================
            // Connection Helper
            // ============================================================================

            /**
             * Creates and connects to the Aspire AppHost.
             * Reads connection info from environment variables set by `aspire run`.
             */
            export async function connect(): Promise<AspireClientRpc> {
                const socketPath = process.env.REMOTE_APP_HOST_SOCKET_PATH;
                if (!socketPath) {
                    throw new Error(
                        'REMOTE_APP_HOST_SOCKET_PATH environment variable not set. ' +
                        'Run this application using `aspire run`.'
                    );
                }

                const authToken = process.env.ASPIRE_RPC_AUTH_TOKEN;
                if (!authToken) {
                    throw new Error(
                        'ASPIRE_RPC_AUTH_TOKEN environment variable not set. ' +
                        'Run this application using `aspire run`.'
                    );
                }

                const client = new AspireClientRpc(socketPath);
                await client.connect();
                await client.authenticate(authToken);

                return client;
            }

            /**
             * Creates a new distributed application builder.
             * This is the entry point for building Aspire applications.
             *
             * @param args - Optional command-line arguments to pass to the builder
             * @returns A DistributedApplicationBuilder instance
             *
             * @example
             * const builder = await createBuilder();
             * builder.addRedis("cache");
             * builder.addContainer("api", "mcr.microsoft.com/dotnet/samples:aspnetapp");
             * const app = await builder.build();
             * await app.run();
             */
            export async function createBuilder(args: string[] = process.argv.slice(2)): Promise<DistributedApplicationBuilder> {
                const client = await connect();
                const handle = await client.invokeCapability<{{builderHandle}}>(
                    '{{AtsConstants.CreateBuilderCapability}}',
                    { args }
                );
                return new DistributedApplicationBuilder(handle, client);
            }

            // Re-export commonly used types
            export { Handle, CapabilityError, registerCallback } from './transport.js';
            export { refExpr, ReferenceExpression } from './base.js';
            """);
        WriteLine();
    }

    private void GenerateGlobalErrorHandling()
    {
        WriteLine("""
            // ============================================================================
            // Global Error Handling
            // ============================================================================

            /**
             * Set up global error handlers to ensure the process exits properly on errors.
             * Node.js doesn't exit on unhandled rejections by default, so we need to handle them.
             */
            process.on('unhandledRejection', (reason: unknown) => {
                const error = reason instanceof Error ? reason : new Error(String(reason));

                if (reason instanceof CapabilityError) {
                    console.error(`\n❌ Capability Error: ${error.message}`);
                    console.error(`   Code: ${(reason as CapabilityError).code}`);
                    if ((reason as CapabilityError).capability) {
                        console.error(`   Capability: ${(reason as CapabilityError).capability}`);
                    }
                } else {
                    console.error(`\n❌ Unhandled Error: ${error.message}`);
                    if (error.stack) {
                        console.error(error.stack);
                    }
                }

                process.exit(1);
            });

            process.on('uncaughtException', (error: Error) => {
                console.error(`\n❌ Uncaught Exception: ${error.message}`);
                if (error.stack) {
                    console.error(error.stack);
                }
                process.exit(1);
            });
            """);
    }

    /// <summary>
    /// Generates handle wrapper registrations for all type classes and builder classes.
    /// This allows callback handles to be wrapped as typed instances.
    /// </summary>
    private void GenerateHandleWrapperRegistrations(List<BuilderModel> typeClasses, List<BuilderModel> resourceBuilders)
    {
        WriteLine();
        WriteLine("// ============================================================================");
        WriteLine("// Handle Wrapper Registrations");
        WriteLine("// ============================================================================");
        WriteLine();
        WriteLine("// Register wrapper factories for typed handle wrapping in callbacks");

        // Register type classes (context types like EnvironmentCallbackContext)
        foreach (var typeClass in typeClasses)
        {
            var className = _wrapperClassNames.GetValueOrDefault(typeClass.TypeId) ?? DeriveClassName(typeClass.TypeId);
            var handleType = GetHandleTypeName(typeClass.TypeId);
            WriteLine($"registerHandleWrapper('{typeClass.TypeId}', (handle, client) => new {className}(handle as {handleType}, client));");
        }

        // Register resource builder classes
        foreach (var builder in resourceBuilders)
        {
            var className = _wrapperClassNames.GetValueOrDefault(builder.TypeId) ?? DeriveClassName(builder.TypeId);
            var handleType = GetHandleTypeName(builder.TypeId);
            WriteLine($"registerHandleWrapper('{builder.TypeId}', (handle, client) => new {className}(handle as {handleType}, client));");
        }

        WriteLine();
    }

    /// <summary>
    /// Generates a type class (context type or wrapper type).
    /// Uses property-like object pattern for exposed properties.
    /// </summary>
    private void GenerateTypeClass(BuilderModel model)
    {
        var handleType = GetHandleTypeName(model.TypeId);
        var className = DeriveClassName(model.TypeId);

        WriteLine("// ============================================================================");
        WriteLine($"// {className}");
        WriteLine("// ============================================================================");
        WriteLine();

        // Separate capabilities by type using CapabilityKind enum
        var getters = model.Capabilities.Where(c => c.CapabilityKind == AtsCapabilityKind.PropertyGetter).ToList();
        var setters = model.Capabilities.Where(c => c.CapabilityKind == AtsCapabilityKind.PropertySetter).ToList();
        var contextMethods = model.Capabilities.Where(c => c.CapabilityKind == AtsCapabilityKind.InstanceMethod).ToList();
        var otherMethods = model.Capabilities.Where(c => c.CapabilityKind == AtsCapabilityKind.Method).ToList();

        WriteLine($"/**");
        WriteLine($" * Type class for {className}.");
        WriteLine($" */");
        WriteLine($"export class {className} {{");
        WriteLine($"    constructor(private _handle: {handleType}, private _client: AspireClientRpc) {{}}");
        WriteLine();
        WriteLine($"    /** Serialize for JSON-RPC transport */");
        WriteLine($"    toJSON(): MarshalledHandle {{ return this._handle.toJSON(); }}");
        WriteLine();

        // Group getters and setters by property name to create property-like objects
        var properties = GroupPropertiesByName(getters, setters);

        // Generate property-like objects
        foreach (var prop in properties)
        {
            GeneratePropertyLikeObject(prop.PropertyName, prop.Getter, prop.Setter);
        }

        // Generate context methods (instance methods from ExposeMethods=true)
        foreach (var method in contextMethods)
        {
            GenerateContextMethod(method);
        }

        // Generate other methods (wrapper-style)
        foreach (var method in otherMethods)
        {
            GenerateWrapperMethod(method);
        }

        WriteLine("}");
        WriteLine();
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
    /// Generates a property-like object with get and/or set methods.
    /// For dictionary types, generates a direct AspireDict field instead.
    /// </summary>
    private void GeneratePropertyLikeObject(string propertyName, AtsCapabilityInfo? getter, AtsCapabilityInfo? setter)
    {
        // Determine the return type from getter
        string returnType = "unknown";
        string? description = null;

        if (getter != null)
        {
            returnType = MapTypeRefToTypeScript(getter.ReturnType);
            description = getter.Description;

            // Check if this is a dictionary type - generate direct AspireDict field instead
            if (IsDictionaryType(getter.ReturnType))
            {
                GenerateDictionaryProperty(propertyName, getter);
                return;
            }

            // Check if return type is a wrapper class - use property-like object returning wrapper
            if (getter.ReturnType?.TypeId != null && _wrapperClassNames.TryGetValue(getter.ReturnType.TypeId, out var wrapperClassName))
            {
                GenerateWrapperPropertyObject(propertyName, getter, wrapperClassName);
                return;
            }
        }

        // Generate property-like object for scalar types
        if (!string.IsNullOrEmpty(description))
        {
            WriteLine($"    /** {description} */");
        }

        WriteLine($"    {propertyName} = {{");

        // Generate get method
        if (getter != null)
        {
            WriteLine($"        get: async (): Promise<{returnType}> => {{");
            WriteLine($"            return await this._client.invokeCapability<{returnType}>(");
            WriteLine($"                '{getter.CapabilityId}',");
            WriteLine($"                {{ context: this._handle }}");
            WriteLine("            );");
            WriteLine("        },");
        }

        // Generate set method
        if (setter != null)
        {
            var valueParam = setter.Parameters.FirstOrDefault(p => p.Name == "value");
            if (valueParam != null)
            {
                var valueType = MapTypeRefToTypeScript(valueParam.Type);
                WriteLine($"        set: async (value: {valueType}): Promise<void> => {{");
                WriteLine($"            await this._client.invokeCapability<void>(");
                WriteLine($"                '{setter.CapabilityId}',");
                WriteLine($"                {{ context: this._handle, value }}");
                WriteLine("            );");
                WriteLine("        }");
            }
        }

        WriteLine("    };");
        WriteLine();
    }

    /// <summary>
    /// Generates a property-like object that returns a wrapper class.
    /// </summary>
    private void GenerateWrapperPropertyObject(string propertyName, AtsCapabilityInfo getter, string wrapperClassName)
    {
        var handleType = GetHandleTypeName(getter.ReturnType!.TypeId);

        if (!string.IsNullOrEmpty(getter.Description))
        {
            WriteLine($"    /** {getter.Description} */");
        }

        WriteLine($"    {propertyName} = {{");
        WriteLine($"        get: async (): Promise<{wrapperClassName}> => {{");
        WriteLine($"            const handle = await this._client.invokeCapability<{handleType}>(");
        WriteLine($"                '{getter.CapabilityId}',");
        WriteLine($"                {{ context: this._handle }}");
        WriteLine("            );");
        WriteLine($"            return new {wrapperClassName}(handle, this._client);");
        WriteLine("        },");
        WriteLine("    };");
        WriteLine();
    }

    /// <summary>
    /// Checks if a type reference is a dictionary type.
    /// </summary>
    private static bool IsDictionaryType(AtsTypeRef? typeRef)
    {
        return typeRef?.Category == AtsTypeCategory.Dict;
    }

    /// <summary>
    /// Generates a direct AspireDict property for dictionary types.
    /// </summary>
    private void GenerateDictionaryProperty(string propertyName, AtsCapabilityInfo getter)
    {
        // Determine key and value types
        var keyType = "string";
        var valueType = "unknown";

        // Try to extract key and value types from Dict type
        if (getter.ReturnType?.KeyType != null)
        {
            keyType = MapTypeRefToTypeScript(getter.ReturnType.KeyType);
        }
        if (getter.ReturnType?.ValueType != null)
        {
            // Union types will be mapped correctly via MapTypeRefToTypeScript
            valueType = MapTypeRefToTypeScript(getter.ReturnType.ValueType);
        }

        var typeId = $"'{getter.CapabilityId.Replace(".get", "")}'";
        var getterCapabilityId = $"'{getter.CapabilityId}'";

        if (!string.IsNullOrEmpty(getter.Description))
        {
            WriteLine($"    /** {getter.Description} */");
        }

        // Generate a getter property that returns AspireDict
        // Pass the getter capability ID so AspireDict can lazily fetch the actual dictionary handle
        WriteLine($"    private _{propertyName}?: AspireDict<{keyType}, {valueType}>;");
        WriteLine($"    get {propertyName}(): AspireDict<{keyType}, {valueType}> {{");
        WriteLine($"        if (!this._{propertyName}) {{");
        WriteLine($"            this._{propertyName} = new AspireDict<{keyType}, {valueType}>(");
        WriteLine($"                this._handle,");
        WriteLine($"                this._client,");
        WriteLine($"                {typeId},");
        WriteLine($"                {getterCapabilityId}");
        WriteLine("            );");
        WriteLine("        }");
        WriteLine($"        return this._{propertyName};");
        WriteLine("    }");
        WriteLine();
    }

    /// <summary>
    /// Generates a context instance method (from ExposeMethods=true).
    /// </summary>
    private void GenerateContextMethod(AtsCapabilityInfo method)
    {
        // Use OwningTypeName if available to extract method name, otherwise parse from MethodName
        var methodName = !string.IsNullOrEmpty(method.OwningTypeName) && method.MethodName.Contains('.')
            ? method.MethodName[(method.MethodName.LastIndexOf('.') + 1)..]
            : method.MethodName;

        // Build parameter list (skip "context" parameter which is implicit)
        var paramDefs = new List<string>();
        var paramArgs = new List<string> { "context: this._handle" };

        foreach (var param in method.Parameters.Where(p => p.Name != "context"))
        {
            var tsType = MapParameterToTypeScript(param);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";
            paramDefs.Add($"{param.Name}{optional}: {tsType}");
            paramArgs.Add(param.Name);
        }

        var paramsString = string.Join(", ", paramDefs);
        var argsObject = $"{{ {string.Join(", ", paramArgs)} }}";

        // Determine return type
        var returnType = GetReturnTypeId(method) != null
            ? MapTypeRefToTypeScript(method.ReturnType)
            : "void";

        // Generate JSDoc
        if (!string.IsNullOrEmpty(method.Description))
        {
            WriteLine($"    /** {method.Description} */");
        }

        // Generate async method
        Write($"    async {methodName}(");
        Write(paramsString);
        WriteLine($"): Promise<{returnType}> {{");

        if (returnType == "void")
        {
            WriteLine($"        await this._client.invokeCapability<void>(");
        }
        else
        {
            WriteLine($"        return await this._client.invokeCapability<{returnType}>(");
        }
        WriteLine($"            '{method.CapabilityId}',");
        WriteLine($"            {argsObject}");
        WriteLine("        );");
        WriteLine("    }");
        WriteLine();
    }

    /// <summary>
    /// Generates a method on a wrapper class.
    /// </summary>
    private void GenerateWrapperMethod(AtsCapabilityInfo capability)
    {
        var methodName = GetTypeScriptMethodName(capability.MethodName);

        // Build parameter list
        var paramDefs = new List<string>();
        var paramArgs = new List<string>();

        // First arg is the handle (implicit via this._handle)
        // Use parameter name from the method signature - typically matches the type
        var firstParamName = AtsTypeMapping.GetParameterName(capability.TargetTypeId);
        paramArgs.Add($"{firstParamName}: this._handle");

        foreach (var param in capability.Parameters)
        {
            // Skip the first parameter if it matches the implicit handle parameter
            // (e.g., for context type properties, the context is passed as this._handle)
            if (param.Name == firstParamName)
            {
                continue;
            }

            var tsType = MapParameterToTypeScript(param);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";
            paramDefs.Add($"{param.Name}{optional}: {tsType}");
            paramArgs.Add(param.Name);
        }

        var paramsString = string.Join(", ", paramDefs);
        var argsObject = $"{{ {string.Join(", ", paramArgs)} }}";

        // Determine return type
        var returnType = MapTypeRefToTypeScript(capability.ReturnType);

        // Generate JSDoc
        if (!string.IsNullOrEmpty(capability.Description))
        {
            WriteLine($"    /** {capability.Description} */");
        }

        // Generate async method
        Write($"    async {methodName}(");
        Write(paramsString);
        WriteLine($"): Promise<{returnType}> {{");

        if (returnType == "void")
        {
            WriteLine($"        await this._client.invokeCapability<void>(");
        }
        else
        {
            WriteLine($"        return await this._client.invokeCapability<{returnType}>(");
        }
        WriteLine($"            '{capability.CapabilityId}',");
        WriteLine($"            {argsObject}");
        WriteLine("        );");
        WriteLine("    }");
        WriteLine();
    }

    // ============================================================================
    // Builder Model Helpers (replaces AtsBuilderModelFactory)
    // ============================================================================

    /// <summary>
    /// Groups capabilities by ExpandedTargetTypes to create builder models.
    /// Uses expansion to map interface targets to their concrete implementations.
    /// </summary>
    private static List<BuilderModel> CreateBuilderModels(List<AtsCapabilityInfo> capabilities)
    {
        // Group capabilities by expanded target type IDs
        // A capability targeting IResource with ExpandedTargetTypes = [RedisResource]
        // will be assigned to Aspire.Hosting.Redis/RedisResource (the concrete type)
        var capabilitiesByTypeId = new Dictionary<string, List<AtsCapabilityInfo>>();

        // Track whether each typeId is an interface (from ExpandedTargetTypes metadata)
        var typeIdIsInterface = new Dictionary<string, bool>();

        foreach (var cap in capabilities)
        {
            var targetTypeRef = cap.TargetType;
            var targetTypeId = cap.TargetTypeId;
            if (targetTypeRef == null || string.IsNullOrEmpty(targetTypeId))
            {
                // Entry point methods - handled separately
                continue;
            }

            // Use category-based check instead of string parsing
            if (targetTypeRef.Category != AtsTypeCategory.Handle)
            {
                continue;
            }

            // Use expanded types if available, otherwise fall back to the original target
            var expandedTypes = cap.ExpandedTargetTypes;
            if (expandedTypes is { Count: > 0 })
            {
                foreach (var expandedType in expandedTypes)
                {
                    if (!capabilitiesByTypeId.TryGetValue(expandedType.TypeId, out var list))
                    {
                        list = [];
                        capabilitiesByTypeId[expandedType.TypeId] = list;
                        // Store whether this expanded type is an interface
                        typeIdIsInterface[expandedType.TypeId] = expandedType.IsInterface;
                    }
                    list.Add(cap);
                }
            }
            else
            {
                // No expansion - use original target (concrete type)
                if (!capabilitiesByTypeId.TryGetValue(targetTypeId, out var list))
                {
                    list = [];
                    capabilitiesByTypeId[targetTypeId] = list;
                    // Store whether this target type is an interface
                    typeIdIsInterface[targetTypeId] = targetTypeRef.IsInterface;
                }
                list.Add(cap);
            }
        }

        // Create a builder for each concrete type with its specific capabilities
        var builders = new List<BuilderModel>();
        foreach (var (typeId, typeCapabilities) in capabilitiesByTypeId)
        {
            var builderClassName = DeriveClassName(typeId);

            // Get IsInterface from the tracked metadata (based on target type, not return type)
            var isInterface = typeIdIsInterface.GetValueOrDefault(typeId, false);

            // Deduplicate capabilities by CapabilityId to avoid duplicate methods
            var uniqueCapabilities = typeCapabilities
                .GroupBy(c => c.CapabilityId)
                .Select(g => g.First())
                .ToList();

            var builder = new BuilderModel
            {
                TypeId = typeId,
                BuilderClassName = builderClassName,
                Capabilities = uniqueCapabilities,
                IsInterface = isInterface
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
    /// Gets entry point capabilities (those without TargetTypeId).
    /// </summary>
    private static List<AtsCapabilityInfo> GetEntryPointCapabilities(List<AtsCapabilityInfo> capabilities)
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
        // and array types like "string[]" -> "stringArray"
        typeName = typeName
            .Replace("[]", "Array", StringComparison.Ordinal)
            .Replace("<", "", StringComparison.Ordinal)
            .Replace(">", "", StringComparison.Ordinal)
            .Replace(",", "", StringComparison.Ordinal);

        return $"{typeName}Handle";
    }

    /// <summary>
    /// Extracts the simple type name from a type ID.
    /// </summary>
    /// <example>
    /// "Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource" → "IResource"
    /// "Aspire.Hosting/Aspire.Hosting.DistributedApplication" → "DistributedApplication"
    /// </example>
    private static string ExtractSimpleTypeName(string typeId)
    {
        var slashIndex = typeId.LastIndexOf('/');
        var fullTypeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;

        var dotIndex = fullTypeName.LastIndexOf('.');
        return dotIndex >= 0 ? fullTypeName[(dotIndex + 1)..] : fullTypeName;
    }
}
