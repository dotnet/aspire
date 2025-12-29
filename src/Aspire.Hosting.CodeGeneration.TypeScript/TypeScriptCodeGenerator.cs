// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.TypeScript;

/// <summary>
/// Generates TypeScript code from the Aspire application model with rich type information.
/// Produces instance methods on DistributedApplicationBuilder and resource-specific builder classes.
/// </summary>
public sealed class TypeScriptCodeGenerator : CodeGeneratorVisitor
{
    // Args class tracking - maps method to class name, and class name to definition
    private readonly Dictionary<RoMethod, string> _argsClassNameByMethod = [];
    private readonly Dictionary<string, string> _argsClassDefinitions = [];

    // Types that need proxy wrapper classes (all complex types get proxies)
    private readonly HashSet<string> _proxyTypes = [];
    private readonly Dictionary<string, RoType> _proxyTypesByName = [];

    // Thenable method buffer - populated during EmitExtensionMethod, emitted at class end
    private readonly List<MethodOverload> _thenableMethodBuffer = [];
    private ResourceModel? _currentResource;

    /// <inheritdoc />
    public override string Language => "TypeScript";

    /// <inheritdoc />
    protected override string MainFileName => "distributed-application.ts";

    /// <inheritdoc />
    protected override void AddEmbeddedResources(Dictionary<string, string> files)
    {
        files["types.ts"] = GetEmbeddedResource("types.ts");
        files["RemoteAppHostClient.ts"] = GetEmbeddedResource("RemoteAppHostClient.ts");
    }

