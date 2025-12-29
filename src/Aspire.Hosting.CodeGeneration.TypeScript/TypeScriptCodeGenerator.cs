// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.TypeScript;

/// <summary>
/// Generates TypeScript code from the Aspire application model with rich type information.
/// Produces instance methods on DistributedApplicationBuilder and resource-specific builder classes.
/// </summary>
public sealed class TypeScriptCodeGenerator : ICodeGenerator
{
    // Custom record-like classes that contain the overload parameters
    private readonly Dictionary<RoMethod, string> _overloadParameterClassByMethod = [];
    private readonly Dictionary<string, string> _overloadParameterClassByName = [];

    // Types that need proxy wrapper classes (all complex types get proxies)
    private readonly HashSet<string> _proxyTypes = [];
    private readonly Dictionary<string, RoType> _proxyTypesByName = [];

    /// <inheritdoc />
    public string Language => "TypeScript";

    /// <inheritdoc />
    public Dictionary<string, string> GenerateDistributedApplication(ApplicationModel model)
    {
        var files = new Dictionary<string, string>();

        // Generate main distributed-application.ts
        using var writer = new StringWriter();
        GenerateDistributedApplicationContent(writer, model);
        files["distributed-application.ts"] = writer.ToString();

        // Include embedded resource files
        files["types.ts"] = GetEmbeddedResource("types.ts");
        files["RemoteAppHostClient.ts"] = GetEmbeddedResource("RemoteAppHostClient.ts");

        return files;
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

    private void GenerateDistributedApplicationContent(TextWriter writer, ApplicationModel model)
    {
        // Write the header with imports
        writer.WriteLine("""
        import { RemoteAppHostClient, registerCallback, DotNetProxy, ListProxy, wrapIfProxy } from './RemoteAppHostClient.js';

        // Get socket path from environment variable (set by aspire run)
        const socketPath = process.env.REMOTE_APP_HOST_SOCKET_PATH;
        if (!socketPath) {
            throw new Error('REMOTE_APP_HOST_SOCKET_PATH environment variable not set. Please run with "aspire run".');
        }

        const client = new RemoteAppHostClient(socketPath);
        """);

        // Generate createBuilder() function using reflection
        GenerateCreateBuilderFunction(writer, model);

        // Generate DistributedApplication class using reflection
        GenerateDistributedApplicationClass(writer, model);

        // Generate enum definitions used by proxy classes
        GenerateBuilderEnums(writer, model);

        // Generate proxy classes from BuilderModel.ProxyTypes
        GenerateBuilderProxyClasses(writer, model);

        // Generate DistributedApplicationBuilderBase using reflection
        GenerateBuilderBaseClass(writer, model);

        // Generate DistributedApplicationBuilder class with methods from all integrations
        writer.WriteLine("export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {");
        foreach (var integration in model.IntegrationModels.Values)
        {
            GenerateMethods(writer, model, integration.IDistributedApplicationBuilderExtensionMethods);
        }
        writer.WriteLine("}");

        // Generate base resource builder classes and their thenable wrappers
        writer.WriteLine("""
          export class IResourceWithConnectionStringBuilder {
            constructor(protected _proxy: DotNetProxy) {}

            /** Gets the underlying proxy */
            get proxy(): DotNetProxy { return this._proxy; }
          }

          /**
           * Thenable wrapper for IResourceWithConnectionStringBuilder that enables fluent chaining.
           */
          export class IResourceWithConnectionStringBuilderPromise implements PromiseLike<IResourceWithConnectionStringBuilder> {
            constructor(private _promise: Promise<IResourceWithConnectionStringBuilder>) {}

            then<TResult1 = IResourceWithConnectionStringBuilder, TResult2 = never>(
              onfulfilled?: ((value: IResourceWithConnectionStringBuilder) => TResult1 | PromiseLike<TResult1>) | null,
              onrejected?: ((reason: any) => TResult2 | PromiseLike<TResult2>) | null
            ): PromiseLike<TResult1 | TResult2> {
              return this._promise.then(onfulfilled, onrejected);
            }
          }

          export class ResourceBuilder {
            constructor(protected _proxy: DotNetProxy) {}

            /** Gets the underlying proxy */
            get proxy(): DotNetProxy { return this._proxy; }
          }

          /**
           * Thenable wrapper for ResourceBuilder that enables fluent chaining.
           */
          export class ResourceBuilderPromise implements PromiseLike<ResourceBuilder> {
            constructor(private _promise: Promise<ResourceBuilder>) {}

            then<TResult1 = ResourceBuilder, TResult2 = never>(
              onfulfilled?: ((value: ResourceBuilder) => TResult1 | PromiseLike<TResult1>) | null,
              onrejected?: ((reason: any) => TResult2 | PromiseLike<TResult2>) | null
            ): PromiseLike<TResult1 | TResult2> {
              return this._promise.then(onfulfilled, onrejected);
            }
          }
          """);

        // Generate resource-specific builder classes
        GenerateResourceClasses(writer, model);

        // Generate model classes (enums, etc.)
        GenerateModelClasses(writer, model);

        // Generate overload parameter classes
        GenerateParameterClasses(writer);

        // Generate callback proxy wrapper classes
        GenerateCallbackProxyClasses(writer, model);
    }

    private void GenerateParameterClasses(TextWriter writer)
    {
        foreach (var overloadParameterType in _overloadParameterClassByMethod.Values)
        {
            writer.WriteLine(overloadParameterType);
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

        // Check for generic collection types
        if (propertyType.IsGenericType && propertyType.GenericTypeDefinition is { } genDef)
        {
            // Dictionary types
            var dictionaryTypes = new[] { typeof(Dictionary<,>), typeof(IDictionary<,>) };
            if (dictionaryTypes.Any(d => genDef == model.WellKnownTypes.GetKnownType(d)))
            {
                return "DictionaryProxy";
            }

            // List types
            var listTypes = new[] { typeof(List<>), typeof(IList<>), typeof(ICollection<>) };
            if (listTypes.Any(l => genDef == model.WellKnownTypes.GetKnownType(l)))
            {
                return "ListProxy";
            }
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

    private static string SanitizeClassName(string name) => name.Replace("+", "_");

    private void GenerateResourceClasses(TextWriter textWriter, ApplicationModel model)
    {
        foreach (var resourceModel in model.ResourceModels.Values)
        {
            EmitResourceClass(textWriter, model, resourceModel);
        }

        // Generate thenable builder classes for fluent chaining
        GenerateThenableBuilderClasses(textWriter, model);
    }

    private void GenerateThenableBuilderClasses(TextWriter textWriter, ApplicationModel model)
    {
        foreach (var resourceModel in model.ResourceModels.Values)
        {
            EmitThenableBuilderClass(textWriter, model, resourceModel);
        }
    }

    private void EmitThenableBuilderClass(TextWriter textWriter, ApplicationModel model, ResourceModel resourceModel)
    {
        var resourceName = SanitizeClassName(resourceModel.ResourceType.Name);
        var builderClassName = $"{resourceName}Builder";
        var thenableClassName = $"{resourceName}BuilderPromise";

        textWriter.WriteLine();
        textWriter.WriteLine($$"""
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

        // Generate fluent methods that chain onto the promise
        GenerateThenableMethods(textWriter, model, resourceModel.IResourceTypeBuilderExtensionsMethods, thenableClassName);

        textWriter.WriteLine("}");
    }

    private void GenerateThenableMethods(TextWriter writer, ApplicationModel model, IEnumerable<RoMethod> extensionMethods, string thenableClassName)
    {
        foreach (var methodGroups in extensionMethods.GroupBy(m => m.Name))
        {
            var indexes = new Dictionary<string, int>();
            var overloads = methodGroups.OrderBy(m => m.Parameters.Count).ToArray();

            foreach (var overload in overloads)
            {
                var returnType = overload.ReturnType;
                var jsReturnTypeName = FormatJsType(model, returnType);

                // Skip methods that return primitive types or have out/ref parameters
                if (IsPrimitiveReturnType(model, returnType) ||
                    overload.Parameters.Any(p => p.ParameterType.IsByRef))
                {
                    continue;
                }

                // Skip proxy-returning methods - they don't have internal methods and can't chain
                var isProxyReturnType = jsReturnTypeName.EndsWith("Proxy", StringComparison.Ordinal);
                if (isProxyReturnType)
                {
                    continue;
                }

                // Skip methods that don't return builder types - they can't chain
                var isBuilderReturnType = jsReturnTypeName.EndsWith("Builder", StringComparison.Ordinal);
                if (!isBuilderReturnType)
                {
                    continue;
                }

                var methodNameAttribute = overload.GetCustomAttributes()
                    .FirstOrDefault(attr => attr.AttributeType.FullName == "Aspire.Hosting.Polyglot.PolyglotMethodNameAttribute");

                var methodName = CamelCase(
                    methodNameAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "MethodName").Value?.ToString()
                    ?? methodNameAttribute?.FixedArguments?.ElementAtOrDefault(0)?.ToString()
                    ?? overload.Name);

                if (indexes.TryGetValue(methodName, out var index))
                {
                    indexes[methodName] = index + 1;
                    methodName = $"{methodName}{(index + 1).ToString(CultureInfo.InvariantCulture)}";
                }
                else
                {
                    indexes[methodName] = 0;
                }

                var parameters = overload.Parameters.Skip(1); // Skip the first parameter (this)
                bool ParameterIsOptionalOrNullable(RoParameterInfo p) => p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType);

                var orderedParameters = parameters.OrderBy(p => p.IsOptional ? 1 : 0).ThenBy(p => model.WellKnownTypes.IsNullableOfT(p.ParameterType) ? 1 : 0);

                const string optionalArgumentName = "optionalArguments";
                var optionalParameters = overload.Parameters.Skip(1).Where(ParameterIsOptionalOrNullable).ToArray();
                var shouldCreateArgsClass = optionalParameters.Length > 1;

                var parameterList = string.Join(", ", orderedParameters.Select(p => FormatArgument(model, p)));

                if (shouldCreateArgsClass)
                {
                    var parameterType = $"{overload.Name}Args";
                    if (!_overloadParameterClassByMethod.ContainsKey(overload))
                    {
                        var k = 1;
                        while (_overloadParameterClassByName.ContainsKey(parameterType))
                        {
                            parameterType = $"{overload.Name}Args{k++}";
                        }
                    }
                    else
                    {
                        // Find the existing parameter type name
                        foreach (var (method, _) in _overloadParameterClassByMethod)
                        {
                            if (method == overload)
                            {
                                break;
                            }
                        }
                    }

                    parameterList = string.Join(", ", parameters.Except(optionalParameters).Select(p => FormatArgument(model, p)));
                    if (parameterList.Length > 0)
                    {
                        parameterList += ", ";
                    }
                    parameterList += $"{optionalArgumentName}?: {parameterType}";
                }

                // Determine the return thenable type name
                var returnThenableClassName = thenableClassName;
                if (jsReturnTypeName.EndsWith("Builder", StringComparison.Ordinal) && !jsReturnTypeName.EndsWith("Proxy", StringComparison.Ordinal))
                {
                    returnThenableClassName = $"{jsReturnTypeName}Promise";
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
                writer.WriteLine($$"""

                  /**
                   * {{overload.Name}} (fluent chaining)
                   */
                  {{methodName}}({{parameterList}}): {{returnThenableClassName}} {
                    return new {{returnThenableClassName}}(
                      this._promise.then(b => b._{{methodName}}Internal({{thenableArgsList}}))
                    );
                  }
                """);
            }
        }
    }

    private void EmitResourceClass(TextWriter textWriter, ApplicationModel model, ResourceModel resourceModel)
    {
        var resourceName = SanitizeClassName(resourceModel.ResourceType.Name);
        textWriter.WriteLine();
        textWriter.WriteLine($$"""
                export class {{resourceName}}Builder {
                  constructor(protected _proxy: DotNetProxy) {}

                  /** Gets the underlying proxy */
                  get proxy(): DotNetProxy { return this._proxy; }
                """);

        GenerateMethods(textWriter, model, resourceModel.IResourceTypeBuilderExtensionsMethods);

        textWriter.WriteLine("}");
    }

    private void GenerateMethods(TextWriter writer, ApplicationModel model, IEnumerable<RoMethod> extensionMethods)
    {
        foreach (var methodGroups in extensionMethods.GroupBy(m => m.Name))
        {
            var indexes = new Dictionary<string, int>();
            var overloads = methodGroups.OrderBy(m => m.Parameters.Count).ToArray();

            foreach (var overload in overloads)
            {
                var returnType = overload.ReturnType;

                // Skip methods that return primitive types (can't be instantiated as classes)
                // or have out/ref parameters
                if (IsPrimitiveReturnType(model, returnType) ||
                    overload.Parameters.Any(p => p.ParameterType.IsByRef))
                {
                    continue;
                }

                var methodNameAttribute = overload.GetCustomAttributes()
                    .FirstOrDefault(attr => attr.AttributeType.FullName == "Aspire.Hosting.Polyglot.PolyglotMethodNameAttribute");

                var methodName = CamelCase(
                    methodNameAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "MethodName").Value?.ToString()
                    ?? methodNameAttribute?.FixedArguments?.ElementAtOrDefault(0)?.ToString()
                    ?? overload.Name);

                var jsReturnTypeName = FormatJsType(model, returnType);

                var parameterTypes = new List<string>();

                if (indexes.TryGetValue(methodName, out var index))
                {
                    indexes[methodName] = index + 1;
                    methodName = $"{methodName}{(index + 1).ToString(CultureInfo.InvariantCulture)}";
                }
                else
                {
                    indexes[methodName] = 0;
                }

                var parameters = overload.Parameters.Skip(1); // Skip the first parameter (this)

                bool ParameterIsOptionalOrNullable(RoParameterInfo p) => p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType);

                var orderedParameters = parameters.OrderBy(p => p.IsOptional ? 1 : 0).ThenBy(p => model.WellKnownTypes.IsNullableOfT(p.ParameterType) ? 1 : 0);

                const string optionalArgumentName = "optionalArguments";

                var optionalParameters = overload.Parameters.Skip(1).Where(ParameterIsOptionalOrNullable).ToArray();
                var shouldCreateArgsClass = optionalParameters.Length > 1;

                var parameterList = string.Join(", ", orderedParameters.Select(p => FormatArgument(model, p)));
                var jsonParameterList = string.Join(", ", parameters.Select(p => FormatJsonArgument(model, p, prefix: shouldCreateArgsClass && optionalParameters.Contains(p) ? $"{optionalArgumentName}?." : "")));

                string optionalArgsInitSnippet = "";

                if (shouldCreateArgsClass)
                {
                    var parameterType = $"{overload.Name}Args";

                    if (!_overloadParameterClassByMethod.TryGetValue(overload, out var overloadParameterClass))
                    {
                        var k = 1;
                        while (_overloadParameterClassByName.ContainsKey(parameterType))
                        {
                            parameterType = $"{overload.Name}Args{k++}";
                        }

                        overloadParameterClass = $$"""
                        export class {{parameterType}} {
                        """;

                        foreach (var p in optionalParameters)
                        {
                            overloadParameterClass += $"\n      public {p.Name}?: {FormatJsType(model, p.ParameterType)};";
                        }

                        overloadParameterClass += "\n";

                        static bool HasDefaultValue(RoParameterInfo p) => p.RawDefaultValue != null && p.RawDefaultValue != DBNull.Value && p.RawDefaultValue != Missing.Value;

                        overloadParameterClass += $$"""

                            constructor(args: Partial<{{parameterType}}> = {}) {
                        """;

                        foreach (var p in optionalParameters)
                        {
                            if (HasDefaultValue(p))
                            {
                                var defaultValue = "";

                                if (p.ParameterType == model.WellKnownTypes.GetKnownType<string>())
                                {
                                    defaultValue += $" = \"{p.RawDefaultValue}\"";
                                }
                                else if (p.ParameterType == model.WellKnownTypes.GetKnownType<bool>())
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

                                overloadParameterClass += $"\n      this.{p.Name} {defaultValue};";
                            }
                        }

                        overloadParameterClass += "\n      Object.assign(this, args);";
                        overloadParameterClass += "\n    }";
                        overloadParameterClass += "\n}";

                        _overloadParameterClassByMethod[overload] = overloadParameterClass;
                        _overloadParameterClassByName[parameterType] = overloadParameterClass;
                    }

                    parameterTypes.Add(parameterType);

                    parameterList = string.Join(", ", parameters.Except(optionalParameters).Select(p => FormatArgument(model, p)));
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
                writer.WriteLine($$"""

                   /**
                   * {{overload.Name}}
                   * @remarks C# Definition: {{FormatMethodSignature(overload)}}
                   {{string.Join("\n   ", parameters.Select(p => $"* @param {{{FormatJsType(model, p.ParameterType)}}} {p.Name} C# Type: {PrettyPrintCSharpType(p.ParameterType)}"))}}
                   * @returns {{{jsReturnTypeName}}} C# Type: {{PrettyPrintCSharpType(returnType)}}
                   */
                """);

                // Method body - different for proxy types vs builder types
                var isProxyReturnType = jsReturnTypeName.EndsWith("Proxy", StringComparison.Ordinal);
                var isBuilderReturnType = jsReturnTypeName.EndsWith("Builder", StringComparison.Ordinal);

                if (isProxyReturnType)
                {
                    // For proxy types, use invokeStaticMethod (extension method) and wrap result in proxy
                    var methodAssemblyProxy = overload.DeclaringType?.DeclaringAssembly?.Name ?? "";
                    var methodTypeNameProxy = overload.DeclaringType?.FullName ?? "";
                    var extensionArgsProxy = $"builder: this._proxy, {jsonParameterList}";

                    writer.WriteLine($$"""
                      async {{methodName}}({{parameterList}}) : Promise<{{jsReturnTypeName}}> {{{optionalArgsInitSnippet}}
                        const result = await client.invokeStaticMethod('{{methodAssemblyProxy}}', '{{methodTypeNameProxy}}', '{{overload.Name}}', {{{extensionArgsProxy}}});
                        if (result && typeof result === 'object' && '$id' in result) {
                            return new {{jsReturnTypeName}}(new DotNetProxy(result as any));
                        }
                        throw new Error('{{overload.Name}} did not return a marshalled object');
                      };
                    """);
                }
                else if (isBuilderReturnType)
                {
                    // For builder types, generate internal async method and public fluent method
                    var internalMethodName = $"_{methodName}Internal";
                    var thenableReturnType = $"{jsReturnTypeName}Promise";

                    // Get the extension method type info
                    var methodAssembly = overload.DeclaringType?.DeclaringAssembly?.Name ?? "";
                    var methodTypeName = overload.DeclaringType?.FullName ?? "";

                    // Build args including the builder as first param (for extension methods)
                    var extensionArgs = $"builder: this._proxy, {jsonParameterList}";

                    // Generate internal async method using invokeStaticMethod for extension methods
                    writer.WriteLine($$"""
                      /** @internal */
                      async {{internalMethodName}}({{parameterList}}) : Promise<{{jsReturnTypeName}}> {{{optionalArgsInitSnippet}}
                        const result = await client.invokeStaticMethod('{{methodAssembly}}', '{{methodTypeName}}', '{{overload.Name}}', {{{extensionArgs}}});
                        if (result && typeof result === 'object' && '$id' in result) {
                            return new {{jsReturnTypeName}}(new DotNetProxy(result as any));
                        }
                        throw new Error('{{overload.Name}} did not return a marshalled object');
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

                    writer.WriteLine($$"""

                      /**
                       * {{overload.Name}} (fluent chaining)
                       * @remarks C# Definition: {{FormatMethodSignature(overload)}}
                       */
                      {{methodName}}({{parameterList}}): {{thenableReturnType}} {{{optionalArgsInitSnippet}}
                        return new {{thenableReturnType}}(this.{{internalMethodName}}({{argsList}}));
                      }
                    """);
                }
                else
                {
                    // For other types, use invokeStaticMethod for extension methods
                    var methodAssembly2 = overload.DeclaringType?.DeclaringAssembly?.Name ?? "";
                    var methodTypeName2 = overload.DeclaringType?.FullName ?? "";
                    var extensionArgs2 = $"builder: this._proxy, {jsonParameterList}";

                    // For primitive/unknown types (any, string, etc.), return the proxy directly
                    var returnStatement = IsPrimitiveJsType(jsReturnTypeName)
                        ? "return new DotNetProxy(result as any);"
                        : $"return new {jsReturnTypeName}(new DotNetProxy(result as any));";

                    writer.WriteLine($$"""
                      async {{methodName}}({{parameterList}}) : Promise<{{jsReturnTypeName}}> {{{optionalArgsInitSnippet}}
                        const result = await client.invokeStaticMethod('{{methodAssembly2}}', '{{methodTypeName2}}', '{{overload.Name}}', {{{extensionArgs2}}});
                        if (result && typeof result === 'object' && '$id' in result) {
                            {{returnStatement}}
                        }
                        throw new Error('{{overload.Name}} did not return a marshalled object');
                      };
                    """);
                }
            }
        }
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
                if (callbackArgType.IsGenericType &&
                    callbackArgType.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType)
                {
                    var resourceType = callbackArgType.GetGenericArguments()[0];
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
        if (p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType)
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
    /// Checks if a return type is a primitive type that can't be instantiated as a class.
    /// </summary>
    private static bool IsPrimitiveReturnType(ApplicationModel model, RoType type)
    {
        // Check for void
        if (type == model.WellKnownTypes.GetKnownType(typeof(void)))
        {
            return true;
        }

        // Check for primitive/value types that can't be used as class constructors
        var primitiveTypes = new[]
        {
            typeof(bool), typeof(char),
            typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(nint), typeof(nuint),
            typeof(float), typeof(double), typeof(decimal),
            typeof(string), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan)
        };

        foreach (var primitiveType in primitiveTypes)
        {
            if (type == model.WellKnownTypes.GetKnownType(primitiveType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a type is a delegate type (Action, Func, etc.)
    /// </summary>
    private static bool IsDelegateType(ApplicationModel model, RoType type)
    {
        // Check for Action (no generic args)
        if (type == model.WellKnownTypes.GetKnownType(typeof(Action)))
        {
            return true;
        }

        // Check for generic Action<T>, Action<T1, T2>, etc.
        if (type.IsGenericType)
        {
            var genericDef = type.GenericTypeDefinition;
            var actionTypes = new[]
            {
                typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>)
            };
            var funcTypes = new[]
            {
                typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>)
            };

            foreach (var actionType in actionTypes)
            {
                if (genericDef == model.WellKnownTypes.GetKnownType(actionType))
                {
                    return true;
                }
            }

            foreach (var funcType in funcTypes)
            {
                if (genericDef == model.WellKnownTypes.GetKnownType(funcType))
                {
                    return true;
                }
            }
        }

        return false;
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
            if (argType.IsGenericType && argType.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType)
            {
                var resourceType = argType.GetGenericArguments()[0];
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
    /// Checks if a type is a simple/primitive type that doesn't need a proxy wrapper.
    /// </summary>
    private static bool IsSimpleType(ApplicationModel model, RoType type)
    {
        // Check for primitive types
        var primitiveTypes = new[]
        {
            typeof(bool), typeof(char),
            typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(nint), typeof(nuint),
            typeof(float), typeof(double), typeof(decimal),
            typeof(string), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan)
        };

        foreach (var primitiveType in primitiveTypes)
        {
            if (type == model.WellKnownTypes.GetKnownType(primitiveType))
            {
                return true;
            }
        }

        if (type.IsEnum)
        {
            return true;
        }

        return false;
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

        return type switch
        {
            { IsGenericType: true } when model.WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var t) && t == model.WellKnownTypes.IResourceWithConnectionStringType => "IResourceWithConnectionStringBuilder",
            { IsGenericType: true } when model.WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var t) && t.IsInterface => "ResourceBuilder",
            { IsGenericType: true } when type.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType => $"{type.GetGenericArguments()[0].Name}Builder",
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
    /// Generates TypeScript enum definitions for enums used in proxy classes.
    /// </summary>
    private static void GenerateBuilderEnums(TextWriter writer, ApplicationModel model)
    {
        var enumTypes = new HashSet<RoType>();

        // Collect enum types from proxy properties and methods
        foreach (var (_, proxyModel) in model.BuilderModel.ProxyTypes)
        {
            // Check properties
            foreach (var prop in proxyModel.Properties)
            {
                if (prop.PropertyType.IsEnum)
                {
                    enumTypes.Add(prop.PropertyType);
                }
            }

            // Check method parameters and return types
            foreach (var method in proxyModel.Methods.Concat(proxyModel.StaticMethods))
            {
                if (method.ReturnType.IsEnum)
                {
                    enumTypes.Add(method.ReturnType);
                }

                foreach (var param in method.Parameters)
                {
                    if (param.ParameterType.IsEnum)
                    {
                        enumTypes.Add(param.ParameterType);
                    }
                }
            }
        }

        // Generate enum definitions (skip those already in model.ModelTypes to avoid duplicates)
        foreach (var enumType in enumTypes)
        {
            // Skip if this enum is already in ModelTypes (will be generated by GenerateModelClasses)
            if (model.ModelTypes.Contains(enumType))
            {
                continue;
            }

            writer.WriteLine();
            writer.WriteLine($$"""
                export enum {{enumType.Name}} {
                  {{string.Join(",\n  ", enumType.GetEnumNames().Select(x => $"{x} = \"{x}\""))}}
                }
                """);
        }
    }

    /// <summary>
    /// Generates proxy classes from the BuilderModel.ProxyTypes.
    /// </summary>
    private void GenerateBuilderProxyClasses(TextWriter writer, ApplicationModel model)
    {
        foreach (var (type, proxyModel) in model.BuilderModel.ProxyTypes)
        {
            writer.WriteLine();
            writer.WriteLine($$"""
                /**
                 * Proxy for {{type.Name}}.
                 */
                export class {{proxyModel.ProxyClassName}} {
                  constructor(private _proxy: DotNetProxy) {}

                  get proxy(): DotNetProxy { return this._proxy; }
                """);

            // Generate property accessors from reflection
            foreach (var prop in proxyModel.Properties.Where(p => !p.IsStatic))
            {
                var jsType = FormatJsType(model, prop.PropertyType);

                writer.WriteLine($$"""

                  async get{{prop.Name}}(): Promise<{{jsType}}> {
                    const result = await this._proxy.getProperty('{{prop.Name}}');
                    return result as {{jsType}};
                  }
                """);
            }

            // Generate instance methods (with overload disambiguation)
            var methodIndexes = new Dictionary<string, int>();
            foreach (var method in proxyModel.Methods)
            {
                EmitProxyInstanceMethod(writer, model, method, methodIndexes);
            }

            // Generate helper methods
            foreach (var helper in proxyModel.HelperMethods)
            {
                var paramsStr = string.Join(", ", helper.Parameters.Select(p => $"{p.Name}: {p.Type}"));

                writer.WriteLine($$"""

                  /**
                   * {{helper.Documentation ?? helper.Name}}
                   */
                  async {{CamelCase(helper.Name)}}({{paramsStr}}): Promise<{{helper.ReturnType}}> {
                    {{helper.Body}}
                  }
                """);
            }

            // Generate static methods (with overload disambiguation, reusing the same indexes)
            foreach (var method in proxyModel.StaticMethods)
            {
                EmitProxyStaticMethod(writer, model, type, method, methodIndexes);
            }

            writer.WriteLine("}");
        }
    }

    private void EmitProxyInstanceMethod(TextWriter writer, ApplicationModel model, RoMethod method, Dictionary<string, int> methodIndexes)
    {
        var baseName = CamelCase(method.Name);

        // Skip property getters/setters
        if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
        {
            return;
        }

        // Handle method overloading by adding numeric suffix
        string methodName;
        if (methodIndexes.TryGetValue(baseName, out var index))
        {
            methodIndexes[baseName] = index + 1;
            methodName = $"{baseName}{index + 1}";
        }
        else
        {
            methodIndexes[baseName] = 0;
            methodName = baseName;
        }

        // Get parameters
        var parameters = method.Parameters;
        var paramsList = new List<string>();
        var argsForObject = new List<string>();

        foreach (var param in parameters)
        {
            var jsType = FormatJsType(model, param.ParameterType);
            var paramName = CamelCase(param.Name);
            paramsList.Add($"{paramName}: {jsType}");

            // Check if this parameter is a delegate/callback type
            if (IsDelegateType(model, param.ParameterType))
            {
                // Wrap with registerCallback
                argsForObject.Add($"{paramName}: registerCallback({paramName})");
            }
            else
            {
                argsForObject.Add(paramName);
            }
        }

        var paramsStr = string.Join(", ", paramsList);

        // Use object syntax for arguments: { param1, param2 } or { param1: registerCallback(param1) }
        var argsObjectStr = argsForObject.Count > 0
            ? $"{{ {string.Join(", ", argsForObject)} }}"
            : "{}";

        var returnType = method.ReturnType;
        var jsReturnType = FormatJsType(model, returnType);
        var isVoid = returnType.Name == "Void";

        // Add type assertion for non-void returns
        var returnExpr = isVoid
            ? $"await this._proxy.invokeMethod('{method.Name}', {argsObjectStr});"
            : $"return await this._proxy.invokeMethod('{method.Name}', {argsObjectStr}) as {jsReturnType};";

        writer.WriteLine($$"""

          async {{methodName}}({{paramsStr}}): Promise<{{(isVoid ? "void" : jsReturnType)}}> {
            {{returnExpr}}
          }
        """);
    }

    private void EmitProxyStaticMethod(TextWriter writer, ApplicationModel model, RoType declaringType, RoMethod method, Dictionary<string, int> methodIndexes)
    {
        var baseName = CamelCase(method.Name);

        // Handle method overloading by adding numeric suffix
        string methodName;
        if (methodIndexes.TryGetValue(baseName, out var index))
        {
            methodIndexes[baseName] = index + 1;
            methodName = $"{baseName}{index + 1}";
        }
        else
        {
            methodIndexes[baseName] = 0;
            methodName = baseName;
        }

        // Get parameters
        var parameters = method.Parameters;
        var paramsList = new List<string>();
        var argsForObject = new List<string>();

        foreach (var param in parameters)
        {
            var jsType = FormatJsType(model, param.ParameterType);
            var paramName = CamelCase(param.Name);
            paramsList.Add($"{paramName}: {jsType}");

            // Check if this parameter is a delegate/callback type
            if (IsDelegateType(model, param.ParameterType))
            {
                argsForObject.Add($"{paramName}: registerCallback({paramName})");
            }
            else
            {
                argsForObject.Add(paramName);
            }
        }

        var paramsStr = string.Join(", ", paramsList);

        // Use object syntax for arguments
        var argsObjectStr = argsForObject.Count > 0
            ? $"{{ {string.Join(", ", argsForObject)} }}"
            : "{}";

        var returnType = method.ReturnType;
        var jsReturnType = FormatJsType(model, returnType);
        var isVoid = returnType.Name == "Void";

        // Static method needs client parameter for invoking
        var clientParam = paramsList.Count > 0 ? "client: RemoteAppHostClient, " : "client: RemoteAppHostClient";

        // Add type assertion for non-void returns
        var returnExpr = isVoid
            ? $"await client.invokeStaticMethod('{declaringType.Namespace}', '{declaringType.Name}', '{method.Name}', {argsObjectStr});"
            : $"return await client.invokeStaticMethod('{declaringType.Namespace}', '{declaringType.Name}', '{method.Name}', {argsObjectStr}) as {jsReturnType};";

        writer.WriteLine($$"""

          static async {{methodName}}({{clientParam}}{{paramsStr}}): Promise<{{(isVoid ? "void" : jsReturnType)}}> {
            {{returnExpr}}
          }
        """);
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
