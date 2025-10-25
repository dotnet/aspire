// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Aspire.Cli.Interaction;
using Aspire.Cli.Rosetta.Models;
using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta.Generators;

internal sealed class JavaScriptCodeGenerator(ApplicationModel appModel, IInteractionService interactionService) : ICodeGenerator
{
    private const string ModulePath = "./.modules";

    // Custom record-like classes that contain the overload parameters
    private readonly Dictionary<RoMethod, string> _overloadParameterClassByMethod = [];
    private readonly Dictionary<string, string> _overloadParameterClassByName = [];
    private readonly ApplicationModel _appModel = appModel;

    public IReadOnlyList<string> GenerateDistributedApplication()
    {
        var appPath = _appModel.AppPath;

        var modulesPath = Path.Combine(appPath, ModulePath);
        Directory.CreateDirectory(modulesPath);

        var filename = Path.Combine(modulesPath, "distributed-application.ts");
        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream);

        GenerateDistributedApplication(writer);

        return [filename];
    }

    // For testing purposes
    internal void GenerateDistributedApplication(TextWriter writer)
    {
        var appPath = _appModel.AppPath;
        var escapedAppPath = OperatingSystem.IsWindows() ? appPath.Replace("\\", "\\\\") : appPath;

        string content = $$"""
        import { RemoteAppHostClient } from './RemoteAppHostClient.js';
        import { AnyInstruction, CreateBuilderInstruction, RunBuilderInstruction } from './types.js';
        import { spawn, ChildProcess } from 'child_process';
        import { createHash } from 'crypto';

        // use a fixed pipe name for the app such that it can reconnect to a watched generic host
        const pipeName = createHash('sha256').update(process.cwd(), 'utf8').digest('hex');
        
        const client = new RemoteAppHostClient(pipeName);

        const _name = Symbol('_name');
        let source: string = "";
        let instructions: any[] = [];

        function writeLine(code: string) {
          source += code + '\n';
        }

        async function sendInstruction(instruction: AnyInstruction) {
          instructions.push(instruction);

          const result = await client.executeInstruction(instruction);
          // console.log(`   ${instruction.name} result:`, JSON.stringify(result, null, 2));
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
          // Build an Error whose stack trace starts *after* this helper
          const err = new Error();
          (Error as any).captureStackTrace?.(err, emitLinePragma);

          // Pick the desired frame: depth 0 = direct caller
          const frame = err.stack?.split('\n')[2] || '';

          // Frame looks like: "    at myFn (/abs/path/file.js:42:13)"
          const m = /\s+at\s+(?:.+\s\()?(.+):(\d+):(\d+)\)?/.exec(frame);
          if (!m) return;           // couldn't parse - bail quietly

          const [, file, line] = m;
          writeLine(`#line ${line} "${file}"`);
          // sendInstruction({ name: 'pragma', type: 'line', value: `${line} "${file}"` });
        }
        
        function convertNullable<T>(value?: T): string {
          // Handle null and undefined
          if (value === null || value === undefined) {
            return "default";
          }
          // Handle strings vs other types and quote them
          if (typeof value === 'string') {
            return `"${value}"`;
          } else if (Array.isArray(value)) {
            return convertArray(value);
          } else {
            return `${value}`;
          }
        }

        function convertArray<T>(array?: T[]): string {
          // Handle empty array and undefined
          if (!array) {
            return "default";
          }
          if (array.length === 0) {
            return "[]";
          }
          // Handle strings vs other types and quote them,
          // handle nulls and undefined
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

            // Start the rosetta remote host process
        
            console.log(`🚀 Starting generic app host...`);
        
            const rosettaProcess: ChildProcess = spawn('aspire', ['polyglot', 'serve', '-o', process.cwd()], {
              stdio: 'inherit',
              shell: false,
              env: { ...process.env, 'REMOTE_APP_HOST_PIPE_NAME': pipeName, 'REMOTE_APP_HOST_PID': process.pid.toString() }
            });
        
            // Give the process more time to start up and establish the named pipe
            console.log('🔌 Connecting...');
        
            while (true) {
              try {
                await client.connect();
                await client.ping();
                console.log('✅ Connected successfully!');
        
                break
              } catch (error) {
                // Failed to connect, wait an try again
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
  
            // console.log(source);
            // console.log("//--INSTRUCTIONS--//");
            // console.log("/*");
            // console.log(JSON.stringify(instructions, null, 2));
            // console.log("*/");

            console.log("Application is running. Press Ctrl+C to stop...");
  
            process.on("SIGINT", () => {
              console.log("\nStopping application...");
                client.disconnect();
                console.log(`👋 Disconnected from Generic App Host`);
  
              process.exit(0); // Exit the process if needed
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
            writeLine('// Hack to work around the fact that the ProjectDirectory is internally set');
            writeLine('// to the assembly location. This is a workaround until we expose the API');
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
        """;

        writer.WriteLine(content);

        /*
            export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {
                addRedis(name: string, port?: number | null) {
                    this.addCodeLine(`builder.AddRedis("${name}", ${port});`);
                    return new RedisResourceBuilder(this);
                };

                ...
            }
        */

        writer.WriteLine("export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {");
        foreach (var integration in _appModel.IntegrationModels.Values)
        {
            GenerateMethods(writer, integration.IDistributedApplicationBuilderExtensionMethods, "this");
        }
        writer.WriteLine("}");

        writer.WriteLine("""
          // Special case for IResourceWithConnectionString since it's the only interface used as a builder result
          export class IResourceWithConnectionStringBuilder extends ReferenceClass {
            constructor(builder: DistributedApplicationBuilderBase) {
              super(builder, "IResourceBuilder<IResourceWithConnectionString>", "resourceWithConnectionStringBuilder");
            }
          }
                      
          export class ResourceBuilder extends ReferenceClass {
            constructor(builder: DistributedApplicationBuilderBase, cstype: string) {
              super(builder, cstype, "resourceBuilder");
            }

          """);
        writer.WriteLine("}");

        GenerateResourceClasses(writer);

        GenerateModelClasses(writer);

        GenerateParameterClasses(writer);
    }

    private void GenerateParameterClasses(TextWriter writer)
    {
        foreach (var overloadParameterType in _overloadParameterClassByMethod.Values)
        {
            writer.WriteLine(overloadParameterType);
        }
    }

    private void GenerateModelClasses(TextWriter textWriter)
    {
        foreach (var type in _appModel.ModelTypes)
        {
            if (type.IsEnum)
            {
                // Generate enum class
                textWriter.WriteLine();
                textWriter.WriteLine($$"""
                    export enum {{type.Name}} {
                      {{String.Join(", ", type.GetEnumNames().Select(x => $"{x} = \"{x}\""))}}
                    }
                    """);
                continue;
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

  /// <summary>
  /// Sanitize a class name by replacing '+' with '_' to handle nested classes.
  /// </summary>
  private static string SanitizeClassName(string name) => name.Replace("+", "_");

  private void GenerateResourceClasses(TextWriter textWriter)
  {
    foreach (var resourceModel in _appModel.ResourceModels.Values)
    {
      EmitResourceClass(textWriter, resourceModel);
    }

    void EmitResourceClass(TextWriter textWriter, ResourceModel model)
    {
      var resourceName = SanitizeClassName(model.ResourceType.Name);
      textWriter.WriteLine();
      textWriter.WriteLine($$"""
                export class {{resourceName}}Builder extends ReferenceClass {
                  constructor(builder: DistributedApplicationBuilderBase, cstype: string = "IResourceBuilder<{{model.ResourceType.FullName}}>") {
                      super(builder, cstype, "{{CamelCase(resourceName)}}Builder");
                  }
                """);

      GenerateMethods(textWriter, model.IResourceTypeBuilderExtensionsMethods, "this.builder");

      textWriter.WriteLine($"}}");
    }
  }

    private void GenerateMethods(TextWriter writer, IEnumerable<RoMethod> extensionMethods, string ctorArgs)
    {
        foreach (var methodGroups in extensionMethods.GroupBy(m => m.Name))
        {
            var indexes = new Dictionary<string, int>();
            var overloads = methodGroups.OrderBy(m => m.Parameters.Count).ToArray();

            foreach (var overload in overloads)
            {
                var methodNameAttribute = overload.GetCustomAttributes()
                    .FirstOrDefault(attr => attr.AttributeType.FullName == "Aspire.Hosting.Polyglot.PolyglotMethodNameAttribute");

                var preferredMethodName = methodNameAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "MethodName").Value?.ToString()
                    ?? methodNameAttribute?.FixedArguments.ElementAtOrDefault(0)?.ToString()
                    ?? overload.Name;

                // The return type is either `RedisResourceBuilder` for `IResourceBuilder<T>`
                // or the actual return type of the extension method, e.g. EndpointReference

                var returnType = overload.ReturnType;

                var jsReturnTypeName = FormatJsType(returnType);

                var parameterTypes = new List<string>();

                var methodName = _appModel.TryGetMapping(overload.Name, overload.Parameters.Skip(1).Select(p => p.ParameterType).ToArray(), out var mapping)
                    ? CamelCase(mapping.GeneratedName)
                    : CamelCase(preferredMethodName);

                if (indexes.TryGetValue(methodName, out var index))
                {
                    indexes[methodName] = index + 1;
                    methodName = $"{methodName}{index.ToString(CultureInfo.InvariantCulture) + 1}";
                }
                else
                {
                    indexes[methodName] = 0;
                }

                var parameters = overload.Parameters.Skip(1); // Skip the first parameter (this)

                bool ParameterIsOptionalOrNullable(RoParameterInfo p) => p.IsOptional || _appModel.WellKnownTypes.IsNullableOfT(p.ParameterType);

                // Push optional and nullable parameters to the end of the list
                var orderedParameters = parameters.OrderBy(p => p.IsOptional ? 1 : 0).ThenBy(p => _appModel.WellKnownTypes.IsNullableOfT(p.ParameterType) ? 1 : 0);

                const string optionalArgumentName = "optionalArguments";

                var optionalParameters = overload.Parameters.Skip(1).Where(ParameterIsOptionalOrNullable).ToArray();

                // When there are more than 1 optional parameters, we need to generate a class
                var shouldCreateArgsClass = optionalParameters.Length > 1;

                var parameterList = string.Join(", ", orderedParameters.Select(FormatArgument)); // name: string, port?: number | null
                var csParameterList = string.Join(", ", parameters.Select(FormatCsArgument)); // "${name}", ${port}
                var jsonParameterList = string.Join(", ", parameters.Select(p => FormatJsonArgument(p, prefix: shouldCreateArgsClass && optionalParameters.Contains(p) ? $"{optionalArgumentName}?." : ""))); // name: name, port: port

                string optionalArgsInitSnippet = "";

                if (shouldCreateArgsClass)
                {
                    var parameterType = $"{overload.Name}Args";

                    // We cache the resulting type since multiple classes use the same methods
                    if (!_overloadParameterClassByMethod.TryGetValue(overload, out var overloadParameterClass))
                    {
                        var k = 1;
                        while (_overloadParameterClassByName.ContainsKey(parameterType))
                        {
                            parameterType = $"{overload.Name}Args{k++}";
                        }
                        ;

                        // Generate the type
                        overloadParameterClass = $$"""
                        export class {{parameterType}} {
                        """;

                        // Generate fields for the optional parameters
                        foreach (var p in optionalParameters)
                        {
                            overloadParameterClass += $"\n      public {p.Name}?: {FormatJsType(p.ParameterType)};";
                        }

                        overloadParameterClass += "\n";

                        // These are the two values that are assigned to RawDefaultValue when there is no default value, based on the value of IsOptional
                        // c.f. https://source.dot.net/#System.Reflection.MetadataLoadContext/System/Reflection/TypeLoading/Parameters/Ecma/EcmaFatMethodParameter.cs,48
                        // And also c.f. the RoParameterInfo.cs implementation
                        static bool HasDefaultValue(RoParameterInfo p) => p.RawDefaultValue != null && p.RawDefaultValue != DBNull.Value && p.RawDefaultValue != Missing.Value;

                        // Make a copy ctor
                        // constructor(args: Partial <Type> = { }) {
                        //     Object.assign(this, args);
                        // }

                        overloadParameterClass += $$"""

                            constructor(args: Partial<{{parameterType}}> = {}) {
                        """;

                        // Handle parameters with default values
                        foreach (var p in optionalParameters)
                        {
                            if (HasDefaultValue(p))
                            {
                                var defaultValue = "";

                                if (p.ParameterType == _appModel.WellKnownTypes.GetKnownType<string>())
                                {
                                    defaultValue += $" = \"{p.RawDefaultValue}\"";
                                }
                                else if (p.ParameterType == _appModel.WellKnownTypes.GetKnownType<bool>())
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

                        // Make a copy ctor
                        overloadParameterClass += "\n      Object.assign(this, args);";

                        overloadParameterClass += "\n    }";

                        overloadParameterClass += "\n}";

                        _overloadParameterClassByMethod[overload] = overloadParameterClass;
                        _overloadParameterClassByName[parameterType] = overloadParameterClass;
                    }

                    parameterTypes.Add(parameterType);

                    parameterList = String.Join(", ", parameters.Except(optionalParameters).Select(FormatArgument)); // name: string, port?: number | null, optionalArguments?: OptionalType
                    if (parameterList.Length > 0)
                    {
                        parameterList += ", ";
                    }
                    parameterList += $"{optionalArgumentName}: {parameterType} = new {parameterType}()";

                    var csParameters = new List<string>();
                    foreach (var parameter in parameters)
                    {
                        var parameterName = parameter.Name;

                        if (ParameterIsOptionalOrNullable(parameter))
                        {
                            csParameters.Add(FormatCsArgument(parameter, $"{optionalArgumentName}?."));
                        }
                        else
                        {
                            csParameters.Add(FormatCsArgument(parameter));
                        }
                    }

                    csParameterList = String.Join(", ", csParameters); // "${args.name}", ${args.port}

                    optionalArgsInitSnippet = $$"""

                        {{optionalArgumentName}} = Object.assign(new {{parameterType}}(), {{optionalArgumentName}});
                    """;
                }

                // Generate JSDoc comments
                writer.WriteLine($$"""

                   /**
                   * {{overload.Name}}
                   * @remarks C# Definition: {{FormatMethodSignature(overload)}}
                   {{String.Join("\n   ", parameters.Select(p => $"* @param {{{FormatJsType(p.ParameterType)}}} {p.Name} C# Type: {PrettyPrintCSharpType(p.ParameterType)}"))}}
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

        // Add generic constraints

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

    private string FormatJsonArgument(RoParameterInfo p, string prefix)
    {
        var result = p.Name!;
        result += $": {prefix}{p.Name!}";

        if (p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType)
        {
            result += "?.[_name]";
        }

        // Undefined arguments are not serialized in JSON, so if an optional argument is not provided, we need to force null
        if (p.IsOptional || _appModel.WellKnownTypes.IsNullableOfT(p.ParameterType))
        {
            result = $"{result} || null";
        }

        return result;
    }

    private string FormatArgument(RoParameterInfo p)
    {
        var result = p.Name!;
        var IsNullableOfT = _appModel.WellKnownTypes.IsNullableOfT(p.ParameterType);
        if (p.IsOptional || IsNullableOfT)
        {
            result += "?";
        }

        if (IsNullableOfT)
        {
            result += $": {FormatJsType(p.ParameterType.GetGenericArguments()[0])} | null";
        }
        else
        {
            result += $": {FormatJsType(p.ParameterType)}";
        }

        return result;
    }

    private string FormatCsArgument(RoParameterInfo p) => FormatCsArgument(p, "");

    private string FormatCsArgument(RoParameterInfo p, string prefix)
    {
        string result;

        var actionType = _appModel.WellKnownTypes.GetKnownType(typeof(Action<>));

        // NOTE: Callbacks are a challenge. Currently, callbacks are projected to run inline.
        // Aspire integrations should indicate if callbacks are safe to run inline or need to be deferred.
        if (p.ParameterType.IsGenericType &&
            p.ParameterType.GenericTypeDefinition == actionType &&
            p.ParameterType.GetGenericArguments()[0] is { } genericArgument &&
            genericArgument.IsGenericType &&
            genericArgument.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType)
        {
            // This is a resource builder
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

        // Value resolution
        else if (p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType)
        {
            result = $"{prefix}{p.Name}?.[_name]";
        }
        else if (_appModel.ModelTypes.Contains(p.ParameterType) && !p.ParameterType.IsEnum)
        {
            result = $"{prefix}{p.Name}?.[_name]";
        }
        else
        {
            result = $"{prefix}{p.Name}";
        }

        // Now determine if we need to convert the value
        if (p.ParameterType.IsArray)
        {
            result = $"${{convertArray({result})}}";
        }
        else if (p.IsOptional || _appModel.WellKnownTypes.IsNullableOfT(p.ParameterType))
        {
            result = $"${{convertNullable({result})}}";
        }
        else if (p.ParameterType == _appModel.WellKnownTypes.GetKnownType<string>())
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

    private string FormatJsType(RoType type)
    {
        return type switch
        {
            { IsGenericType: true } when _appModel.WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var t) && t == _appModel.WellKnownTypes.IResourceWithConnectionStringType => "IResourceWithConnectionStringBuilder",
            { IsGenericType: true } when _appModel.WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var t) && t.IsInterface => "ResourceBuilder",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType => $"{type.GetGenericArguments()[0].Name}Builder",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Nullable<>)) => FormatJsType(type.GetGenericArguments()[0]) + " | null",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(List<>)) => "Array",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Dictionary<,>)) => "Map",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(IList<>)) => "Array",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(ICollection<>)) => "Array",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(IReadOnlyList<>)) => "Array",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(IReadOnlyCollection<>)) => "Array",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Action<>)) => $"({String.Join(" ,", type.GetGenericArguments().Select((x, i) => $"p{i}: {FormatJsType(x)}"))}) => void",
            { } when type.IsArray => $"Array<{FormatJsType(type.GetElementType() ?? _appModel.WellKnownTypes.GetKnownType(typeof(object)))}>",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Char)) => "string",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(String)) => "string",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Version)) => "string",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Uri)) => "string",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(SByte)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Byte)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Int16)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(UInt16)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Int32)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(UInt32)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Int64)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(UInt64)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(IntPtr)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(UIntPtr)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Double)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Single)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Decimal)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Boolean)) => "boolean",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Guid)) => "string",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Object)) => "any",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(DateTime)) => "Date",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(TimeSpan)) => "number",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(DateTimeOffset)) => "Date",
            { } when _appModel.ModelTypes.Contains(type) => SanitizeClassName(type.Name),
            { } when type.IsEnum => "any", // Enums should be handled by _modelTypes
            _ => "any"
        };
    }

    public void GenerateAppHost(string appPath)
    {
        Directory.CreateDirectory(appPath);

        // Create .modules directory and add embedded TypeScript files
        var modulesPath = Path.Combine(appPath, ModulePath);
        Directory.CreateDirectory(modulesPath);

        // Extract and write embedded TypeScript client files
        var assembly = Assembly.GetExecutingAssembly();

        // Extract types.ts
        var typesPath = Path.Combine(modulesPath, "types.ts");
        using var stream1 = assembly.GetManifestResourceStream("Aspire.Cli.Rosetta.Shared.typescript.types.ts")
            ?? throw new InvalidOperationException("Embedded resource 'types.ts' not found.");
        using var reader1 = new StreamReader(stream1);
        File.WriteAllText(typesPath, reader1.ReadToEnd());

        // Extract RemoteAppHostClient.ts
        var remoteAppHostClientPath = Path.Combine(modulesPath, "RemoteAppHostClient.ts");
        using var stream2 = assembly.GetManifestResourceStream("Aspire.Cli.Rosetta.Shared.typescript.RemoteAppHostClient.ts")
            ?? throw new InvalidOperationException("Embedded resource 'RemoteAppHostClient.ts' not found.");
        using var reader2 = new StreamReader(stream2);
        File.WriteAllText(remoteAppHostClientPath, reader2.ReadToEnd());

        var content = """
            import { createBuilder } from "./.modules/distributed-application.js";

            const builder = createBuilder();

            // Add your resources here
            // builder.addYourResourcesHere();

            await builder.build().run();
            """;

        var appHostPath = Path.Combine(appPath, "apphost.ts");

        if (!File.Exists(appHostPath))
        {
            File.WriteAllText(appHostPath, content);
        }

        var tsconfigPath = Path.Combine(appPath, "tsconfig.json");

        // language=json
        var tsconfig = """
            {
              "compilerOptions": {
                "target": "es2022",
                "module": "es2022",
                "moduleResolution": "node",
                "outDir": "./dist",
                "rootDir": "./",
                "strict": true
              },
              "include": [
                "**/*.ts"
              ],
              "exclude": [
                "node_modules"
              ]
            }
            """;

        File.WriteAllText(tsconfigPath, tsconfig);

        // Create or Update package.json

        var packageJsonPath = Path.Combine(appPath, "package.json");

        if (!File.Exists(packageJsonPath))
        {
            // Extract package.json
            using var stream3 = assembly.GetManifestResourceStream("Aspire.Cli.Rosetta.Shared.typescript.package.json")
                ?? throw new InvalidOperationException("Embedded resource 'package.json' not found.");
            using var reader3 = new StreamReader(stream3);
            File.WriteAllText(packageJsonPath, reader3.ReadToEnd());
        }
        else
        {
            try
            {
                // TODO: Patch existing package.json file
            }
            catch (JsonException)
            {
                Console.WriteLine($"Failed to parse {packageJsonPath}");
            }
        }
    }

    public string ExecuteAppHost(string appPath)
    {
        // Toolchain requires Node.js and TypeScript
        // npm install -g typescript

        interactionService.ShowStatus(
            $":package:  Installing npm packages...",
            () =>
            {
                var startInfo = new ProcessStartInfo("npm");
                startInfo.WorkingDirectory = appPath;
                startInfo.ArgumentList.Add("install");
                startInfo.WindowStyle = ProcessWindowStyle.Minimized;
                startInfo.UseShellExecute = true;
                startInfo.CreateNoWindow = true;

                using var npmProcess = Process.Start(startInfo);
                npmProcess!.WaitForExit();
            });

        // tsc --project tsconfig.json

        interactionService.ShowStatus(
            $":floppy_disk:  Building typescript...",
            () =>
            {
                var startInfo = new ProcessStartInfo("tsc");
                startInfo.WorkingDirectory = appPath;
                startInfo.WindowStyle = ProcessWindowStyle.Minimized;
                startInfo.UseShellExecute = true;
                startInfo.CreateNoWindow = true;

                using var tscProcess = Process.Start(startInfo);
                tscProcess!.WaitForExit();
            });

        var resultFileName = Path.GetTempFileName();

        var startInfo = new ProcessStartInfo("node");
        startInfo.ArgumentList.Add("dist/apphost.js");
        startInfo.WorkingDirectory = appPath;
        // startInfo.WindowStyle = ProcessWindowStyle.Minimized;
        startInfo.UseShellExecute = false;
        //startInfo.CreateNoWindow = true;

        using var nodeProcess = Process.Start(startInfo)!;

        nodeProcess!.WaitForExit();

        return resultFileName;
    }

    public IEnumerable<KeyValuePair<string, string>> GenerateHostFiles()
    {
        yield return new("Helpers.cs", """
            using System.Diagnostics;
            using System.Runtime.InteropServices;

            public static class Helpers
            {
                public static void PrintException(Exception ex)
                {
                    var st = new StackTrace(ex, true);
                    var frames = st.GetFrames();
    
                    Console.Error.WriteLine($"{ex.Message}");
    
                    if (frames != null && frames.Length > 0)
                    {
                        // Bottom frame (entrypoint)
                        var bottom = frames[frames.Length - 1];
                        var bottomMethod = bottom.GetMethod()?.Name ?? "unknown";
                        var bottomFile = !string.IsNullOrEmpty(bottom.GetFileName()) ? System.IO.Path.GetFileName(bottom.GetFileName()) : "unknown.js";
                        var bottomLine = bottom.GetFileLineNumber() - 1;
                        var bottomCol = bottom.GetFileColumnNumber();
    
                        if (bottomLine > 0 && bottomCol > 0)
                            Console.Error.WriteLine($"    at {bottomMethod} ({bottomFile}:{bottomLine}:{bottomCol})");
                        else if (bottomLine > 0)
                            Console.Error.WriteLine($"    at {bottomMethod} ({bottomFile}:{bottomLine})");
                        else
                            Console.Error.WriteLine($"    at {bottomMethod} ({bottomFile})");
                    }
                }
            }
            """);
    }
}