    /// <summary>
    /// Gets the package.json template content.
    /// </summary>
    public static string GetPackageJsonTemplate()
    {
        return GetEmbeddedResource("package.json");
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

    // === Emit method overrides (called by base class traversal) ===

    /// <inheritdoc />
    protected override void EmitHeader()
    {
        Writer.WriteLine("""
        import { RemoteAppHostClient, registerCallback, DotNetProxy, ListProxy, wrapIfProxy } from './RemoteAppHostClient.js';

        // Get socket path from environment variable (set by aspire run)
        const socketPath = process.env.REMOTE_APP_HOST_SOCKET_PATH;
        if (!socketPath) {
            throw new Error('REMOTE_APP_HOST_SOCKET_PATH environment variable not set. Please run with "aspire run".');
        }

        const client = new RemoteAppHostClient(socketPath);
        """);
    }

    /// <inheritdoc />
    protected override void EmitCreateBuilderFunction()
    {
        GenerateCreateBuilderFunction(Writer, Model);
    }

    /// <inheritdoc />
    protected override void EmitDistributedApplicationClass()
    {
        GenerateDistributedApplicationClass(Writer, Model);
    }

    /// <inheritdoc />
    protected override void EmitEnumType(RoType enumType)
    {
        Writer.WriteLine();
        Writer.WriteLine($$"""
            export enum {{enumType.Name}} {
              {{string.Join(",\n  ", enumType.GetEnumNames().Select(x => $"{x} = \"{x}\""))}}
            }
            """);
    }

    /// <inheritdoc />
    protected override void EmitBuilderBaseClass()
    {
        GenerateBuilderBaseClass(Writer, Model);
    }

    /// <inheritdoc />
    protected override void EmitBuilderClassStart()
    {
        Writer.WriteLine("export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {");
    }

    /// <inheritdoc />
    protected override void EmitBuilderClassEnd()
    {
        Writer.WriteLine("}");
    }

    /// <inheritdoc />
    protected override void EmitModelClasses()
    {
        GenerateModelClasses(Writer, Model);
    }

    /// <inheritdoc />
    protected override void EmitAdditionalClasses()
    {
        GenerateParameterClasses(Writer);
        GenerateCallbackProxyClasses(Writer, Model);
    }

    // Use base class traversal for VisitBuilderModel, VisitIntegration, VisitResource

    /// <inheritdoc />
    protected override void EmitProxyClassStart(RoType type, ProxyTypeModel proxyModel)
    {
        Writer.WriteLine();
        Writer.WriteLine($$"""
            /**
             * Proxy for {{type.Name}}.
             */
            export class {{proxyModel.ProxyClassName}} {
              constructor(private _proxy: DotNetProxy) {}

              get proxy(): DotNetProxy { return this._proxy; }
            """);
    }

    /// <inheritdoc />
    protected override void EmitProxyHelperMethods(ProxyTypeModel proxyModel)
    {
        // Generate helper methods
        foreach (var helper in proxyModel.HelperMethods)
        {
            var paramsStr = string.Join(", ", helper.Parameters.Select(p => $"{p.Name}: {p.Type}"));

            Writer.WriteLine($$"""

              /**
               * {{helper.Documentation ?? helper.Name}}
               */
              async {{CamelCase(helper.Name)}}({{paramsStr}}): Promise<{{helper.ReturnType}}> {
                {{helper.Body}}
              }
            """);
        }
    }

    /// <inheritdoc />
    protected override void EmitProxyClassEnd(RoType type, ProxyTypeModel proxyModel)
    {
        Writer.WriteLine("}");
    }

    /// <inheritdoc />
    protected override void EmitResourceBuilderClassStart(ResourceModel resource)
    {
        _currentResource = resource;
        _thenableMethodBuffer.Clear();

        var resourceName = SanitizeClassName(resource.ResourceType.Name);
        Writer.WriteLine();
        Writer.WriteLine($$"""
                export class {{resourceName}}Builder {
                  constructor(protected _proxy: DotNetProxy) {}

                  /** Gets the underlying proxy */
                  get proxy(): DotNetProxy { return this._proxy; }
                """);
    }

    /// <inheritdoc />
    protected override void EmitResourceBuilderClassEnd(ResourceModel resource)
    {
        Writer.WriteLine("}");

        // Emit the thenable wrapper class with buffered methods
        EmitThenableBuilderClass(resource, _thenableMethodBuffer);
        _currentResource = null;
    }

    /// <inheritdoc />
    protected override void EmitProperty(RoPropertyInfo property, PropertyContext context)
    {
        if (context == PropertyContext.ProxyProperty)
        {
            var jsType = FormatJsType(Model, property.PropertyType);

            Writer.WriteLine($$"""

              async get{{property.Name}}(): Promise<{{jsType}}> {
                const result = await this._proxy.getProperty('{{property.Name}}');
                return result as {{jsType}};
              }
            """);
        }
    }

    /// <inheritdoc />
    protected override string FormatParameterName(string name) => CamelCase(name);

    /// <inheritdoc />
    protected override void EmitProxyMethodStart(ProxyMethodContext context)
    {
        var paramsStr = string.Join(", ", context.Parameters.Select(p => $"{p.Name}: {p.Type}"));
        var returnType = context.IsVoid ? "void" : context.ReturnType;

        Writer.WriteLine();
        if (context.IsStatic)
        {
            var clientParam = context.Parameters.Count > 0 ? "client: RemoteAppHostClient, " : "client: RemoteAppHostClient";
            Writer.WriteLine($"  static async {context.MethodName}({clientParam}{paramsStr}): Promise<{returnType}> {{");
        }
        else
        {
            Writer.WriteLine($"  async {context.MethodName}({paramsStr}): Promise<{returnType}> {{");
        }
    }

    /// <inheritdoc />
    protected override void EmitProxyMethodBody(ProxyMethodContext context)
    {
        // Build args object, wrapping callbacks with registerCallback
        var args = context.Parameters.Select(p =>
            p.IsCallback ? $"{p.Name}: registerCallback({p.Name})" : p.Name);
        var argsStr = context.Parameters.Count > 0
            ? $"{{ {string.Join(", ", args)} }}"
            : "{}";

        if (context.IsStatic)
        {
            var declaringType = context.DeclaringType!;
            if (context.IsVoid)
            {
                Writer.WriteLine($"    await client.invokeStaticMethod('{declaringType.Namespace}', '{declaringType.Name}', '{context.OriginalMethodName}', {argsStr});");
            }
            else
            {
                Writer.WriteLine($"    return await client.invokeStaticMethod('{declaringType.Namespace}', '{declaringType.Name}', '{context.OriginalMethodName}', {argsStr}) as {context.ReturnType};");
            }
        }
        else
        {
            if (context.IsVoid)
            {
                Writer.WriteLine($"    await this._proxy.invokeMethod('{context.OriginalMethodName}', {argsStr});");
            }
            else
            {
                Writer.WriteLine($"    return await this._proxy.invokeMethod('{context.OriginalMethodName}', {argsStr}) as {context.ReturnType};");
            }
        }
    }

    /// <inheritdoc />
    protected override void EmitProxyMethodEnd(ProxyMethodContext context)
    {
        Writer.WriteLine("  }");
    }

    /// <inheritdoc />
    protected override void EmitExtensionMethod(MethodOverload overload)
    {
        var method = overload.Method;
        var returnType = method.ReturnType;

        // Skip methods that return primitive types or have out/ref parameters
        if (IsPrimitiveReturnType(Model, returnType) ||
            method.Parameters.Any(p => p.ParameterType.IsByRef))
        {
            return;
        }

        // UniqueName is already camelCase and disambiguated by base class
        var methodName = overload.UniqueName;

        var jsReturnTypeName = FormatJsType(Model, returnType);

        // Buffer methods that return builders for thenable class generation
        var isBuilderReturnType = jsReturnTypeName.EndsWith("Builder", StringComparison.Ordinal);
        if (_currentResource != null && isBuilderReturnType)
        {
            _thenableMethodBuffer.Add(overload);
        }

        var parameters = method.Parameters.Skip(1); // Skip the first parameter (this)

        bool ParameterIsOptionalOrNullable(RoParameterInfo p) => p.IsOptional || Model.WellKnownTypes.IsNullableOfT(p.ParameterType);

        var orderedParameters = parameters.OrderBy(p => p.IsOptional ? 1 : 0).ThenBy(p => Model.WellKnownTypes.IsNullableOfT(p.ParameterType) ? 1 : 0);

        const string optionalArgumentName = "optionalArguments";

        var optionalParameters = method.Parameters.Skip(1).Where(ParameterIsOptionalOrNullable).ToArray();
        var shouldCreateArgsClass = optionalParameters.Length > 1;

        var parameterList = string.Join(", ", orderedParameters.Select(p => FormatArgument(Model, p)));
        var jsonParameterList = string.Join(", ", parameters.Select(p => FormatJsonArgument(Model, p, prefix: shouldCreateArgsClass && optionalParameters.Contains(p) ? $"{optionalArgumentName}?." : "")));

        string optionalArgsInitSnippet = "";

        if (shouldCreateArgsClass)
        {
            if (!_argsClassNameByMethod.TryGetValue(method, out var parameterType))
            {
                parameterType = $"{method.Name}Args";
                var k = 1;
                while (_argsClassDefinitions.ContainsKey(parameterType))
                {
                    parameterType = $"{method.Name}Args{k++}";
                }

                var classDefinition = $$"""
                export class {{parameterType}} {
                """;

                foreach (var p in optionalParameters)
                {
                    classDefinition += $"\n      public {p.Name}?: {FormatJsType(Model, p.ParameterType)};";
                }

                classDefinition += "\n";

                static bool HasDefaultValue(RoParameterInfo p) => p.RawDefaultValue != null && p.RawDefaultValue != DBNull.Value && p.RawDefaultValue != System.Reflection.Missing.Value;

                classDefinition += $$"""

                    constructor(args: Partial<{{parameterType}}> = {}) {
                """;

                foreach (var p in optionalParameters)
                {
                    if (HasDefaultValue(p))
                    {
                        var defaultValue = "";

                        if (p.ParameterType == Model.WellKnownTypes.GetKnownType<string>())
                        {
                            defaultValue += $" = \"{p.RawDefaultValue}\"";
                        }
                        else if (p.ParameterType == Model.WellKnownTypes.GetKnownType<bool>())
                        {
                            defaultValue += $" = {p.RawDefaultValue!.ToString()!.ToLower()}";
                        }
                        else if (p.ParameterType.IsEnum)
                        {
                            defaultValue += $" = {p.ParameterType.Name}.{p.RawDefaultValue}";
                        }
                        else
                        {
                            defaultValue += $" = {p.RawDefaultValue}";
                        }

                        classDefinition += $"\n      this.{p.Name} {defaultValue};";
                    }
                }

                classDefinition += "\n      Object.assign(this, args);";
                classDefinition += "\n    }";
                classDefinition += "\n}";

                _argsClassNameByMethod[method] = parameterType;
                _argsClassDefinitions[parameterType] = classDefinition;
            }

            parameterList = string.Join(", ", parameters.Except(optionalParameters).Select(p => FormatArgument(Model, p)));
            if (parameterList.Length > 0)
            {
                parameterList += ", ";
            }
            parameterList += $"{optionalArgumentName}: {parameterType} = new {parameterType}()";

            optionalArgsInitSnippet = $$"""

                {{optionalArgumentName}} = Object.assign(new {{parameterType}}(), {{optionalArgumentName}});
            """;
        }

        // Generate JSDoc comments
        Writer.WriteLine($$"""

           /**
           * {{method.Name}}
           * @remarks C# Definition: {{FormatMethodSignature(method)}}
           {{string.Join("\n   ", parameters.Select(p => $"* @param {{{FormatJsType(Model, p.ParameterType)}}} {p.Name} C# Type: {PrettyPrintCSharpType(p.ParameterType)}"))}}
           * @returns {{{jsReturnTypeName}}} C# Type: {{PrettyPrintCSharpType(returnType)}}
           */
        """);

        // Method body - different for proxy types vs builder types
        var isProxyReturnType = jsReturnTypeName.EndsWith("Proxy", StringComparison.Ordinal);

        if (isProxyReturnType)
        {
            // For proxy types, use invokeStaticMethod (extension method) and wrap result in proxy
            var methodAssemblyProxy = method.DeclaringType?.DeclaringAssembly?.Name ?? "";
            var methodTypeNameProxy = method.DeclaringType?.FullName ?? "";
            var extensionArgsProxy = $"builder: this._proxy, {jsonParameterList}";

            Writer.WriteLine($$"""
              async {{methodName}}({{parameterList}}) : Promise<{{jsReturnTypeName}}> {{{optionalArgsInitSnippet}}
                const result = await client.invokeStaticMethod('{{methodAssemblyProxy}}', '{{methodTypeNameProxy}}', '{{method.Name}}', {{{extensionArgsProxy}}});
                if (result && typeof result === 'object' && '$id' in result) {
                    return new {{jsReturnTypeName}}(new DotNetProxy(result as any));
                }
                throw new Error('{{method.Name}} did not return a marshalled object');
              };
            """);
        }
        else if (isBuilderReturnType)
        {
            // For builder types, generate internal async method and public fluent method
            var internalMethodName = $"_{methodName}Internal";
            var thenableReturnType = $"{jsReturnTypeName}Promise";

            // Get the extension method type info
            var methodAssembly = method.DeclaringType?.DeclaringAssembly?.Name ?? "";
            var methodTypeName = method.DeclaringType?.FullName ?? "";

            // Build args including the builder as first param (for extension methods)
            var extensionArgs = $"builder: this._proxy, {jsonParameterList}";

            // Generate internal async method using invokeStaticMethod for extension methods
            Writer.WriteLine($$"""
              /** @internal */
              async {{internalMethodName}}({{parameterList}}) : Promise<{{jsReturnTypeName}}> {{{optionalArgsInitSnippet}}
                const result = await client.invokeStaticMethod('{{methodAssembly}}', '{{methodTypeName}}', '{{method.Name}}', {{{extensionArgs}}});
                if (result && typeof result === 'object' && '$id' in result) {
                    return new {{jsReturnTypeName}}(new DotNetProxy(result as any));
                }
                throw new Error('{{method.Name}} did not return a marshalled object');
              };
            """);

            // Generate public fluent method that returns thenable wrapper
            string argsList;
            if (shouldCreateArgsClass)
            {
                var requiredArgs = string.Join(", ", parameters.Except(optionalParameters).Select(p => p.Name));
                argsList = requiredArgs.Length > 0
                    ? $"{requiredArgs}, {optionalArgumentName}"
                    : optionalArgumentName;
            }
            else
            {
                argsList = string.Join(", ", parameters.Select(p => p.Name));
            }

            Writer.WriteLine($$"""

              /**
               * {{method.Name}} (fluent chaining)
               * @remarks C# Definition: {{FormatMethodSignature(method)}}
               */
              {{methodName}}({{parameterList}}): {{thenableReturnType}} {{{optionalArgsInitSnippet}}
                return new {{thenableReturnType}}(this.{{internalMethodName}}({{argsList}}));
              }
            """);
        }
        else
        {
            // For other types, use invokeStaticMethod for extension methods
            var methodAssembly2 = method.DeclaringType?.DeclaringAssembly?.Name ?? "";
            var methodTypeName2 = method.DeclaringType?.FullName ?? "";
            var extensionArgs2 = $"builder: this._proxy, {jsonParameterList}";

            // For primitive/unknown types (any, string, etc.), return the proxy directly
            var returnStatement = IsPrimitiveJsType(jsReturnTypeName)
                ? "return new DotNetProxy(result as any);"
                : $"return new {jsReturnTypeName}(new DotNetProxy(result as any));";

            Writer.WriteLine($$"""
              async {{methodName}}({{parameterList}}) : Promise<{{jsReturnTypeName}}> {{{optionalArgsInitSnippet}}
                const result = await client.invokeStaticMethod('{{methodAssembly2}}', '{{methodTypeName2}}', '{{method.Name}}', {{{extensionArgs2}}});
                if (result && typeof result === 'object' && '$id' in result) {
                    {{returnStatement}}
                }
                throw new Error('{{method.Name}} did not return a marshalled object');
              };
            """);
        }
    }

    private void GenerateParameterClasses(TextWriter writer)
    {
        foreach (var classDefinition in _argsClassDefinitions.Values)
        {
            writer.WriteLine(classDefinition);
        }
    }

    private void GenerateCallbackProxyClasses(TextWriter writer, ApplicationModel model)
    {
        // Generate proxy wrapper classes for callback parameter types
        // These provide typed wrappers around DotNetProxy with property accessors
        foreach (var (typeName, roType) in _proxyTypesByName)
        {
            // Collect static properties to generate static accessors
            var staticProperties = roType.Properties.Where(p => p.IsStatic).ToList();
            var instanceProperties = roType.Properties.Where(p => !p.IsStatic).ToList();

            writer.WriteLine();
            writer.WriteLine($$"""
                /**
                 * Typed proxy wrapper for {{typeName}}
                 * Provides typed access to .NET object properties via JSON-RPC
                 */
                export class {{typeName}}Proxy {
                    constructor(private _proxy: DotNetProxy) {}

                    /** Get the underlying proxy for advanced operations */
                    get proxy(): DotNetProxy { return this._proxy; }

                    /** The .NET type name */
                    get $type(): string { return this._proxy.$type; }

                    /** The object identifier for use in method calls */
                    get $id(): string { return this._proxy.$id; }
                """);

            // Generate static property accessors first
            foreach (var property in staticProperties)
            {
                var jsReturnType = GetProxyReturnType(model, property.PropertyType);
                var needsWrapper = jsReturnType.EndsWith("Proxy", StringComparison.Ordinal) && jsReturnType != "DotNetProxy";

                if (property.CanRead)
                {
                    if (needsWrapper)
                    {
                        writer.WriteLine($$"""

                            /**
                             * Gets the static {{property.Name}} property
                             * @returns Promise<{{jsReturnType}}>
                             */
                            static async get{{property.Name}}(client: RemoteAppHostClient): Promise<{{jsReturnType}}> {
                                const result = await client.getStaticProperty("{{roType.DeclaringAssembly.Name}}", "{{roType.FullName}}", "{{property.Name}}");
                                return new {{jsReturnType}}(wrapIfProxy(result) as DotNetProxy);
                            }
                        """);
                    }
                    else
                    {
                        writer.WriteLine($$"""

                            /**
                             * Gets the static {{property.Name}} property
                             * @returns Promise<{{jsReturnType}}>
                             */
                            static async get{{property.Name}}(client: RemoteAppHostClient): Promise<{{jsReturnType}}> {
                                const result = await client.getStaticProperty("{{roType.DeclaringAssembly.Name}}", "{{roType.FullName}}", "{{property.Name}}");
                                return wrapIfProxy(result) as {{jsReturnType}};
                            }
                        """);
                    }
                }

                if (property.CanWrite)
                {
                    var jsParamType = GetSimpleJsType(model, property.PropertyType);
                    writer.WriteLine($$"""

                        /**
                         * Sets the static {{property.Name}} property
                         */
                        static async set{{property.Name}}(client: RemoteAppHostClient, value: {{jsParamType}}): Promise<void> {
                            await client.setStaticProperty("{{roType.DeclaringAssembly.Name}}", "{{roType.FullName}}", "{{property.Name}}", value);
                        }
                    """);
                }
            }

            // Generate typed instance property accessors
            foreach (var property in instanceProperties)
            {
                var jsReturnType = GetProxyReturnType(model, property.PropertyType);
                // Only wrap with typed proxies - DotNetProxy is the base class and doesn't need wrapping
                var needsWrapper = jsReturnType.EndsWith("Proxy", StringComparison.Ordinal) && jsReturnType != "DotNetProxy";

                if (property.CanRead)
                {
                    if (needsWrapper)
                    {
                        writer.WriteLine($$"""

                            /**
                             * Gets the {{property.Name}} property
                             * @returns Promise<{{jsReturnType}}>
                             */
                            async get{{property.Name}}(): Promise<{{jsReturnType}}> {
                                const result = await this._proxy.getProperty("{{property.Name}}");
                                return new {{jsReturnType}}(result as DotNetProxy);
                            }
                        """);
                    }
                    else
                    {
                        writer.WriteLine($$"""

                            /**
                             * Gets the {{property.Name}} property
                             * @returns Promise<{{jsReturnType}}>
                             */
                            async get{{property.Name}}(): Promise<{{jsReturnType}}> {
                                const result = await this._proxy.getProperty("{{property.Name}}");
                                return result as {{jsReturnType}};
                            }
                        """);
                    }
                }

                if (property.CanWrite)
                {
                    var jsParamType = GetSimpleJsType(model, property.PropertyType);
                    writer.WriteLine($$"""

                        /**
                         * Sets the {{property.Name}} property
                         */
                        async set{{property.Name}}(value: {{jsParamType}}): Promise<void> {
                            await this._proxy.setProperty("{{property.Name}}", value);
                        }
                    """);
                }
            }

            // Add generic fallback methods for accessing any property/method
            writer.WriteLine($$"""

                    /**
                     * Gets a property value from the .NET object (generic fallback)
                     * @param propertyName The property name
                     */
                    async getProperty<T = unknown>(propertyName: string): Promise<T> {
                        const result = await this._proxy.getProperty(propertyName);
                        return result as T;
                    }

                    /**
                     * Sets a property value on the .NET object (generic fallback)
                     * @param propertyName The property name
                     * @param value The value to set
                     */
                    async setProperty(propertyName: string, value: unknown): Promise<void> {
                        await this._proxy.setProperty(propertyName, value);
                    }

                    /**
                     * Gets an indexed value (e.g., dictionary[key])
                     */
                    async getIndexer<T = unknown>(key: string | number): Promise<T> {
                        const result = await this._proxy.getIndexer(key);
                        return result as T;
                    }

                    /**
                     * Sets an indexed value (e.g., dictionary[key] = value)
                     */
                    async setIndexer(key: string | number, value: unknown): Promise<void> {
                        await this._proxy.setIndexer(key, value);
                    }

                    /**
                     * Invokes a method on the .NET object
                     */
                    async invokeMethod<T = unknown>(methodName: string, args?: Record<string, unknown>): Promise<T> {
                        const result = await this._proxy.invokeMethod(methodName, args);
                        return result as T;
                    }

                    /**
                     * Releases the proxy reference
                     */
                    async dispose(): Promise<void> {
                        await this._proxy.dispose();
                    }
                }
                """);
        }

        // Generate a DictionaryProxy for generic dictionary access
        writer.WriteLine($$"""

            /**
             * Generic dictionary proxy for IDictionary<string, object> access
             */
            export class DictionaryProxy {
                constructor(private _proxy: DotNetProxy) {}

                /** Get the underlying proxy for advanced operations */
                get proxy(): DotNetProxy { return this._proxy; }

                async get<T = unknown>(key: string): Promise<T> {
                    const result = await this._proxy.getIndexer(key);
                    return result as T;
                }

                async set(key: string, value: unknown): Promise<void> {
                    await this._proxy.setIndexer(key, value);
                }

                async dispose(): Promise<void> {
                    await this._proxy.dispose();
                }
            }
            """);
    }

    private string GetProxyReturnType(ApplicationModel model, RoType propertyType)
    {
        // Check if this is a known proxy type
        if (_proxyTypesByName.ContainsKey(propertyType.Name))
        {
            return $"{propertyType.Name}Proxy";
        }

        // Dictionary types
        if (IsDictionaryType(model, propertyType))
        {
            return "DictionaryProxy";
        }

        // List types
        if (IsListType(model, propertyType))
        {
            return "ListProxy";
        }

        // Simple types return as-is
        if (IsSimpleType(model, propertyType))
        {
            return GetSimpleJsType(model, propertyType);
        }

        // Complex types return DotNetProxy
        return "DotNetProxy";
    }

    private static string GetSimpleJsType(ApplicationModel model, RoType type)
    {
        if (type == model.WellKnownTypes.GetKnownType(typeof(string)))
        {
            return "string";
        }

        if (type == model.WellKnownTypes.GetKnownType(typeof(bool)))
        {
            return "boolean";
        }

        if (type == model.WellKnownTypes.GetKnownType(typeof(int)) ||
            type == model.WellKnownTypes.GetKnownType(typeof(long)) ||
            type == model.WellKnownTypes.GetKnownType(typeof(double)) ||
            type == model.WellKnownTypes.GetKnownType(typeof(float)))
        {
            return "number";
        }

        if (type.IsEnum)
        {
            return "string";
        }

        return "unknown";
    }

    private static void GenerateModelClasses(TextWriter textWriter, ApplicationModel model)
    {
        // Only generate enums as TypeScript enums
        // Non-enum model types are handled by *Proxy classes that wrap DotNetProxy
        foreach (var type in model.ModelTypes)
        {
            if (type.IsEnum)
            {
                textWriter.WriteLine();
                textWriter.WriteLine($$"""
                    export enum {{type.Name}} {
                      {{string.Join(", ", type.GetEnumNames().Select(x => $"{x} = \"{x}\""))}}
                    }
                    """);
            }
            // Non-enum model types don't need ReferenceClass wrappers
            // They're accessed via DotNetProxy or typed *Proxy wrapper classes
        }
    }

    private void EmitThenableBuilderClass(ResourceModel resource, IReadOnlyList<MethodOverload> methodOverloads)
    {
        var resourceName = SanitizeClassName(resource.ResourceType.Name);
        var builderClassName = $"{resourceName}Builder";
        var thenableClassName = $"{resourceName}BuilderPromise";

        Writer.WriteLine();
        Writer.WriteLine($$"""
            /**
             * Thenable wrapper for {{builderClassName}} that enables fluent chaining.
             * Usage: await builder.addX("name").withY().withZ();
             */
            export class {{thenableClassName}} implements PromiseLike<{{builderClassName}}> {
              constructor(private _promise: Promise<{{builderClassName}}>) {}

              then<TResult1 = {{builderClassName}}, TResult2 = never>(
                onfulfilled?: ((value: {{builderClassName}}) => TResult1 | PromiseLike<TResult1>) | null,
                onrejected?: ((reason: any) => TResult2 | PromiseLike<TResult2>) | null
              ): PromiseLike<TResult1 | TResult2> {
                return this._promise.then(onfulfilled, onrejected);
              }
            """);

        // Generate fluent methods using pre-computed overloads
        foreach (var overload in methodOverloads)
        {
            EmitThenableMethod(overload);
        }

        Writer.WriteLine("}");
    }

    private void EmitThenableMethod(MethodOverload overload)
    {
        var method = overload.Method;
        var methodName = overload.UniqueName; // Already camelCase and disambiguated

        var jsReturnTypeName = FormatJsType(Model, method.ReturnType);
        var returnThenableClassName = $"{jsReturnTypeName}Promise";

        var parameters = method.Parameters.Skip(1); // Skip the first parameter (this)
        bool ParameterIsOptionalOrNullable(RoParameterInfo p) => p.IsOptional || Model.WellKnownTypes.IsNullableOfT(p.ParameterType);

        var orderedParameters = parameters.OrderBy(p => p.IsOptional ? 1 : 0).ThenBy(p => Model.WellKnownTypes.IsNullableOfT(p.ParameterType) ? 1 : 0);

        const string optionalArgumentName = "optionalArguments";
        var optionalParameters = method.Parameters.Skip(1).Where(ParameterIsOptionalOrNullable).ToArray();
        var shouldCreateArgsClass = optionalParameters.Length > 1;

        var parameterList = string.Join(", ", orderedParameters.Select(p => FormatArgument(Model, p)));

        if (shouldCreateArgsClass)
        {
            // Use the same Args class name that was created during EmitExtensionMethod
            var parameterType = _argsClassNameByMethod.GetValueOrDefault(method, $"{method.Name}Args");

            parameterList = string.Join(", ", parameters.Except(optionalParameters).Select(p => FormatArgument(Model, p)));
            if (parameterList.Length > 0)
            {
                parameterList += ", ";
            }
            parameterList += $"{optionalArgumentName}?: {parameterType}";
        }

        // Build the argument list for the internal method call
        string thenableArgsList;
        if (shouldCreateArgsClass)
        {
            var requiredArgs = string.Join(", ", parameters.Except(optionalParameters).Select(p => p.Name));
            thenableArgsList = requiredArgs.Length > 0
                ? $"{requiredArgs}, {optionalArgumentName}"
                : optionalArgumentName;
        }
        else
        {
            thenableArgsList = string.Join(", ", parameters.Select(p => p.Name));
        }

        // Generate fluent method that chains onto the promise
        Writer.WriteLine($$"""

          /**
           * {{method.Name}} (fluent chaining)
           */
          {{methodName}}({{parameterList}}): {{returnThenableClassName}} {
            return new {{returnThenableClassName}}(
              this._promise.then(b => b._{{methodName}}Internal({{thenableArgsList}}))
            );
          }
        """);
    }

    private static string FormatMethodSignature(RoMethod method)
    {
        var parameters = method.Parameters;
        var parameterList = string.Join(", ", parameters.Select(p => $"{PrettyPrintCSharpType(p.ParameterType)} {p.Name}"));

        var genericArguments = method.GetGenericArguments();

        if (genericArguments.Count > 0)
        {
            var genericArgumentList = string.Join(", ", genericArguments.Select(PrettyPrintCSharpType));
            return $"{PrettyPrintCSharpType(method.ReturnType)} {method.Name}<{genericArgumentList}>({parameterList})";
        }

        return $"{PrettyPrintCSharpType(method.ReturnType)} {method.Name}({parameterList})";
    }

    private static string PrettyPrintCSharpType(RoType? t)
    {
        if (t is null)
        {
            return "";
        }

        return t.Name;
    }

    private static string CamelCase(string methodName)
    {
        return char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);
    }

