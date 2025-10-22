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
            Args = instruction.Args ?? [],
            ProjectDirectory = instruction.ProjectDirectory
        };

        // Create the distributed application builder
        var builder = DistributedApplication.CreateBuilder(options);
        
        // Store the builder in the variables dictionary
        _variables[instruction.BuilderName] = builder;

        return Task.FromResult<object>(new { success = true, builderName = instruction.BuilderName });
    }

    private Task<object> ExecuteRunBuilderAsync(string instructionJson)
    {
        var instruction = JsonSerializer.Deserialize<RunBuilderInstruction>(instructionJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize RUN_BUILDER instruction");

        if (!_variables.TryGetValue(instruction.BuilderName, out var builderObj) || 
            builderObj is not IDistributedApplicationBuilder builder)
        {
            throw new InvalidOperationException($"Builder '{instruction.BuilderName}' not found or is not a valid builder");
        }

        // Build and run the application
        var app = builder.Build();
        
        // Start the application in the background
        _ = Task.Run(async () =>
        {
            try
            {
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application run failed: {ex.Message}");
            }
        });

        return Task.FromResult<object>(new { success = true, builderName = instruction.BuilderName, status = "running" });
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

        // Find the method using metadata token
        var method = type.GetMethods().FirstOrDefault(m => m.MetadataToken == instruction.MetadataToken)
            ?? throw new InvalidOperationException($"Method with metadata token '{instruction.MetadataToken}' not found on type '{instruction.MethodType}'");

        // Check if this is an extension method
        var isExtensionMethod = method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);

        // Check if the selected method matches the expected name and parameter count 
        if (method.Name != instruction.MethodName)
        {
            throw new InvalidOperationException($"Method name mismatch: expected '{instruction.MethodName}', found '{method.Name}'");
        }

        if ((method.GetParameters().Length - (isExtensionMethod ? 1 : 0)) != instruction.Args.Count)
        {
            throw new InvalidOperationException($"Method parameter count mismatch: expected {method.GetParameters().Length - (isExtensionMethod ? 1 : 0)}, found {instruction.Args.Count}");
        }

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
                        if (jsonElement.ValueKind == JsonValueKind.String &&
                            _variables.TryGetValue(jsonElement.GetString()!, out var varValue) &&
                            varValue != null &&
                            methodParameters[i].ParameterType.IsAssignableFrom(varValue.GetType()))
                        {
                            arguments[i] = varValue;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Failed to convert argument '{paramName}' to type '{methodParameters[i].ParameterType}': {ex.Message}");
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
