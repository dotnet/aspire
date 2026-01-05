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
/// <list type="table">
///   <listheader>
///     <term>ATS Type ID</term>
///     <description>TypeScript Type</description>
///   </listheader>
///   <item><term><c>aspire/Builder</c></term><description><c>BuilderHandle</c> (alias for <c>Handle&lt;'aspire/Builder'&gt;</c>)</description></item>
///   <item><term><c>aspire/Application</c></term><description><c>ApplicationHandle</c></description></item>
///   <item><term><c>aspire/ExecutionContext</c></term><description><c>ExecutionContextHandle</c></description></item>
///   <item><term><c>aspire/Redis</c></term><description><c>RedisBuilderHandle</c></description></item>
///   <item><term><c>aspire/Container</c></term><description><c>ContainerBuilderHandle</c></description></item>
///   <item><term><c>aspire/IResource</c></term><description><c>IResourceHandle</c></description></item>
///   <item><term><c>aspire/IResourceWithEnvironment</c></term><description><c>IResourceWithEnvironmentHandle</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Handle Type Naming Rules:</b>
/// <list type="bullet">
///   <item><description>Core types (<c>aspire/Builder</c>, <c>aspire/Application</c>): Use type name + "Handle"</description></item>
///   <item><description>Interface types (<c>aspire/IResource*</c>): Use interface name + "Handle" (keep the I prefix)</description></item>
///   <item><description>Resource types (<c>aspire/Redis</c>, etc.): Use type name + "BuilderHandle"</description></item>
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
///   <item><description><c>aspire/Redis</c> → <c>RedisBuilder</c> class with <c>RedisBuilderPromise</c> thenable wrapper</description></item>
///   <item><description><c>aspire/IResource</c> → <c>ResourceBuilderBase</c> abstract class (interface types get "BuilderBase" suffix)</description></item>
///   <item><description>Concrete builders extend interface builders based on type hierarchy</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Method Naming:</b>
/// <list type="bullet">
///   <item><description>Derived from capability ID: <c>aspire.redis/addRedis@1</c> → <c>addRedis</c></description></item>
///   <item><description>Can be overridden via <c>[AspireExport(MethodName = "...")]</c></description></item>
///   <item><description>TypeScript uses camelCase (the canonical form from capability IDs)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AtsTypeScriptCodeGenerator : ICodeGenerator
{
    private TextWriter _writer = null!;

    /// <inheritdoc />
    public string Language => "TypeScript";

    /// <inheritdoc />
    public Dictionary<string, string> GenerateDistributedApplication(IReadOnlyList<AtsCapabilityInfo> capabilities)
    {
        var files = new Dictionary<string, string>();

        // Add embedded resource files (types.ts, RemoteAppHostClient.ts)
        files["types.ts"] = GetEmbeddedResource("types.ts");
        files["RemoteAppHostClient.ts"] = GetEmbeddedResource("RemoteAppHostClient.ts");

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
            // Capabilities are versioned endpoints like 'aspire/createBuilder@1'.
            //
            // GENERATED CODE - DO NOT EDIT

            import {
                RemoteAppHostClient,
                Handle,
                CapabilityError,
                registerCallback,
                wrapIfHandle
            } from './RemoteAppHostClient.js';
            """);
        WriteLine();

        // Get builder models (flattened - each builder has all its applicable capabilities)
        var allBuilders = CreateBuilderModels(capabilities);
        var entryPoints = GetEntryPointCapabilities(capabilities);

        // Extract the DistributedApplicationBuilder's capabilities (TargetTypeId = "aspire/Builder")
        var distributedAppBuilder = allBuilders.FirstOrDefault(b => b.TypeId == AtsTypeMapping.TypeIds.Builder);
        var builderMethods = distributedAppBuilder?.Capabilities ?? [];

        // Resource builders are all other builders (not the main builder)
        var builders = allBuilders.Where(b => b.TypeId != AtsTypeMapping.TypeIds.Builder).ToList();

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
            if (!string.IsNullOrEmpty(cap.ReturnTypeId) && cap.ReturnTypeId.StartsWith("aspire/", StringComparison.Ordinal))
            {
                typeIds.Add(cap.ReturnTypeId);
            }
            // Add parameter type IDs (for types like IResourceBuilder<IResource>)
            foreach (var param in cap.Parameters)
            {
                if (!string.IsNullOrEmpty(param.AtsTypeId) && param.AtsTypeId.StartsWith("aspire/", StringComparison.Ordinal))
                {
                    typeIds.Add(param.AtsTypeId);
                }
            }
        }

        // Add core type IDs
        typeIds.Add(AtsTypeMapping.TypeIds.Builder);
        typeIds.Add(AtsTypeMapping.TypeIds.Application);
        typeIds.Add(AtsTypeMapping.TypeIds.ExecutionContext);
        typeIds.Add(AtsTypeMapping.TypeIds.EnvironmentContext);
        typeIds.Add(AtsTypeMapping.TypeIds.EndpointReference);

        // Generate handle type aliases
        GenerateHandleTypeAliases(typeIds);

        // Separate resource builders from wrapper types
        // Resource builders: aspire/IResource*, aspire/Container, aspire/Executable, etc.
        // Wrapper types: aspire/Configuration, aspire/HostEnvironment, aspire/ExecutionContext
        var resourceBuilders = builders.Where(b => AtsTypeMapping.IsResourceBuilderType(b.TypeId)).ToList();
        var wrapperTypes = builders.Where(b => !AtsTypeMapping.IsResourceBuilderType(b.TypeId)).ToList();

        // Generate wrapper classes first (Configuration, Environment, ExecutionContext)
        foreach (var wrapperType in wrapperTypes)
        {
            GenerateWrapperClass(wrapperType);
        }

        // Generate DistributedApplicationBuilder class
        GenerateDistributedApplicationBuilder(builderMethods, resourceBuilders, wrapperTypes);

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
        WriteLine("// Handle Type Aliases");
        WriteLine("// ============================================================================");
        WriteLine();

        foreach (var typeId in typeIds.OrderBy(t => t))
        {
            var handleName = GetHandleTypeName(typeId);
            var description = GetTypeDescription(typeId);
            WriteLine($"/** {description} */");
            WriteLine($"export type {handleName} = Handle<'{typeId}'>;");
            WriteLine();
        }
    }

    private static string GetTypeDescription(string typeId)
    {
        return typeId switch
        {
            _ when typeId == AtsTypeMapping.TypeIds.Builder => "Handle to IDistributedApplicationBuilder",
            _ when typeId == AtsTypeMapping.TypeIds.Application => "Handle to DistributedApplication",
            _ when typeId == AtsTypeMapping.TypeIds.ExecutionContext => "Handle to DistributedApplicationExecutionContext",
            _ when typeId == AtsTypeMapping.TypeIds.EnvironmentContext => "Handle to EnvironmentCallbackContext",
            _ when typeId == AtsTypeMapping.TypeIds.EndpointReference => "Handle to EndpointReference",
            _ when typeId == AtsTypeMapping.TypeIds.Container => "Handle to IResourceBuilder<ContainerResource>",
            _ when typeId.StartsWith("aspire/IResource", StringComparison.Ordinal) =>
                $"Handle to IResourceBuilder<{typeId[7..]}>",
            _ => $"Handle to IResourceBuilder<{typeId[7..]}Resource>"
        };
    }

    private void GenerateDistributedApplicationBuilder(List<AtsCapabilityInfo> methods, List<BuilderModel> resourceBuilders, List<BuilderModel> wrapperTypes)
    {
        WriteLine("// ============================================================================");
        WriteLine("// DistributedApplicationBuilder");
        WriteLine("// ============================================================================");
        WriteLine();

        // First generate DistributedApplication class for build() return type
        WriteLine("""
            /**
             * Represents a built distributed application ready to run.
             */
            export class DistributedApplication {
                constructor(
                    private _handle: ApplicationHandle,
                    private _client: AspireClient
                ) {}

                /** Gets the underlying handle */
                get handle(): ApplicationHandle { return this._handle; }

                /**
                 * Runs the distributed application, starting all configured resources.
                 */
                async run(): Promise<void> {
                    await this._client.client.invokeCapability<void>(
                        'aspire/run@1',
                        { app: this._handle }
                    );
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
                 * Chains through the promise for fluent usage: await builder.build().run()
                 */
                run(): Promise<void> {
                    return this._promise.then(app => app.run());
                }
            }
            """);
        WriteLine();

        // Now generate DistributedApplicationBuilder
        WriteLine("""
            /**
             * Builder for creating distributed applications.
             * Use createBuilder() to get an instance.
             */
            export class DistributedApplicationBuilder {
                constructor(
                    private _handle: BuilderHandle,
                    private _client: AspireClient
                ) {}

                /** Gets the underlying handle */
                get handle(): BuilderHandle { return this._handle; }

                /** Gets the AspireClient for invoking capabilities */
                get client(): AspireClient { return this._client; }

                /** @internal - actual async implementation */
                async _buildInternal(): Promise<DistributedApplication> {
                    const handle = await this._client.client.invokeCapability<ApplicationHandle>(
                        'aspire/build@1',
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
            .Where(m => !string.IsNullOrEmpty(m.ReturnTypeId) &&
                        AtsTypeMapping.IsWrapperType(m.ReturnTypeId) &&
                        m.Parameters.Count == 0)  // Property getters have no additional params
            .ToList();

        var otherMethods = methods.Except(propertyMethods).ToList();

        // Generate property-style getters for wrapper types
        foreach (var capability in propertyMethods)
        {
            GenerateBuilderPropertyGetter(capability);
        }

        // Generate methods that extend IDistributedApplicationBuilder
        foreach (var capability in otherMethods)
        {
            GenerateDistributedApplicationBuilderMethod(capability, resourceBuilders, wrapperTypes);
        }

        WriteLine("}");
        WriteLine();
    }

    private void GenerateBuilderPropertyGetter(AtsCapabilityInfo capability)
    {
        var returnTypeId = capability.ReturnTypeId!;
        var wrapperClassName = DeriveWrapperClassName(returnTypeId);
        var handleType = GetHandleTypeName(returnTypeId);

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
        WriteLine($"        return this._client.client.invokeCapability<{handleType}>(");
        WriteLine($"            '{capability.CapabilityId}',");
        WriteLine("            { builder: this._handle }");
        WriteLine($"        ).then(handle => new {wrapperClassName}(handle, this._client));");
        WriteLine("    }");
    }

    private void GenerateDistributedApplicationBuilderMethod(AtsCapabilityInfo capability, List<BuilderModel> resourceBuilders, List<BuilderModel> wrapperTypes)
    {
        var methodName = capability.MethodName;

        // Build parameter list (builder is implicit via this._handle)
        var paramDefs = new List<string>();
        var paramArgs = new List<string> { "builder: this._handle" };

        foreach (var param in capability.Parameters)
        {
            var tsType = MapAtsTypeToTypeScript(param.AtsTypeId, param.IsCallback);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";
            paramDefs.Add($"{param.Name}{optional}: {tsType}");
            paramArgs.Add(param.Name);
        }

        var paramsString = string.Join(", ", paramDefs);
        var argsObject = $"{{ {string.Join(", ", paramArgs)} }}";

        // Check if return type is a wrapper type
        BuilderModel? wrapperInfo = null;
        if (!string.IsNullOrEmpty(capability.ReturnTypeId) && AtsTypeMapping.IsWrapperType(capability.ReturnTypeId))
        {
            wrapperInfo = wrapperTypes.FirstOrDefault(w => w.TypeId == capability.ReturnTypeId);
        }

        // Determine return type for resource builders
        BuilderModel? builderInfo = null;
        if (wrapperInfo == null && !string.IsNullOrEmpty(capability.ReturnTypeId) && capability.ReturnsBuilder)
        {
            builderInfo = resourceBuilders.FirstOrDefault(b => b.TypeId == capability.ReturnTypeId && !b.IsInterface);
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
            var handleType = GetHandleTypeName(capability.ReturnTypeId!);
            Write($"    {methodName}(");
            Write(paramsString);
            WriteLine($"): {builderInfo.BuilderClassName}Promise {{");
            WriteLine($"        const promise = this._client.client.invokeCapability<{handleType}>(");
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsObject}");
            WriteLine($"        ).then(handle => new {builderInfo.BuilderClassName}(handle, this._client));");
            WriteLine($"        return new {builderInfo.BuilderClassName}Promise(promise);");
            WriteLine("    }");
        }
        else if (!string.IsNullOrEmpty(capability.ReturnTypeId))
        {
            // Returns raw handle or value
            var returnType = MapAtsTypeToTypeScript(capability.ReturnTypeId, false);
            Write($"    async {methodName}(");
            Write(paramsString);
            WriteLine($"): Promise<{returnType}> {{");
            WriteLine($"        return await this._client.client.invokeCapability<{returnType}>(");
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
            WriteLine($"        await this._client.client.invokeCapability<void>(");
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

        // Generate builder class (no inheritance - capabilities are flattened)
        WriteLine($"export class {builder.BuilderClassName} {{");

        // Constructor
        WriteLine($"    constructor(protected _handle: {handleType}, protected _client: AspireClient) {{}}");
        WriteLine();

        // Handle getter
        WriteLine("    /** Gets the underlying handle */");
        WriteLine($"    get handle(): {handleType} {{ return this._handle; }}");
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
            var tsType = MapAtsTypeToTypeScript(param.AtsTypeId, param.IsCallback);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";

            paramDefs.Add($"{param.Name}{optional}: {tsType}");

            if (param.IsCallback)
            {
                // Callbacks need to be wrapped with registerCallback
                paramArgs.Add($"callback: {param.Name}Id");
            }
            else
            {
                paramArgs.Add($"{param.Name}");
            }
        }

        var paramsString = string.Join(", ", paramDefs);
        var argsString = paramArgs.Count > 0 ? $"{{ builder: this._handle, {string.Join(", ", paramArgs)} }}" : "{ builder: this._handle }";

        // Determine return type
        var returnHandle = !string.IsNullOrEmpty(capability.ReturnTypeId) && capability.ReturnsBuilder
            ? GetHandleTypeName(capability.ReturnTypeId)
            : "void";
        var returnsBuilder = capability.ReturnsBuilder;

        // Generate internal async method
        WriteLine($"    /** @internal */");
        Write($"    async {internalMethodName}(");
        Write(paramsString);
        Write($"): Promise<{builder.BuilderClassName}> {{");
        WriteLine();

        // Handle callback registration if any
        var callbackParams = capability.Parameters.Where(p => p.IsCallback).ToList();
        foreach (var callbackParam in callbackParams)
        {
            WriteLine($"        const {callbackParam.Name}Id = registerCallback(async (contextData: unknown) => {{");
            WriteLine($"            const context = wrapIfHandle(contextData) as EnvironmentContextHandle;");
            WriteLine($"            await {callbackParam.Name}(context);");
            WriteLine("        });");
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
                var tsType = MapAtsTypeToTypeScript(p.AtsTypeId, p.IsCallback);
                var optional = p.IsOptional || p.IsNullable ? "?" : "";
                return $"{p.Name}{optional}: {tsType}";
            });

            var paramsString = string.Join(", ", paramDefs);
            var argsString = string.Join(", ", capability.Parameters.Select(p => p.Name));

            if (!string.IsNullOrEmpty(capability.Description))
            {
                WriteLine($"    /** {capability.Description} */");
            }
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
            WriteLine();
        }

        WriteLine("}");
        WriteLine();
    }

    private void GenerateAspireClient(List<AtsCapabilityInfo> entryPoints, List<BuilderModel> builders)
    {
        WriteLine("// ============================================================================");
        WriteLine("// AspireClient - Entry point and factory methods");
        WriteLine("// ============================================================================");
        WriteLine();

        WriteLine("""
            /**
             * High-level Aspire client that provides typed access to ATS capabilities.
             */
            export class AspireClient {
                constructor(private readonly rpc: RemoteAppHostClient) {}

                /** Get the underlying RPC client */
                get client(): RemoteAppHostClient {
                    return this.rpc;
                }

                /**
                 * Invokes a capability by ID with the given arguments.
                 * Use this for capabilities not exposed as typed methods.
                 */
                async invokeCapability<T>(
                    capabilityId: string,
                    args?: Record<string, unknown>
                ): Promise<T> {
                    return await this.rpc.invokeCapability<T>(capabilityId, args ?? {});
                }

                /**
                 * Lists all available capabilities from the server.
                 */
                async getCapabilities(): Promise<string[]> {
                    return await this.rpc.getCapabilities();
                }
            """);

        // Generate entry point methods
        foreach (var capability in entryPoints)
        {
            GenerateClientMethod(capability, builders);
        }

        WriteLine("}");
        WriteLine();
    }

    private void GenerateClientMethod(AtsCapabilityInfo capability, List<BuilderModel> builders)
    {
        var methodName = capability.MethodName;

        // Build parameter list
        var paramDefs = new List<string>();
        var paramArgs = new List<string>();

        foreach (var param in capability.Parameters)
        {
            var tsType = MapAtsTypeToTypeScript(param.AtsTypeId, param.IsCallback);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";
            paramDefs.Add($"{param.Name}{optional}: {tsType}");
            paramArgs.Add(param.Name);
        }

        var paramsString = string.Join(", ", paramDefs);
        var argsObject = paramArgs.Count > 0
            ? $"{{ {string.Join(", ", paramArgs)} }}"
            : "{}";

        // Determine return type
        string returnType;
        var returnsBuilder = false;
        BuilderModel? builderInfo = null;

        if (!string.IsNullOrEmpty(capability.ReturnTypeId))
        {
            if (capability.ReturnsBuilder)
            {
                builderInfo = builders.FirstOrDefault(b => b.TypeId == capability.ReturnTypeId && !b.IsInterface);
                if (builderInfo != null)
                {
                    returnType = $"{builderInfo.BuilderClassName}Promise";
                    returnsBuilder = true;
                }
                else
                {
                    returnType = GetHandleTypeName(capability.ReturnTypeId);
                }
            }
            else
            {
                returnType = MapAtsTypeToTypeScript(capability.ReturnTypeId, false);
            }
        }
        else
        {
            returnType = "void";
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
        if (returnsBuilder && builderInfo != null)
        {
            // Returns a thenable wrapper
            var handleType = GetHandleTypeName(capability.ReturnTypeId!);
            Write($"    {methodName}(");
            Write(paramsString);
            WriteLine($"): {returnType} {{");
            WriteLine($"        const promise = this.rpc.invokeCapability<{handleType}>(");
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsObject}");
            WriteLine($"        ).then(handle => new {builderInfo.BuilderClassName}(handle, this));");
            WriteLine($"        return new {builderInfo.BuilderClassName}Promise(promise);");
            WriteLine("    }");
        }
        else
        {
            // Returns raw value
            Write($"    async {methodName}(");
            Write(paramsString);
            WriteLine($"): Promise<{returnType}> {{");
            if (returnType == "void")
            {
                WriteLine($"        await this.rpc.invokeCapability<void>(");
            }
            else
            {
                WriteLine($"        return await this.rpc.invokeCapability<{returnType}>(");
            }
            WriteLine($"            '{capability.CapabilityId}',");
            WriteLine($"            {argsObject}");
            WriteLine("        );");
            WriteLine("    }");
        }
    }

    private static string MapAtsTypeToTypeScript(string atsTypeId, bool isCallback)
    {
        if (isCallback)
        {
            return "(context: EnvironmentContextHandle) => Promise<void>";
        }

        return atsTypeId switch
        {
            "string" => "string",
            "number" => "number",
            "boolean" => "boolean",
            "any" => "unknown",
            "callback" => "(context: EnvironmentContextHandle) => Promise<void>",
            _ when atsTypeId.StartsWith("aspire/", StringComparison.Ordinal) =>
                GetHandleTypeName(atsTypeId),
            _ when atsTypeId.EndsWith("[]", StringComparison.Ordinal) =>
                $"{MapAtsTypeToTypeScript(atsTypeId[..^2], false)}[]",
            _ => "unknown"
        };
    }

    private void GenerateConnectionHelper()
    {
        WriteLine("""
            // ============================================================================
            // Connection Helper
            // ============================================================================

            /**
             * Creates and connects an AspireClient.
             * Reads connection info from environment variables set by `aspire run`.
             */
            export async function connect(): Promise<AspireClient> {
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

                const rpc = new RemoteAppHostClient(socketPath);
                await rpc.connect();
                await rpc.authenticate(authToken);

                return new AspireClient(rpc);
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
                const handle = await client.client.invokeCapability<BuilderHandle>(
                    'aspire/createBuilder@1',
                    { args }
                );
                return new DistributedApplicationBuilder(handle, client);
            }

            // Re-export commonly used types
            export { Handle, CapabilityError, registerCallback, refExpr, ReferenceExpression } from './RemoteAppHostClient.js';
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
    /// Generates a simple wrapper class for non-resource types like Configuration, Environment.
    /// </summary>
    private void GenerateWrapperClass(BuilderModel wrapperType)
    {
        var handleType = GetHandleTypeName(wrapperType.TypeId);
        var className = DeriveWrapperClassName(wrapperType.TypeId);

        WriteLine("// ============================================================================");
        WriteLine($"// {className}");
        WriteLine("// ============================================================================");
        WriteLine();

        WriteLine($"/**");
        WriteLine($" * Wrapper class for {wrapperType.TypeId}.");
        WriteLine($" */");
        WriteLine($"export class {className} {{");
        WriteLine($"    constructor(private _handle: {handleType}, private _client: AspireClient) {{}}");
        WriteLine();
        WriteLine($"    /** Gets the underlying handle */");
        WriteLine($"    get handle(): {handleType} {{ return this._handle; }}");
        WriteLine();

        // Generate methods for each capability
        foreach (var capability in wrapperType.Capabilities)
        {
            GenerateWrapperMethod(capability);
        }

        WriteLine("}");
        WriteLine();
    }

    /// <summary>
    /// Generates a method on a wrapper class.
    /// </summary>
    private void GenerateWrapperMethod(AtsCapabilityInfo capability)
    {
        var methodName = capability.MethodName;

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

            var tsType = MapAtsTypeToTypeScript(param.AtsTypeId, param.IsCallback);
            var optional = param.IsOptional || param.IsNullable ? "?" : "";
            paramDefs.Add($"{param.Name}{optional}: {tsType}");
            paramArgs.Add(param.Name);
        }

        var paramsString = string.Join(", ", paramDefs);
        var argsObject = $"{{ {string.Join(", ", paramArgs)} }}";

        // Determine return type
        var returnType = MapAtsTypeToTypeScript(capability.ReturnTypeId ?? "void", false);

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
            WriteLine($"        await this._client.client.invokeCapability<void>(");
        }
        else
        {
            WriteLine($"        return await this._client.client.invokeCapability<{returnType}>(");
        }
        WriteLine($"            '{capability.CapabilityId}',");
        WriteLine($"            {argsObject}");
        WriteLine("        );");
        WriteLine("    }");
        WriteLine();
    }

    /// <summary>
    /// Derives a wrapper class name from a type ID.
    /// </summary>
    private static string DeriveWrapperClassName(string typeId)
    {
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;

        return typeName switch
        {
            "HostEnvironment" => "Environment",
            "ExecutionContext" => "ExecutionContext",
            "EnvironmentContext" => "EnvironmentContext",
            _ => typeName
        };
    }

    // ============================================================================
    // Builder Model Helpers (replaces AtsBuilderModelFactory)
    // ============================================================================

    /// <summary>
    /// Groups capabilities by ExpandedTargetTypeIds to create builder models.
    /// Uses expansion to map interface targets to their concrete implementations.
    /// </summary>
    private static List<BuilderModel> CreateBuilderModels(List<AtsCapabilityInfo> capabilities)
    {
        // Group capabilities by expanded target type IDs
        // A capability targeting aspire/IResource with ExpandedTargetTypeIds = [aspire/TestRedis]
        // will be assigned to aspire/TestRedis (the concrete type)
        var capabilitiesByTypeId = new Dictionary<string, List<AtsCapabilityInfo>>();

        foreach (var cap in capabilities)
        {
            var targetType = cap.TargetTypeId;
            if (string.IsNullOrEmpty(targetType))
            {
                // Entry point methods - handled separately
                continue;
            }

            if (targetType == AtsTypeMapping.TypeIds.Builder ||
                targetType == AtsTypeMapping.TypeIds.Application)
            {
                // Builder/Application methods handled separately
                continue;
            }

            if (!targetType.StartsWith("aspire/", StringComparison.Ordinal))
            {
                continue;
            }

            // Use expanded type IDs if available, otherwise fall back to the original target
            var expandedTypeIds = cap.ExpandedTargetTypeIds;
            if (expandedTypeIds is { Count: > 0 })
            {
                foreach (var expandedTypeId in expandedTypeIds)
                {
                    if (!capabilitiesByTypeId.TryGetValue(expandedTypeId, out var list))
                    {
                        list = [];
                        capabilitiesByTypeId[expandedTypeId] = list;
                    }
                    list.Add(cap);
                }
            }
            else
            {
                // No expansion - use original target (concrete type)
                if (!capabilitiesByTypeId.TryGetValue(targetType, out var list))
                {
                    list = [];
                    capabilitiesByTypeId[targetType] = list;
                }
                list.Add(cap);
            }
        }

        // Create a builder for each concrete type with its specific capabilities
        var builders = new List<BuilderModel>();
        foreach (var (typeId, typeCapabilities) in capabilitiesByTypeId)
        {
            var builderClassName = DeriveBuilderClassName(typeId);
            var isInterface = IsInterfaceType(typeId);

            var builder = new BuilderModel
            {
                TypeId = typeId,
                BuilderClassName = builderClassName,
                Capabilities = typeCapabilities,
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
    /// Derives the builder class name from an ATS type ID.
    /// </summary>
    private static string DeriveBuilderClassName(string typeId)
    {
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;

        // Handle interface types
        if (typeName.StartsWith("IResource", StringComparison.Ordinal))
        {
            typeName = typeName[1..]; // Remove leading 'I'
            return $"{typeName}BuilderBase";
        }

        return $"{typeName}Builder";
    }

    /// <summary>
    /// Gets the handle type alias name for a type ID.
    /// </summary>
    private static string GetHandleTypeName(string typeId)
    {
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;

        // Special cases for core types
        if (typeName == "Builder")
        {
            return "BuilderHandle";
        }
        if (typeName == "Application")
        {
            return "ApplicationHandle";
        }
        if (typeName == "ExecutionContext")
        {
            return "ExecutionContextHandle";
        }

        // For interface types, keep the I prefix
        if (typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]))
        {
            return $"{typeName}Handle";
        }

        return $"{typeName}BuilderHandle";
    }

    /// <summary>
    /// Checks if a type ID represents an interface type.
    /// </summary>
    private static bool IsInterfaceType(string typeId)
    {
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;
        return typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]);
    }
}