    /// <inheritdoc />
    protected override string FormatMethodName(string name) => CamelCase(name);

    /// <inheritdoc />
    protected override string FormatType(RoType type) => FormatJsType(Model, type);

    private static string FormatJsonArgument(ApplicationModel model, RoParameterInfo p, string prefix)
    {
        var actionType = model.WellKnownTypes.GetKnownType(typeof(Action<>));

        // Handle delegate types - register callback and pass ID
        if (IsDelegateType(model, p.ParameterType))
        {
            // Register the callback and pass the callback ID to the server
            // The server will invoke it via JSON-RPC when the C# delegate is called

            // Check if this is an Action<T> with a complex type or IResourceBuilder<T>
            if (p.ParameterType.IsGenericType &&
                p.ParameterType.GenericTypeDefinition == actionType &&
                p.ParameterType.GetGenericArguments()[0] is { } callbackArgType)
            {
                // Handle Action<IResourceBuilder<T>> - wrap with the appropriate builder type
                if (IsResourceBuilderType(model, callbackArgType))
                {
                    var resourceType = GetResourceBuilderResourceType(callbackArgType)!;
                    var builderTypeName = $"{SanitizeClassName(resourceType.Name)}Builder";

                    if (p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType))
                    {
                        // Use non-null assertion (!) inside callback since we check existence with ternary
                        return $"{p.Name}: {prefix}{p.Name} ? registerCallback((arg: DotNetProxy) => {prefix}{p.Name}!(new {builderTypeName}(arg))) : null";
                    }
                    return $"{p.Name}: registerCallback((arg: DotNetProxy) => {prefix}{p.Name}(new {builderTypeName}(arg)))";
                }

                // Handle Action<T> with a complex type that has a proxy wrapper
                if (!IsSimpleType(model, callbackArgType) && !callbackArgType.IsGenericType)
                {
                    var proxyTypeName = $"{callbackArgType.Name}Proxy";
                    // Wrap the callback to convert DotNetProxy to the expected proxy type
                    if (p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType))
                    {
                        // Use non-null assertion (!) inside callback since we check existence with ternary
                        return $"{p.Name}: {prefix}{p.Name} ? registerCallback((arg: DotNetProxy) => {prefix}{p.Name}!(new {proxyTypeName}(arg))) : null";
                    }
                    return $"{p.Name}: registerCallback((arg: DotNetProxy) => {prefix}{p.Name}(new {proxyTypeName}(arg)))";
                }
            }

