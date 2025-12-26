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

    // Types that need proxy wrapper classes (callback parameter types)
    private readonly HashSet<string> _callbackProxyTypes = [];
    private readonly Dictionary<string, RoType> _callbackProxyTypesByName = [];

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

    /// <inheritdoc />
    public Dictionary<string, string> GenerateIntegration(IntegrationModel integration)
    {
        // Integration methods are included in the main distributed-application.ts
        return [];
    }

    /// <inheritdoc />
    public Dictionary<string, string> GenerateResource(ResourceModel resource)
    {
        // Resource classes are included in the main distributed-application.ts
        return [];
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
        var escapedAppPath = OperatingSystem.IsWindows()
            ? model.AppPath.Replace("\\", "\\\\")
            : model.AppPath;

        // Write the header with imports and utility functions
        writer.WriteLine($$"""
        import { RemoteAppHostClient, registerCallback, DotNetProxy, ListProxy, wrapIfProxy } from './RemoteAppHostClient.js';
        import { AnyInstruction, CreateBuilderInstruction, RunBuilderInstruction } from './types.js';

        // Get socket path from environment variable (set by aspire run)
        const socketPath = process.env.REMOTE_APP_HOST_SOCKET_PATH;
        if (!socketPath) {
            throw new Error('REMOTE_APP_HOST_SOCKET_PATH environment variable not set. Please run with "aspire run".');
        }

        const client = new RemoteAppHostClient(socketPath);

        const _name = Symbol('_name');
        let source: string = "";
        let instructions: any[] = [];

        function writeLine(code: string) {
          source += code + '\n';
        }

        async function sendInstruction(instruction: AnyInstruction) {
          instructions.push(instruction);
          const result = await client.executeInstruction(instruction);
          // Check for error responses (defensive - JSON-RPC errors will throw automatically)
          if (result && typeof result === 'object' && 'success' in result && result.success === false) {
            const errorMessage = 'error' in result ? String(result.error) : 'Unknown error';
            throw new Error(`Instruction failed: ${errorMessage}`);
          }
        }

        function capture(fn: () => void) : string {
          var tmp = source;
          source = "";
          fn();
          var result = source;
          source = tmp;
          return result;
        }

        function emitLinePragma() {
          const err = new Error();
          (Error as any).captureStackTrace?.(err, emitLinePragma);
          const frame = err.stack?.split('\n')[2] || '';
          const m = /\s+at\s+(?:.+\s\()?(.+):(\d+):(\d+)\)?/.exec(frame);
          if (!m) return;
          const [, file, line] = m;
          writeLine(`#line ${line} "${file}"`);
        }

        function convertNullable<T>(value?: T): string {
          if (value === null || value === undefined) {
            return "default";
          }
          if (typeof value === 'string') {
            return `"${value}"`;
          } else if (Array.isArray(value)) {
            return convertArray(value);
          } else {
            return `${value}`;
          }
        }

        function convertArray<T>(array?: T[]): string {
          if (!array) {
            return "default";
          }
          if (array.length === 0) {
            return "[]";
          }
          var values = array?.map((item) => {
            if (typeof item === 'string') {
              return `"${item}"`;
            }
            else if (Array.isArray(item)) {
              return convertArray(item);
            }
            else if (item === null || item === undefined) {
                return "null";
            } else {
              return item;
            }
          }).join(', ');

          return `[${values}]`;
        }

        export async function createBuilder(args: string[] = []): Promise<DistributedApplicationBuilder> {
            const distributedApplicationBuilder = new DistributedApplicationBuilder(args);

            console.log('🔌 Connecting to GenericAppHost...');

            while (true) {
              try {
                await client.connect();
                await client.ping();
                console.log('✅ Connected successfully!');
                break;
              } catch (error) {
                await new Promise(resolve => setTimeout(resolve, 1000));
              }
            }

            const createBuilderInstruction: CreateBuilderInstruction = {
              name: 'CREATE_BUILDER',
              builderName: distributedApplicationBuilder[_name],
              projectDirectory: process.cwd(),
              args: args
            };

            await sendInstruction(createBuilderInstruction);

            return distributedApplicationBuilder;
        }

        export class DistributedApplication {
          constructor(private builderName: string) {
          }
          async run() {
            writeLine(`${this.builderName}.Build().Run();`);
            writeLine('}');
            writeLine('catch (Exception ex)');
            writeLine('{');
            writeLine('    Helpers.PrintException(ex);');
            writeLine('    Environment.Exit(1);');
            writeLine('}');

            const runBuilderInstruction: RunBuilderInstruction = {
                    name: 'RUN_BUILDER',
                    builderName: this.builderName
                };

            sendInstruction(runBuilderInstruction);

            console.log("Application is running. Press Ctrl+C to stop...");

            process.on("SIGINT", () => {
              console.log("\nStopping application...");
                client.disconnect();
                console.log(`👋 Disconnected from GenericAppHost`);
              process.exit(0);
            });
          }
        }

        abstract class DistributedApplicationBuilderBase {
          constructor(private args: string[]) {
            writeLine('try {');
            this[_name] = `appBuilder${DistributedApplicationBuilderBase.index++}`;
            writeLine('var options = new DistributedApplicationOptions');
            writeLine('{');
            writeLine('    Args = args ?? []')
            writeLine('};');
            writeLine('typeof(DistributedApplicationOptions).GetProperty("ProjectDirectory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(options, @"{{escapedAppPath}}");');
            writeLine(`var ${this[_name]} = DistributedApplication.CreateBuilder(options);`);
          }

          build() {
            return new DistributedApplication(this[_name]);
          }

          private [_name]: string;
          private static index: number = 1;
        }

        abstract class ReferenceClass {
          constructor(protected builder: DistributedApplicationBuilderBase, protected cstype: string, prefix: string) {
            this[_name] = `${prefix}${ReferenceClass.index++}`;
            writeLine(`${cstype} ${this[_name]};`);
          }

          private [_name]: string;
          private static index: number = 1;
        }
        """);

        // Generate DistributedApplicationBuilder class with methods from all integrations
        writer.WriteLine("export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {");
        foreach (var integration in model.IntegrationModels.Values)
        {
            GenerateMethods(writer, model, integration.IDistributedApplicationBuilderExtensionMethods, "this");
        }
        writer.WriteLine("}");

        // Generate base resource builder classes
        writer.WriteLine("""
          export class IResourceWithConnectionStringBuilder extends ReferenceClass {
            constructor(builder: DistributedApplicationBuilderBase) {
              super(builder, "IResourceBuilder<IResourceWithConnectionString>", "resourceWithConnectionStringBuilder");
            }
          }

          export class ResourceBuilder extends ReferenceClass {
            constructor(builder: DistributedApplicationBuilderBase, cstype: string) {
              super(builder, cstype, "resourceBuilder");
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
        foreach (var (typeName, roType) in _callbackProxyTypesByName)
        {
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
                """);

            // Generate typed property accessors
            foreach (var property in roType.Properties)
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
        if (_callbackProxyTypesByName.ContainsKey(propertyType.Name))
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
            else
            {
                textWriter.WriteLine();
                textWriter.WriteLine($$"""
                export class {{SanitizeClassName(type.Name)}} extends ReferenceClass {
                  constructor(builder: DistributedApplicationBuilderBase) {
                      super(builder, "{{type.Name}}", "{{CamelCase(type.Name)}}");
                  }
                }
                """);
            }
        }
    }

    private static string SanitizeClassName(string name) => name.Replace("+", "_");

    private void GenerateResourceClasses(TextWriter textWriter, ApplicationModel model)
    {
        foreach (var resourceModel in model.ResourceModels.Values)
        {
            EmitResourceClass(textWriter, model, resourceModel);
        }
    }

    private void EmitResourceClass(TextWriter textWriter, ApplicationModel model, ResourceModel resourceModel)
    {
        var resourceName = SanitizeClassName(resourceModel.ResourceType.Name);
        textWriter.WriteLine();
        textWriter.WriteLine($$"""
                export class {{resourceName}}Builder extends ReferenceClass {
                  constructor(builder: DistributedApplicationBuilderBase, cstype: string = "IResourceBuilder<{{resourceModel.ResourceType.FullName}}>") {
                      super(builder, cstype, "{{CamelCase(resourceName)}}Builder");
                  }
                """);

        GenerateMethods(textWriter, model, resourceModel.IResourceTypeBuilderExtensionsMethods, "this.builder");

        textWriter.WriteLine("}");
    }

    private void GenerateMethods(TextWriter writer, ApplicationModel model, IEnumerable<RoMethod> extensionMethods, string ctorArgs)
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

                var preferredMethodName = methodNameAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "MethodName").Value?.ToString()
                    ?? methodNameAttribute?.FixedArguments?.ElementAtOrDefault(0)?.ToString()
                    ?? overload.Name;
                var jsReturnTypeName = FormatJsType(model, returnType);

                var parameterTypes = new List<string>();

                var methodName = model.TryGetMapping(overload.Name, overload.Parameters.Skip(1).Select(p => p.ParameterType).ToArray(), out var mapping)
                    ? CamelCase(mapping.GeneratedName)
                    : CamelCase(preferredMethodName);

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
                var csParameterList = string.Join(", ", parameters.Select(p => FormatCsArgument(model, p)));
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

                    var csParameters = new List<string>();
                    foreach (var parameter in parameters)
                    {
                        if (ParameterIsOptionalOrNullable(parameter))
                        {
                            csParameters.Add(FormatCsArgument(model, parameter, $"{optionalArgumentName}?."));
                        }
                        else
                        {
                            csParameters.Add(FormatCsArgument(model, parameter));
                        }
                    }

                    csParameterList = string.Join(", ", csParameters);

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

                // Method body
                writer.WriteLine($$"""
                  async {{methodName}}({{parameterList}}) : Promise<{{jsReturnTypeName}}> {{{optionalArgsInitSnippet}}
                    emitLinePragma();
                    var result = new {{jsReturnTypeName}}({{ctorArgs}});
                    writeLine(`${result[_name]} = ${this[_name]}.{{overload.Name}}({{csParameterList}});`);
                    await sendInstruction({
                        name: 'INVOKE',
                        source: this[_name],
                        target: result[_name],
                        methodAssembly: '{{overload.DeclaringType?.DeclaringAssembly.Name}}',
                        methodType: '{{overload.DeclaringType?.FullName}}',
                        methodName: '{{overload.Name}}',
                        methodArgumentTypes: [{{string.Join(", ", overload.Parameters.Select(p => "'" + p.ParameterType.FullName + "'"))}}],
                        metadataToken: {{overload.MetadataToken}},
                        args: {{{jsonParameterList}}}
                    });
                    return result;
                  };
                """);
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

        // Handle Action<IResourceBuilder<T>> - these use inline capture, not JSON-RPC callbacks
        // So we don't pass them in the args (the C# code is generated inline)
        if (p.ParameterType.IsGenericType &&
            p.ParameterType.GenericTypeDefinition == actionType &&
            p.ParameterType.GetGenericArguments()[0] is { } genericArgument &&
            genericArgument.IsGenericType &&
            genericArgument.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType)
        {
            // This callback is handled by inline capture in FormatCsArgument, not via JSON-RPC
            // Return null for this parameter in the args
            return $"{p.Name}: null";
        }

        // Handle other delegate types - register callback and pass ID
        if (IsDelegateType(model, p.ParameterType))
        {
            // Register the callback and pass the callback ID to the server
            // The server will invoke it via JSON-RPC when the C# delegate is called

            // Check if this is an Action<T> with a complex type that has a proxy wrapper
            if (p.ParameterType.IsGenericType &&
                p.ParameterType.GenericTypeDefinition == actionType &&
                p.ParameterType.GetGenericArguments()[0] is { } callbackArgType &&
                !IsSimpleType(model, callbackArgType) &&
                !callbackArgType.IsGenericType) // Skip generic types like IResourceBuilder<T>
            {
                var proxyTypeName = $"{callbackArgType.Name}Proxy";
                // Wrap the callback to convert DotNetProxy to the expected proxy type
                if (p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType))
                {
                    return $"{p.Name}: {prefix}{p.Name} ? registerCallback((arg: DotNetProxy) => {prefix}{p.Name}(new {proxyTypeName}(arg))) : null";
                }
                return $"{p.Name}: registerCallback((arg: DotNetProxy) => {prefix}{p.Name}(new {proxyTypeName}(arg)))";
            }

            if (p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType))
            {
                return $"{p.Name}: {prefix}{p.Name} ? registerCallback({prefix}{p.Name}) : null";
            }
            return $"{p.Name}: registerCallback({prefix}{p.Name})";
        }

        var result = p.Name!;
        result += $": {prefix}{p.Name!}";

        if (p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType)
        {
            result += "?.[_name]";
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

    private static string FormatCsArgument(ApplicationModel model, RoParameterInfo p) => FormatCsArgument(model, p, "");

    private static string FormatCsArgument(ApplicationModel model, RoParameterInfo p, string prefix)
    {
        string result;

        var actionType = model.WellKnownTypes.GetKnownType(typeof(Action<>));

        // Handle Action<IResourceBuilder<T>> callbacks with inline capture
        if (p.ParameterType.IsGenericType &&
            p.ParameterType.GenericTypeDefinition == actionType &&
            p.ParameterType.GetGenericArguments()[0] is { } genericArgument &&
            genericArgument.IsGenericType &&
            genericArgument.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType)
        {
            var resourceType = genericArgument.GetGenericArguments()[0];
            var resourceName = SanitizeClassName(resourceType.Name);

            return $$"""
                ${ capture(() => {
                        if ({{prefix}}{{p.Name}}) {
                          writeLine('builder =>');
                          writeLine('{');
                          let r = new {{resourceName}}Builder(result.builder);
                          writeLine(`${r[_name]} = builder;`);
                          {{prefix}}{{p.Name}}(r);
                          writeLine('}');
                          } else { writeLine('builder => { }'); }
                        }) }
                """;
        }
        // Handle other delegate types (Action<T>, Func<T,R>, etc.) with bidirectional JSON-RPC callbacks
        // For C# code generation, we use a placeholder since the actual callback is handled via JSON-RPC
        else if (IsDelegateType(model, p.ParameterType))
        {
            // The actual callback invocation happens via JSON-RPC, not in the generated C# code
            // Use null as placeholder in the C# code - the real callback is passed in instruction args
            return "null /* callback handled via JSON-RPC */";
        }
        else if (p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType)
        {
            result = $"{prefix}{p.Name}?.[_name]";
        }
        else if (model.ModelTypes.Contains(p.ParameterType) && !p.ParameterType.IsEnum)
        {
            result = $"{prefix}{p.Name}?.[_name]";
        }
        else
        {
            result = $"{prefix}{p.Name}";
        }

        // Value conversion
        if (p.ParameterType.IsArray)
        {
            result = $"${{convertArray({result})}}";
        }
        else if (p.IsOptional || model.WellKnownTypes.IsNullableOfT(p.ParameterType))
        {
            result = $"${{convertNullable({result})}}";
        }
        else if (p.ParameterType == model.WellKnownTypes.GetKnownType<string>())
        {
            result = $"\"${{{result}}}\"";
        }
        else if (p.ParameterType.IsEnum)
        {
            result = $"{p.ParameterType.Name}.${{{result}}}";
        }
        else
        {
            result = $"${{{result}}}";
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

            // Check if this is an IResourceBuilder<T> type - these use inline capture, not JSON-RPC callbacks
            // So they should use the regular formatted type, not a proxy wrapper
            if (argType.IsGenericType && argType.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType)
            {
                argTypes.Add($"p{i}: {FormatJsType(model, argType)}");
            }
            // Register complex types for proxy wrapper generation
            else if (!IsSimpleType(model, argType) && !string.IsNullOrEmpty(typeName))
            {
                if (_callbackProxyTypes.Add(typeName))
                {
                    _callbackProxyTypesByName[typeName] = argType;
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

    private string FormatJsType(ApplicationModel model, RoType type)
    {
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
            { } when model.ModelTypes.Contains(type) => SanitizeClassName(type.Name),
            { } when type.IsEnum => "any",
            { IsGenericParameter: true } => "any", // Generic type parameters like T, TBuilder
            _ => "any"
        };
    }
}
