// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting;

namespace RemoteAppHost;

public class InstructionProcessor
{
    private readonly Dictionary<string, object> _variables = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<object?> ExecuteInstructionAsync(string instructionJson)
    {
        var jsonDocument = JsonDocument.Parse(instructionJson);
        var instructionName = jsonDocument.RootElement.GetProperty("name").GetString();

        return instructionName switch
        {
            "CREATE_BUILDER" => await ExecuteCreateBuilderAsync(instructionJson),
            "RUN_BUILDER" => await ExecuteRunBuilderAsync(instructionJson),
            "pragma" => ExecutePragma(instructionJson),
            "INVOKE" => ExecuteInvoke(instructionJson),
            _ => throw new NotSupportedException($"Instruction '{instructionName}' is not supported")
        };
    }

    private Task<object> ExecuteCreateBuilderAsync(string instructionJson)
    {
        var instruction = JsonSerializer.Deserialize<CreateBuilderInstruction>(instructionJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize CREATE_BUILDER instruction");

        var options = new DistributedApplicationOptions
        {
            Args = instruction.Args ?? []
        };

        // Create the distributed application builder
        var builder = DistributedApplication.CreateBuilder(options);

        // Store the builder in the variables dictionary
        _variables[instruction.BuilderName] = builder;

        return Task.FromResult<object>(new { success = true, builderName = instruction.BuilderName });
    }

    private async Task<object> ExecuteRunBuilderAsync(string instructionJson)
    {
        var instruction = JsonSerializer.Deserialize<RunBuilderInstruction>(instructionJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize RUN_BUILDER instruction");

        if (!_variables.TryGetValue(instruction.BuilderName, out var builderObj) ||
            builderObj is not IDistributedApplicationBuilder builder)
        {
            throw new InvalidOperationException($"Builder '{instruction.BuilderName}' not found or is not a valid builder");
        }

        // Build and start the application
        var app = builder.Build();

        // Store the app so we can access it later for shutdown
        _variables[$"{instruction.BuilderName}_app"] = app;

        try
        {
            // Start the application and wait for startup to complete
            // This will throw if startup fails (e.g., port conflict)
            await app.StartAsync();

            // The app is now running in the background.
            // When the TypeScript client disconnects, the server will shut down
            // and the app will be disposed.

            return new { success = true, builderName = instruction.BuilderName, status = "running" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application startup failed: {ex.Message}");
            throw; // Re-throw to propagate error back to client
        }
    }

    private object ExecutePragma(string instructionJson)
    {
        var instruction = JsonSerializer.Deserialize<PragmaInstruction>(instructionJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize pragma instruction");

        // For now, just acknowledge the pragma instruction
        Console.WriteLine($"Pragma: {instruction.Type} = {instruction.Value}");

        return new { success = true, type = instruction.Type, value = instruction.Value };
    }

    private object ExecuteInvoke(string instructionJson)
    {
        var instruction = JsonSerializer.Deserialize<InvokeInstruction>(instructionJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize INVOKE instruction");

        // Get the source object from variables
        if (!_variables.TryGetValue(instruction.Source, out var sourceObj))
        {
            throw new InvalidOperationException($"Source variable '{instruction.Source}' not found");
        }

        // Load the assembly
        var assembly = System.Reflection.Assembly.Load(instruction.MethodAssembly);

        // Get the type
        var type = assembly.GetType(instruction.MethodType)
            ?? throw new InvalidOperationException($"Type '{instruction.MethodType}' not found in assembly '{instruction.MethodAssembly}'");

        // Find the method by metadata token or by name
        System.Reflection.MethodInfo? method = null;

        if (instruction.MetadataToken != 0)
        {
            method = type.GetMethods().FirstOrDefault(m => m.MetadataToken == instruction.MetadataToken);
        }

        // Fall back to finding by name if metadata token is 0 or not found
        if (method == null)
        {
            var sourceType = sourceObj.GetType();

            // Find all methods with the matching name
            var candidateMethods = type.GetMethods()
                .Where(m => m.Name == instruction.MethodName)
                .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                .ToList();

            // Try to find the best matching method based on the first parameter type (extension method target)
            foreach (var candidate in candidateMethods)
            {
                var parameters = candidate.GetParameters();
                if (parameters.Length == 0)
                {
                    continue;
                }

                var firstParamType = parameters[0].ParameterType;

                // Handle generic parameters
                if (firstParamType.IsGenericType)
                {
                    var genericTypeDef = firstParamType.GetGenericTypeDefinition();
                    // Check if source type implements the generic interface
                    var sourceInterfaces = sourceType.GetInterfaces();
                    foreach (var iface in sourceInterfaces)
                    {
                        if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericTypeDef)
                        {
                            method = candidate;
                            break;
                        }
                    }
                }
                else if (firstParamType.IsAssignableFrom(sourceType))
                {
                    method = candidate;
                }

                if (method != null)
                {
                    break;
                }
            }

            if (method == null && candidateMethods.Count > 0)
            {
                // Just use the first candidate if we couldn't find a perfect match
                method = candidateMethods[0];
            }
        }

        if (method == null)
        {
            throw new InvalidOperationException($"Method '{instruction.MethodName}' not found on type '{instruction.MethodType}'");
        }

        // Check if this is an extension method
        var isExtensionMethod = method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);

        // Prepare arguments in the correct order
        var methodParameters = method.GetParameters();
        var arguments = new object?[methodParameters.Length];

        var startIndex = isExtensionMethod ? 1 : 0;

        if (isExtensionMethod)
        {
            // For extension methods, the source object becomes the first argument (this parameter)
            arguments[0] = sourceObj;
        }

        // Fill remaining arguments from instruction.Args (skip the first 'this' parameter)
        for (int i = startIndex; i < methodParameters.Length; i++)
        {
            var paramName = methodParameters[i].Name;
            if (paramName != null && instruction.Args.TryGetValue(paramName, out var argValue))
            {
                // Convert JsonElement to the appropriate type if needed
                if (argValue is JsonElement jsonElement)
                {
                    try
                    {
                        arguments[i] = JsonSerializer.Deserialize(jsonElement.GetRawText(), methodParameters[i].ParameterType, _jsonOptions);
                    }
                    catch (Exception ex)
                    {
                        if (jsonElement.ValueKind == JsonValueKind.String
                            && _variables.TryGetValue(jsonElement.GetString()!, out var varValue)
                            && varValue != null
                            )
                        {
                            // Check the type compatibility. This may be an error if the wrong extension method was picked by the code generation.
                            if (!methodParameters[i].ParameterType.IsAssignableFrom(varValue.GetType()))
                            {
                                throw new InvalidOperationException($"Failed to convert argument '{paramName}' to type '{methodParameters[i].ParameterType}': {ex.Message}");
                            }

                            arguments[i] = varValue;
                        }
                    }
                }
                else
                {
                    arguments[i] = argValue;
                }
            }
            else
            {
                throw new InvalidOperationException($"Argument '{paramName}' not found in instruction args for extension method parameter at index {i}");
            }
        }

        // Make generic methods based on the knowledge of the actual argument instance types
        // The issue comes from the fact that the reflected method may have generic parameters like
        // IResourceBuilder<T> WaitFor<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> dependency)
        // but at runtime we need the actual type of T to invoke the method. We do this by looking at the actual argument types and extract their generic arguments.

        if (method.ContainsGenericParameters)
        {
            // Find which arguments correspond to generic parameters
            var genericArguments = method.GetGenericArguments();
            for (var i = 0; i < genericArguments.Length; i++)
            {
                var genericArgument = genericArguments[i];
                for (var j = 0; j < methodParameters.Length; j++)
                {
                    var p = methodParameters[j];
                    var argument = arguments[j];

                    for (var k = 0; k < p.ParameterType.GenericTypeArguments.Length; k++)
                    {
                        var ga = p.ParameterType.GenericTypeArguments[k];
                        if (ga.UnderlyingSystemType == genericArgument.UnderlyingSystemType)
                        {
                            genericArguments[i] = argument?.GetType().GetGenericArguments()[k] ?? typeof(object);
                        }
                    }
                }
            }

            method = method.MakeGenericMethod(genericArguments);
        }

        // Invoke the method
        var result = isExtensionMethod ? method.Invoke(null, arguments) : method.Invoke(sourceObj, arguments);

        // Store the result in the target variable
        if (result != null)
        {
            _variables[instruction.Target] = result;
        }

        return new {
            success = true,
            source = instruction.Source,
            target = instruction.Target,
            methodName = instruction.MethodName,
            result = result?.ToString() ?? "null"
        };
    }
}