            if (p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType))
            {
                return $"{p.Name}: {prefix}{p.Name} ? registerCallback({prefix}{p.Name}) : null";
            }
            return $"{p.Name}: registerCallback({prefix}{p.Name})";
        }

        var result = p.Name!;
        result += $": {prefix}{p.Name!}";

        // For IResourceBuilder<T> parameters, pass the proxy reference
        if (IsResourceBuilderType(model, p.ParameterType))
        {
            result += "?.proxy";
        }

        if (p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType))
        {
            result = $"{result} || null";
        }

        return result;
    }

    private string FormatArgument(ApplicationModel model, RoParameterInfo p)
    {
        var result = p.Name!;
        var IsNullableOfT = model.WellKnownTypes.IsNullableOfT(p.ParameterType);
        if (p.IsOptional || IsNullableOfT)
        {
            result += "?";
        }

        if (IsNullableOfT)
        {
            result += $": {FormatJsType(model, p.ParameterType.GetGenericArguments()[0])} | null";
        }
        else
        {
            result += $": {FormatJsType(model, p.ParameterType)}";
        }

        return result;
    }

    /// <summary>
    /// Formats an Action delegate type and registers its argument type for proxy generation.
    /// </summary>
    private string FormatActionType(ApplicationModel model, RoType actionType)
    {
        var args = actionType.GetGenericArguments();
        var argTypes = new List<string>();

        for (int i = 0; i < args.Count; i++)
        {
            var argType = args[i];
            var typeName = argType.Name;

            // Check if this is an IResourceBuilder<T> type - use the builder class name
            if (IsResourceBuilderType(model, argType))
            {
                var resourceType = GetResourceBuilderResourceType(argType)!;
                var builderTypeName = $"{SanitizeClassName(resourceType.Name)}Builder";
                argTypes.Add($"p{i}: {builderTypeName}");
            }
            // Skip other generic types like IDictionary<K,V> - they're accessed via DotNetProxy
            else if (argType.IsGenericType)
            {
                argTypes.Add($"p{i}: DotNetProxy");
            }
            // Register complex types for proxy wrapper generation
            else if (!IsSimpleType(model, argType) && !string.IsNullOrEmpty(typeName))
            {
                if (_proxyTypes.Add(typeName))
                {
                    _proxyTypesByName[typeName] = argType;
                }
                // Use the proxy wrapper type name
                argTypes.Add($"p{i}: {typeName}Proxy");
            }
            else
            {
                argTypes.Add($"p{i}: {FormatJsType(model, argType)}");
            }
        }

        return $"({string.Join(", ", argTypes)}) => void | Promise<void>";
    }

    /// <summary>
    /// Checks if a JS type name is a primitive type that cannot be instantiated with 'new'.
    /// </summary>
    private static bool IsPrimitiveJsType(string jsTypeName)
    {
        return jsTypeName is "any" or "string" or "number" or "boolean" or "void" or "unknown" or "never";
    }

    private string FormatJsType(ApplicationModel model, RoType type)
    {
        // Check if this type has a proxy wrapper
        if (model.BuilderModel.ProxyTypes.TryGetValue(type, out var proxyModel))
        {
            return proxyModel.ProxyClassName;
        }

        // For IResourceBuilder<T>, generate TypeBuilder
        // Discovery phase already ensured all referenced types are known
        if (model.WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var resourceType))
        {
            return $"{resourceType.Name}Builder";
        }

        return type switch
        {
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(Nullable<>)) => FormatJsType(model, type.GetGenericArguments()[0]) + " | null",
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(List<>)) => $"Array<{FormatJsType(model, type.GetGenericArguments()[0])}>",
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(Dictionary<,>)) => "Map<any, any>",
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(IList<>)) => $"Array<{FormatJsType(model, type.GetGenericArguments()[0])}>",
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(ICollection<>)) => $"Array<{FormatJsType(model, type.GetGenericArguments()[0])}>",
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(IReadOnlyList<>)) => $"Array<{FormatJsType(model, type.GetGenericArguments()[0])}>",
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(IReadOnlyCollection<>)) => $"Array<{FormatJsType(model, type.GetGenericArguments()[0])}>",
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(Action<>)) => FormatActionType(model, type),
            { } when type.IsArray => $"Array<{FormatJsType(model, type.GetElementType() ?? model.WellKnownTypes.GetKnownType(typeof(object)))}>",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(char)) => "string",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(string)) => "string",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(Version)) => "string",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(Uri)) => "string",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(sbyte)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(byte)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(short)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(ushort)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(int)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(uint)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(long)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(ulong)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(nint)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(nuint)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(double)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(float)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(decimal)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(bool)) => "boolean",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(Guid)) => "string",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(object)) => "any",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(DateTime)) => "Date",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(TimeSpan)) => "number",
            { } when type == model.WellKnownTypes.GetKnownType(typeof(DateTimeOffset)) => "Date",
            { } when type.IsEnum => type.Name,
            { IsGenericParameter: true } => "any", // Generic type parameters like T, TBuilder
            _ => "any"
        };
    }

    #region Reflection-based Generation Methods

    /// <summary>
    /// Generates the createBuilder() function using reflected type information.
    /// </summary>
    private static void GenerateCreateBuilderFunction(TextWriter writer, ApplicationModel model)
    {
        var appType = model.WellKnownTypes.DistributedApplicationType;
        var assemblyName = appType.DeclaringAssembly?.Name ?? "Aspire.Hosting";
        var typeName = appType.FullName;

        writer.WriteLine($$"""

        export async function createBuilder(args: string[] = process.argv.slice(2)): Promise<DistributedApplicationBuilder> {
            console.log('Connecting to AppHost server...');

            while (true) {
              try {
                await client.connect();
                await client.ping();
                console.log('Connected successfully!');
                break;
              } catch (error) {
                await new Promise(resolve => setTimeout(resolve, 1000));
              }
            }

            const result = await client.invokeStaticMethod('{{assemblyName}}', '{{typeName}}', 'CreateBuilder', {
              options: {
                Args: args,
                ProjectDirectory: process.cwd()
              }
            });

            if (result && typeof result === 'object' && '$id' in result) {
              return new DistributedApplicationBuilder(new DotNetProxy(result as any));
            }

            throw new Error('Failed to create DistributedApplicationBuilder');
        }
        """);
    }

    /// <summary>
    /// Generates the DistributedApplication class using reflected type information.
    /// </summary>
    private void GenerateDistributedApplicationClass(TextWriter writer, ApplicationModel model)
    {
        writer.WriteLine("""

        export class DistributedApplication {
          private _appProxy: DotNetProxy | null = null;

          constructor(private builderProxy: DotNetProxy | null) {
          }

          async run() {
            // Build the application using invokeMethod
            const buildResult = await this.builderProxy?.invokeMethod('Build', {});

            // Store the app proxy for service resolution
            if (buildResult && typeof buildResult === 'object' && '$id' in buildResult) {
              this._appProxy = new DotNetProxy(buildResult as any);
            }

            // Run the application using invokeMethod
            const runPromise = this._appProxy?.invokeMethod('RunAsync', {});

            // Wait for either Ctrl+C, SIGTERM, or the connection to close
            await new Promise<void>((resolve) => {
              const shutdown = async () => {
                console.log("\nStopping application...");
                try {
                  await this._appProxy?.invokeMethod('StopAsync', {});
                } catch {
                  // Ignore errors during shutdown
                }
                client.disconnect();
                resolve();
              };

              process.on("SIGINT", shutdown);
              process.on("SIGTERM", shutdown);

              client.onDisconnect(() => {
                resolve();
              });

              runPromise?.then(() => resolve()).catch(() => resolve());
            });

            process.exit(0);
          }
        """);

        // Generate property accessors from reflected ApplicationProperties
        foreach (var prop in model.BuilderModel.ApplicationProperties)
        {
            var jsReturnType = FormatJsType(model, prop.PropertyType);
            var methodName = $"get{prop.Name}";

            writer.WriteLine($$"""

              async {{CamelCase(methodName)}}(): Promise<{{jsReturnType}}> {
                if (!this._appProxy) {
                  throw new Error('Application not yet running. Call run() first.');
                }
                const result = await this._appProxy.getProperty('{{prop.Name}}');
                return result as {{jsReturnType}};
              }
            """);
        }

        writer.WriteLine("}");
    }

    /// <summary>
    /// Generates the DistributedApplicationBuilderBase class from reflected properties.
    /// </summary>
    private void GenerateBuilderBaseClass(TextWriter writer, ApplicationModel model)
    {
        writer.WriteLine("""

        abstract class DistributedApplicationBuilderBase {
          constructor(protected _proxy: DotNetProxy) {}

          get proxy(): DotNetProxy { return this._proxy; }
        """);

        // Generate property accessors from reflected BuilderProperties
        foreach (var prop in model.BuilderModel.BuilderProperties)
        {
            // Check if this property type has a proxy wrapper
            if (model.BuilderModel.ProxyTypes.TryGetValue(prop.PropertyType, out var proxyModel))
            {
                writer.WriteLine($$"""

              async get{{prop.Name}}(): Promise<{{proxyModel.ProxyClassName}}> {
                const result = await this._proxy.getProperty('{{prop.Name}}') as DotNetProxy;
                return new {{proxyModel.ProxyClassName}}(result);
              }
            """);
            }
            else
            {
                var jsType = FormatJsType(model, prop.PropertyType);

                writer.WriteLine($$"""

              async get{{prop.Name}}(): Promise<{{jsType}}> {
                const result = await this._proxy.getProperty('{{prop.Name}}');
                return result as {{jsType}};
              }
            """);
            }
        }

        // Generate build() method
        writer.WriteLine("""

          build() {
            return new DistributedApplication(this._proxy);
          }
        }
        """);
    }

    #endregion
}
