// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Aspire.Cli.Rosetta.Models;
using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta.Generators;

internal sealed class PythonCodeGenerator(ApplicationModel appModel) : ICodeGenerator
{
    private const string ModulePath = "./modules";
    private const string TripleQuotes = "\"\"\"";

    // Custom record-like classes that contain the overload parameters
    private static readonly Dictionary<MemberInfo, string> s_overloadParameterClassByMethod = new();

    // Code snippets that will be injected at the end of the file
    private static readonly List<string> s_globalSnippets = [];
    private readonly ApplicationModel _appModel = appModel;

    private static int s_counter;

    public IReadOnlyList<string> GenerateDistributedApplication()
    {
        var modulesPath = Path.Combine(_appModel.AppPath, ModulePath);
        Directory.CreateDirectory(modulesPath);

        var filename = Path.Combine(modulesPath, "distributed_application.py");
        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream);

        GenerateDistributedApplication(writer);

        return new List<string> { filename };
    }

    // For testing purposes
    internal void GenerateDistributedApplication(TextWriter writer)
    {
        string content = $$"""
        from .remote_app_host_client import RemoteAppHostClient, make_instruction

        from enum import Enum
        from typing import Optional
        import hashlib
        import os
        import subprocess
        import signal
        import sys
        import time
        from datetime import timedelta # Required by integrations

        # Global source variable to capture output code
        source = ""
        
        # Global variable to store the aspire process
        aspire_process = None

        # Global variable to store the client
        client = None

        def write_line(code):
            global source
            source += code + '\n'

        def send_instruction(instruction):
            result = client.execute_instruction(instruction)
            # print(f"   {instruction['name']} result:", result)

        tmp_source = ""

        def begin_capture():
            global source
            global tmp_source
            tmp_source = source
            source = ""
            
        def end_capture():
            result = source
            source = tmp_source
            return result
            
        def convert_nullable(value):
            # Convert nullable values to appropriate C# string representation
            # Handle None
            if value is None:
                return "default"
            # Handle strings vs other types and quote them
            if isinstance(value, str):
                return f'"{value}"'
            elif isinstance(value, list):
                return convert_array(value)
            else:
                return f"{value}"
                
        def convert_array(array):
            # Convert array/ list values to appropriate C# string representation
            # Handle empty list and None
            if array is None:
                return "default"
            if len(array) == 0:
                return "[]"
            # Handle strings vs other types and quote them
            values = []
            for item in array:
                if isinstance(item, str):
                    values.append(f'"{item}"')
                elif isinstance(item, list):
                    values.append(convert_array(item))
                elif item is None:
                    values.append("null")
                else:
                    values.append(str(item))
            
            return f"[{', '.join(values)}]"
        
        class DistributedApplication:
            def __init__(self, builder_name):
                self.builder_name = builder_name

            def run(self):
                run = make_instruction("RUN_BUILDER", builderName = self.builder_name)
                send_instruction(run)

                print("Application is running. Press Ctrl+C to stop...")

                def signal_handler(sig, frame):
                    global aspire_process, client

                    # Disconnect client if it exists
                    if client:
                        try:
                            client.disconnect()
                            print("👋 Disconnected from Generic App Host")
                        except Exception as e:
                            print(f"Error disconnecting client: {e}")
                    # Terminate aspire process if it exists
                    if aspire_process:
                        print("Terminating aspire process...")
                        aspire_process.terminate()
                        try:
                            # Wait for process to terminate gracefully
                            aspire_process.wait(timeout=5)
                        except subprocess.TimeoutExpired:
                            print("Forcing aspire process to stop...")
                            aspire_process.kill()
                    sys.exit(0)

                # Register signal handler for CTRL+C
                signal.signal(signal.SIGINT, signal_handler)

                # Block the process until CTRL+C is sent
                try:
                    while True:
                        time.sleep(1)
                except KeyboardInterrupt:
                    # This will be caught by the signal handler
                    pass

        class DistributedApplicationBuilderBase:
            _index = 1
            
            def __init__(self, args=None):
                if args is None:
                    args = []
                self.args = args
                self._name = f"appBuilder{DistributedApplicationBuilderBase._index}"
                DistributedApplicationBuilderBase._index += 1
            
            def build(self):
                return DistributedApplication(self._name)
        
        class ReferenceClass:
            _index = 1
            
            def __init__(self, builder, cs_type, prefix):
                self.builder = builder
                self.cs_type = cs_type
                self._name = f"{prefix}{ReferenceClass._index}"
                ReferenceClass._index += 1
                write_line(f"{cs_type} {self._name};")
        
        def create_builder(args=None):
            if args is None:
                args = [] 

            print("🚀 Starting Generic App Host...")
            global rosetta_process

            pipeName = hashlib.md5(os.getcwd().encode()).hexdigest()
            env = os.environ.copy()
            env["REMOTE_APP_HOST_PIPE_NAME"] = pipeName
            env["REMOTE_APP_HOST_PID"] = str(os.getpid())

            # This process inherits the current console directly
            rosetta_process = subprocess.Popen(
                ["aspire", "polyglot", "serve", "-o", os.getcwd()],
                env=env
            )

            print("🔌 Connecting...")

            global client
            client = RemoteAppHostClient()
            client.pipe_name = pipeName

            # Loop until connection is successful
            while True:
                try:
                    client.connect()
                    client.ping()
                    print("✅ Connected to Generic App Host")
                    break  # Connection successful, exit loop
                except Exception as e:
                    time.sleep(1)

            global distributedApplicationBuilder
            distributedApplicationBuilder = DistributedApplicationBuilder(args)

            create = make_instruction("CREATE_BUILDER", builderName=distributedApplicationBuilder._name, projectDirectory=os.getcwd(), args=[])
            send_instruction(create)

            return distributedApplicationBuilder
        """;

        writer.WriteLine(content);

        GenerateModelClasses(writer, _appModel.IntegrationModels.Values);

        GenerateParameterClasses(writer, s_overloadParameterClassByMethod.Values);

        // Generate Resource classes headers as there are cross-references
        GenerateResourceClasses(writer, _appModel.IntegrationModels.Values, false);

        // Generate DistributedApplicationBuilder class
        writer.WriteLine("class DistributedApplicationBuilder(DistributedApplicationBuilderBase):");
        foreach (var integration in _appModel.IntegrationModels.Values)
        {
            GenerateDistributedApplicationBuilderMethod(writer, integration);
        }
        writer.WriteLine();

        // Generate IResourceWithConnectionString
        writer.WriteLine("class IResourceWithConnectionStringBuilder(ReferenceClass):");
        writer.WriteLine("    def __init__(self, builder):");
        writer.WriteLine("        super().__init__(builder, \"IResourceBuilder<IResourceWithConnectionString>\", \"resourceWithConnectionStringBuilder\")");
        writer.WriteLine();

        // Generate ResourceBuilder
        writer.WriteLine("class ResourceBuilder(ReferenceClass):");
        writer.WriteLine("    def __init__(self, builder, cs_type):");
        writer.WriteLine("        super().__init__(builder, cs_type, \"resourceBuilder\")");
        writer.WriteLine();

        foreach (var integration in _appModel.IntegrationModels.Values)
        {
            GenerateResourceBuilderMethod(writer, integration);
        }
        writer.WriteLine();

        GenerateResourceClasses(writer, _appModel.IntegrationModels.Values, true);

        GenerateGlobalSnippet(writer);
    }

    private static void GenerateGlobalSnippet(TextWriter writer)
    {
        foreach (var snippet in s_globalSnippets)
        {
            writer.WriteLine(snippet);
        }
    }

    private static void GenerateParameterClasses(TextWriter writer, IEnumerable<string> values)
    {
        foreach (var overloadParameterType in values)
        {
            writer.WriteLine(overloadParameterType);
        }
    }

    private void GenerateResourceBuilderMethod(TextWriter writer, IntegrationModel model)
    {
        var overloads = model.SharedExtensionMethods.Where(x => _appModel.WellKnownTypes.TryGetResourceBuilderTypeArgument(x.ReturnType, out var t) && t == _appModel.WellKnownTypes.ResourceType).ToArray();

        foreach (var mg in overloads.GroupBy(m => m.Name))
        {
            var indexes = new Dictionary<string, int>();

            foreach (var overload in mg.OrderBy(m => m.Parameters.Count))
            {
                var methodNameAttribute = overload.GetCustomAttributes()
                    .FirstOrDefault(attr => attr.AttributeType.FullName == "Aspire.Hosting.Polyglot.PolyglotMethodNameAttribute");

                var preferredMethodName = methodNameAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "MethodName").Value?.ToString()
                                    ?? methodNameAttribute?.FixedArguments.ElementAtOrDefault(0)?.ToString()
                                    ?? overload.Name;

                var methodName = _appModel.TryGetMapping(overload.Name, overload.Parameters.Skip(1).Select(p => p.ParameterType).ToArray(), out var mapping)
                    ? ToSnakeCase(mapping.GeneratedName)
                    : ToSnakeCase(preferredMethodName);

                if (indexes.TryGetValue(methodName, out var index))
                {
                    indexes[methodName] = index + 1;
                    methodName = $"{methodName}{index.ToString(CultureInfo.InvariantCulture) + 1}";
                }
                else
                {
                    indexes[methodName] = 0;
                }

                var parameters = overload.Parameters.Skip(1);

                // Push optional and nullable parameters to the end of the list
                var orderedParameters = parameters.OrderBy(p => p.IsOptional ? 1 : 0).ThenBy(p => _appModel.WellKnownTypes.IsNullableOfT(p.ParameterType) ? 1 : 0);

                var parameterList = String.Join(", ", orderedParameters.Select(FormatArgument)); // name: string, port?: number | null
                var csParameterList = String.Join(", ", parameters.Select(FormatCsArgument));
                var jsonParameterList = string.Join(", ", parameters.Select(FormatJsonArgument)); // name: name, port: port

                writer.WriteLine($$"""

                    def {{methodName}}(self, {{parameterList}}):
                        {{TripleQuotes}}
                        C# Definition: {{FormatMethodSignature(overload)}}
                        
                        {{String.Join("\n        ", parameters.Select(p => $":param {FormatPyType(p.ParameterType)} {p.Name}: C# Type: {PrettyPrintCSharpType(p.ParameterType)}"))}}
                        :return: C# Type: {{PrettyPrintCSharpType(overload.ReturnType)}}
                        :rtype: ResourceBuilder
                        {{TripleQuotes}}

                        result = ResourceBuilder(self.builder, self.cs_type)
                        write_line(f'{result._name} = {self._name}.{{overload.Name}}({{csParameterList}});')
                        invoke = make_instruction("INVOKE",
                            source = self._name, 
                            target = result._name, 
                            methodAssembly = "{{overload.DeclaringType?.DeclaringAssembly.Name}}", 
                            methodType = "{{overload.DeclaringType?.FullName}}", 
                            methodName = "{{overload.Name}}", 
                            methodArgumentTypes = [{{string.Join(", ", overload.Parameters.Select(p => "'" + p.ParameterType.FullName + "'"))}}], 
                            metadataToken = {{overload.MetadataToken}},
                            args = {{{jsonParameterList}}} 
                        )

                        send_instruction(invoke)

                        return result
                """);
            }
        }
    }

    private void GenerateDistributedApplicationBuilderMethod(TextWriter writer, IntegrationModel model)
    {
        var builderMethods = model.IDistributedApplicationBuilderExtensionMethods;
        foreach (var methodGroups in builderMethods.GroupBy(m => m.Name))
        {
            var index = 0;
            var overloads = methodGroups.OrderBy(m => m.Parameters.Count).ToArray();

            foreach (var overload in overloads)
            {
                if (overload.ReturnType.ContainsGenericParameters)
                {
                    continue;
                }

                var returnType = overload.ReturnType;
                var pyReturnTypeName = returnType.Name;

                if (returnType.IsGenericType && returnType.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType)
                {
                    returnType = returnType.GetGenericArguments()[0];
                    pyReturnTypeName = returnType.Name + "Builder";
                }

                var methodName = _appModel.TryGetMapping(overload.Name, overload.Parameters.Skip(1).Select(p => p.ParameterType).ToArray(), out var mapping)
                    ? ToSnakeCase(mapping.GeneratedName)
                    : ToSnakeCase(overload.Name) + (index > 0 ? index.ToString(CultureInfo.InvariantCulture) : string.Empty);

                var parameters = overload.Parameters.Skip(1);

                // Push optional and nullable parameters to the end of the list
                var orderedParameters = parameters.OrderBy(p => p.IsOptional ? 1 : 0).ThenBy(p => _appModel.WellKnownTypes.IsNullableOfT(p.ParameterType) ? 1 : 0);

                var parameterList = String.Join(", ", orderedParameters.Select(FormatArgument)); // name: string, port?: number | null
                var csParameterList = String.Join(", ", parameters.Select(FormatCsArgument));
                var jsonParameterList = string.Join(", ", parameters.Select(FormatJsonArgument)); // name: name, port: port

                writer.WriteLine($$"""

                    def {{methodName}}(self, {{parameterList}}):
                        {{TripleQuotes}}
                        C# Definition: {{FormatMethodSignature(overload)}}

                        {{String.Join("\n        ", parameters.Select(p => $":param {FormatPyType(p.ParameterType)} {p.Name}: C# Type: {PrettyPrintCSharpType(p.ParameterType)}"))}}
                        :return: C# Type: {{PrettyPrintCSharpType(overload.ReturnType)}}
                        :rtype: {{pyReturnTypeName}}
                        {{TripleQuotes}}

                        result = {{pyReturnTypeName}}(self)
                        write_line(f'{result._name} = {self._name}.{{overload.Name}}({{csParameterList}});')

                        invoke = make_instruction("INVOKE",
                            source = self._name, 
                            target = result._name, 
                            methodAssembly = "{{overload.DeclaringType?.DeclaringAssembly.Name}}", 
                            methodType = "{{overload.DeclaringType?.FullName}}", 
                            methodName = "{{overload.Name}}", 
                            methodArgumentTypes = [{{string.Join(", ", overload.Parameters.Select(p => "'" + p.ParameterType.FullName + "'"))}}], 
                            metadataToken = {{overload.MetadataToken}},
                            args = {{{jsonParameterList}}} 
                        )

                        send_instruction(invoke)

                        return result
                """);

                index++;
            }
        }
    }

    private void GenerateModelClasses(TextWriter textWriter, IEnumerable<IntegrationModel> _)
    {
        foreach (var type in _appModel.ModelTypes)
        {
            if (type.IsEnum)
            {
                textWriter.WriteLine();
                textWriter.WriteLine($"class {type.Name}(Enum):");
                foreach (var n in type.GetEnumNames())
                {
                    if (n == "None")
                    {
                        textWriter.WriteLine($"    # Can't assign None in Python");
                        textWriter.WriteLine($"    # {n} = 'None'");
                    }
                    else
                    {
                        textWriter.WriteLine($"    {n} = '{n}'");
                    }
                }
                continue;
            }
            else
            {
                textWriter.WriteLine();
                textWriter.WriteLine($$"""
                class {{SanitizeClassName(type.Name)}}(ReferenceClass):
                    def __init__(self, builder):
                        super().__init__(builder, "{{type.Name}}", "{{ToSnakeCase(type.Name)}}")
                """);
            }
        }
    }

    /// <summary>
    /// Sanitize a class name by replacing '+' with '_' to handle nested classes.
    /// </summary>
    private static string SanitizeClassName(string name) => name.Replace("+", "_");

    private void GenerateResourceClasses(TextWriter textWriter, IEnumerable<IntegrationModel> allIntegrationModels, bool includeMethods)
    {
        foreach (var im in allIntegrationModels)
        {
            foreach (var resourceModel in im.Resources.Values)
            {
                EmitResourceClass(textWriter, resourceModel);
            }
        }

        void EmitResourceClass(TextWriter textWriter, ResourceModel model)
        {
            var resourceName = SanitizeClassName(model.ResourceType.Name);
            textWriter.WriteLine();
            if (includeMethods)
            {
                textWriter.WriteLine($$"""
                class {{resourceName}}Builder(ReferenceClass):
                    def __init__(self, builder, cs_type="IResourceBuilder<{{model.ResourceType.FullName}}>"):
                        super().__init__(builder, cs_type, "{{ToSnakeCase(resourceName)}}Builder")
                """);

                GenerateResourceExtensionMethods(textWriter, model.ResourceType, model.IResourceTypeBuilderExtensionsMethods);
            }
            else
            {
                textWriter.WriteLine($$"""
                class {{resourceName}}Builder(ReferenceClass):
                    pass
                """);
            }

            textWriter.WriteLine();
        }
    }

    private void GenerateResourceExtensionMethods(TextWriter writer, RoType type, IEnumerable<RoMethod> extensionMethods)
    {
        foreach (var methodGroups in extensionMethods.GroupBy(m => m.Name))
        {
            var index = 0;
            var overloads = methodGroups.OrderBy(m => m.Parameters.Count).ToArray();

            foreach (var overload in overloads)
            {
                var returnType = overload.ReturnType;
                var pyReturnTypeName = returnType.Name;

                if (returnType.IsGenericType && returnType.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType)
                {
                    returnType = returnType.ContainsGenericParameters ? type : returnType.GetGenericArguments()[0];
                    pyReturnTypeName = returnType.Name + "Builder";
                }

                var methodName = _appModel.TryGetMapping(overload.Name, overload.Parameters.Skip(1).Select(p => p.ParameterType).ToArray(), out var mapping)
                    ? ToSnakeCase(mapping.GeneratedName)
                    : ToSnakeCase(overload.Name) + (index > 0 ? index.ToString(CultureInfo.InvariantCulture) : string.Empty);

                var parameters = overload.Parameters.Skip(1);
                // Push optional and nullable parameters to the end of the list

                var orderedParameters = parameters.OrderBy(p => p.IsOptional ? 1 : 0).ThenBy(p => _appModel.WellKnownTypes.IsNullableOfT(p.ParameterType) ? 1 : 0);

                var parameterList = String.Join(", ", orderedParameters.Select(FormatArgument)); // name: string, port?: number | null
                var csParameterList = String.Join(", ", parameters.Select(FormatCsArgument));
                var jsonParameterList = string.Join(", ", parameters.Select(FormatJsonArgument)); // name: name, port: port

                writer.WriteLine($$"""

                    def {{methodName}}(self, {{parameterList}}):
                        {{TripleQuotes}}
                        C# Definition: {{FormatMethodSignature(overload)}}

                        {{String.Join("\n        ", parameters.Select(p => $":param {FormatPyType(p.ParameterType)} {p.Name}: C# Type: {PrettyPrintCSharpType(p.ParameterType)}"))}}
                        :return: C# Type: {{PrettyPrintCSharpType(overload.ReturnType)}}
                        :rtype: {{pyReturnTypeName}}
                        {{TripleQuotes}}

                        result = {{pyReturnTypeName}}(self.builder)
                        write_line(f'{result._name} = {self._name}.{{overload.Name}}({{csParameterList}});')

                        invoke = make_instruction("INVOKE",
                            source = self._name, 
                            target = result._name, 
                            methodAssembly = "{{overload.DeclaringType?.DeclaringAssembly.Name}}", 
                            methodType = "{{overload.DeclaringType?.FullName}}", 
                            methodName = "{{overload.Name}}", 
                            methodArgumentTypes = [{{string.Join(", ", overload.Parameters.Select(p => "'" + p.ParameterType.FullName + "'"))}}], 
                            metadataToken = {{overload.MetadataToken}},
                            args = {{{jsonParameterList}}} 
                        )

                        send_instruction(invoke)

                        return result
                """);

                index++;
            }
        }
    }

    private static string ToSnakeCase(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return methodName;
        }

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(methodName[0]));

        for (int i = 1; i < methodName.Length; i++)
        {
            if (char.IsUpper(methodName[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(methodName[i]));
            }
            else
            {
                result.Append(methodName[i]);
            }
        }

        return result.ToString();
    }

    public void GenerateAppHost(string appPath)
    {
        Directory.CreateDirectory(appPath);

        var content = """
        from modules.distributed_application import create_builder

        builder = create_builder()

        builder.build().run()
        
        """;

        var appHostPath = Path.Combine(appPath, "apphost.py");

        if (!File.Exists(appHostPath))
        {
            File.WriteAllText(appHostPath, content);
        }

        var assembly = Assembly.GetExecutingAssembly();

        // Extract remote_app_host_client.py
        var remoteAppHostClientPath = Path.Combine(appPath, "modules", "remote_app_host_client.py");
        using var stream = assembly.GetManifestResourceStream("Aspire.Cli.Rosetta.Shared.python.remote_app_host_client.py")
            ?? throw new InvalidOperationException("Embedded resource 'remote_app_host_client.py' not found."); ;
        using var reader = new StreamReader(stream);
        File.WriteAllText(remoteAppHostClientPath, reader.ReadToEnd());
    }

    public string ExecuteAppHost(string appPath)
    {
        var resultFileName = Path.GetTempFileName();

        var startInfo = new ProcessStartInfo(OperatingSystem.IsWindows() ? "python.exe" : "python");
        startInfo.ArgumentList.Add("apphost.py");
        startInfo.WorkingDirectory = appPath;
        startInfo.WindowStyle = ProcessWindowStyle.Minimized;
        startInfo.UseShellExecute = false;
        //startInfo.RedirectStandardOutput = true;

        using var pythonProcess = Process.Start(startInfo)!;

        pythonProcess!.WaitForExit();

        return resultFileName;
    }

    private string FormatArgument(RoParameterInfo p)
    {
        var result = p.Name!;

        if (p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Nullable<>)))
        {
            result += $": Optional[{FormatPyType(p.ParameterType.GetGenericArguments()[0])}] = None";
        }
        else
        {
            if (p.IsOptional)
            {
                result += $": Optional[{FormatPyType(p.ParameterType)}] = None";
            }
            else
            {
                result += $": {FormatPyType(p.ParameterType)}";
            }
        }

        return result;
    }

    private string FormatJsonArgument(RoParameterInfo p)
    {
        var result = $"\"{p.Name!}\": {p.Name!}";

        if (p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType)
        {
            result += "._name";
        }

        return result;
    }

    private string FormatCsArgument(RoParameterInfo p) => FormatCsArgument(p, "");

    private string FormatCsArgument(RoParameterInfo p, string prefix)
    {
        string result;

        // Is the parameter of Type Action<IResourceBuilder<T>>? (callback)
        if (p.ParameterType.IsGenericType &&
            p.ParameterType.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Action<>)) &&
            p.ParameterType.GetGenericArguments()[0] is { } genericArgument &&
            genericArgument.IsGenericType &&
            genericArgument.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType)
        {
            // This is a resource builder
            var resourceType = genericArgument.GetGenericArguments()[0];
            var resourceName = SanitizeClassName(resourceType.Name);

            var index = s_globalSnippets.Count;
            var csMethodName = $"Callback{s_counter++}";

            s_globalSnippets.Add($$"""
                def callback{{index}}(builder, callback):
                    # begin_capture()
                    write_line('')
                    write_line('void {{csMethodName}}(IResourceBuilder<{{resourceType.FullName}}> builder)')
                    write_line('{')
                    r = {{resourceName}}Builder(builder)
                    write_line(f'{r._name} = builder;')
                    callback(r)
                    write_line('}')
                    write_line('')
                    return "{{csMethodName}}"
                    # end_capture()
                """);

            return $"{{default if callback{index} is None else callback{index}(result.builder, {prefix}{p.Name})}}";
        }
        // Value resolution
        else if (p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType)
        {
            result = $"{prefix}{p.Name}._name";
        }
        else if (_appModel.ModelTypes.Contains(p.ParameterType) && !p.ParameterType.IsEnum)
        {
            result = $"{prefix}{p.Name}._name";
        }
        else
        {
            result = $"{prefix}{p.Name!}";
        }

        // Now determine if we need to convert the value
        if (p.ParameterType.IsArray)
        {
            result = $"{{convert_array({result})}}";
        }
        else if (p.IsOptional || p.ParameterType.IsGenericType && p.ParameterType.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Nullable<>)))
        {
            result = $"{{convert_nullable({result})}}";
        }
        else if (p.ParameterType == _appModel.WellKnownTypes.GetKnownType<string>())
        {
            result = $"\"{{{result}}}\"";
        }
        else if (p.ParameterType.IsEnum)
        {
            result = $"{p.ParameterType.Name}.{{{result}}}";
        }
        else
        {
            result = $"{{{result}}}";
        }

        return result;
    }

    private string FormatPyType(RoType type)
    {
        return type switch
        {
            { IsGenericType: true } when _appModel.WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var t) && t == _appModel.WellKnownTypes.IResourceWithConnectionStringType => "ResourceBuilder",
            { IsGenericType: true } when _appModel.WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var t) && t.IsInterface => "ResourceBuilder",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.IResourceBuilderType => $"{SanitizeClassName(type.GetGenericArguments()[0].Name)}Builder",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Nullable<>)) => $"Optional[{FormatPyType(type.GetGenericArguments()[0])}]",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(List<>)) => "list",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Dictionary<,>)) => "dict",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(IList<>)) => "list",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(ICollection<>)) => "list",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(IReadOnlyList<>)) => "list",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(IReadOnlyCollection<>)) => "list",
            { IsGenericType: true } when type.GenericTypeDefinition == _appModel.WellKnownTypes.GetKnownType(typeof(Action<>)) => "callable",
            { } when type.IsArray => "list",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Char)) => "str",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(String)) => "str",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Version)) => "str",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Uri)) => "str",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(SByte)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Byte)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Int16)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(UInt16)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Int32)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(UInt32)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Int64)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(UInt64)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(IntPtr)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(UIntPtr)) => "int",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Double)) => "float",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Single)) => "float",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Decimal)) => "float",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Boolean)) => "bool",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Guid)) => "str",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(Object)) => "object",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(DateTime)) => "datetime",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(TimeSpan)) => "timedelta",
            { } when type == _appModel.WellKnownTypes.GetKnownType(typeof(DateTimeOffset)) => "datetime",
            { } when _appModel.ModelTypes.Contains(type) => SanitizeClassName(type.Name),
            { } when type.IsEnum => type.Name,
            _ => "object"
        };
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

    public IEnumerable<KeyValuePair<string, string>> GenerateHostFiles()
    {
        return [];
    }
}
