// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using Aspire.Hosting;
using StreamJsonRpc;

namespace RemoteAppHost;

public class InstructionProcessor : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, object> _variables = new();
    private readonly ConcurrentDictionary<string, System.Reflection.Assembly> _assemblyCache = new();
    private readonly List<DistributedApplication> _runningApps = new();
    private readonly object _appsLock = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private volatile bool _disposed;
    private JsonRpc? _clientRpc;

    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CallbackTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Sets the JSON-RPC connection to use for invoking callbacks on the client.
    /// </summary>
    public void SetClientConnection(JsonRpc clientRpc)
    {
        _clientRpc = clientRpc;
    }

    /// <summary>
    /// Invokes a callback registered on the client side.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="callbackId">The callback ID registered on the client.</param>
    /// <param name="args">Arguments to pass to the callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result from the callback.</returns>
    public async Task<TResult> InvokeCallbackAsync<TResult>(string callbackId, object? args, CancellationToken cancellationToken = default)
    {
        if (_clientRpc == null)
        {
            throw new InvalidOperationException("No client connection available for callback invocation");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(CallbackTimeout);

        try
        {
            return await _clientRpc.InvokeWithCancellationAsync<TResult>(
                "invokeCallback",
                [callbackId, args],
                cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Callback '{callbackId}' timed out after {CallbackTimeout.TotalSeconds}s");
        }
    }

    /// <summary>
    /// Invokes a callback that returns no value.
    /// </summary>
    public async Task InvokeCallbackAsync(string callbackId, object? args, CancellationToken cancellationToken = default)
    {
        await InvokeCallbackAsync<object?>(callbackId, args, cancellationToken);
    }

    /// <summary>
    /// Creates a proxy delegate that invokes a callback on the TypeScript client.
    /// </summary>
    private object? CreateCallbackProxy(string callbackId, Type delegateType)
    {
        // Handle common delegate patterns
        // We need to create a delegate that, when invoked, calls back to TypeScript

        if (delegateType == typeof(Action))
        {
            return new Action(() =>
            {
                InvokeCallbackAsync(callbackId, null).GetAwaiter().GetResult();
            });
        }

        // Check for Func<Task> (async action with no args)
        if (delegateType == typeof(Func<Task>))
        {
            return new Func<Task>(() => InvokeCallbackAsync(callbackId, null));
        }

        // Check for Func<CancellationToken, Task> (async action with cancellation)
        if (delegateType == typeof(Func<CancellationToken, Task>))
        {
            return new Func<CancellationToken, Task>(ct => InvokeCallbackAsync(callbackId, null, ct));
        }

        // Handle generic Action<T>
        if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(Action<>))
        {
            var argType = delegateType.GetGenericArguments()[0];
            var proxyMethod = GetType().GetMethod(nameof(CreateActionProxy), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(argType);
            return proxyMethod.Invoke(this, [callbackId]);
        }

        // Handle Func<T, Task> (async with one arg)
        if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(Func<,>))
        {
            var args = delegateType.GetGenericArguments();
            if (args[1] == typeof(Task))
            {
                var proxyMethod = GetType().GetMethod(nameof(CreateAsyncActionProxy), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(args[0]);
                return proxyMethod.Invoke(this, [callbackId]);
            }
            // Func<T, TResult> - sync function with return value
            var funcProxyMethod = GetType().GetMethod(nameof(CreateFuncProxy), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(args[0], args[1]);
            return funcProxyMethod.Invoke(this, [callbackId]);
        }

        // Handle Func<T, Task<TResult>> (async with one arg and return value)
        if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(Func<,>))
        {
            var args = delegateType.GetGenericArguments();
            if (args[1].IsGenericType && args[1].GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = args[1].GetGenericArguments()[0];
                var proxyMethod = GetType().GetMethod(nameof(CreateAsyncFuncProxy), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(args[0], resultType);
                return proxyMethod.Invoke(this, [callbackId]);
            }
        }

        // Handle Func<T1, T2, Task> (async with two args)
        if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(Func<,,>))
        {
            var args = delegateType.GetGenericArguments();
            if (args[2] == typeof(Task))
            {
                var proxyMethod = GetType().GetMethod(nameof(CreateAsyncAction2Proxy), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(args[0], args[1]);
                return proxyMethod.Invoke(this, [callbackId]);
            }
        }

        Console.WriteLine($"Warning: Unsupported delegate type for callback: {delegateType}");
        return null;
    }

    // Helper methods for creating typed proxy delegates
    private Action<T> CreateActionProxy<T>(string callbackId)
    {
        return arg => InvokeCallbackAsync(callbackId, arg).GetAwaiter().GetResult();
    }

    private Func<T, Task> CreateAsyncActionProxy<T>(string callbackId)
    {
        return arg => InvokeCallbackAsync(callbackId, arg);
    }

    private Func<T, TResult> CreateFuncProxy<T, TResult>(string callbackId)
    {
        return arg => InvokeCallbackAsync<TResult>(callbackId, arg).GetAwaiter().GetResult();
    }

    private Func<T, Task<TResult>> CreateAsyncFuncProxy<T, TResult>(string callbackId)
    {
        return arg => InvokeCallbackAsync<TResult>(callbackId, arg);
    }

    private Func<T1, T2, Task> CreateAsyncAction2Proxy<T1, T2>(string callbackId)
    {
        return (arg1, arg2) => InvokeCallbackAsync(callbackId, new { arg1, arg2 });
    }

    /// <summary>
    /// Checks if a type is a delegate type.
    /// </summary>
    private static bool IsDelegateType(Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type);
    }

    public async Task<object?> ExecuteInstructionAsync(string instructionJson, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var jsonDocument = JsonDocument.Parse(instructionJson);
        var instructionName = jsonDocument.RootElement.GetProperty("name").GetString();

        return instructionName switch
        {
            "CREATE_BUILDER" => await ExecuteCreateBuilderAsync(instructionJson, cancellationToken),
            "RUN_BUILDER" => await ExecuteRunBuilderAsync(instructionJson, cancellationToken),
            "pragma" => ExecutePragma(instructionJson),
            "INVOKE" => ExecuteInvoke(instructionJson),
            _ => throw new NotSupportedException($"Instruction '{instructionName}' is not supported")
        };
    }

    private Task<object> ExecuteCreateBuilderAsync(string instructionJson, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var instruction = JsonSerializer.Deserialize<CreateBuilderInstruction>(instructionJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize CREATE_BUILDER instruction");

        var options = new DistributedApplicationOptions
        {
            Args = instruction.Args ?? []
        };

        // Create the distributed application builder
        var builder = DistributedApplication.CreateBuilder(options);

        // Store the builder in the variables dictionary (thread-safe)
        _variables[instruction.BuilderName] = builder;

        return Task.FromResult<object>(new { success = true, builderName = instruction.BuilderName });
    }

    private async Task<object> ExecuteRunBuilderAsync(string instructionJson, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var instruction = JsonSerializer.Deserialize<RunBuilderInstruction>(instructionJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize RUN_BUILDER instruction");

        if (!_variables.TryGetValue(instruction.BuilderName, out var builderObj) ||
            builderObj is not IDistributedApplicationBuilder builder)
        {
            throw new InvalidOperationException($"Builder '{instruction.BuilderName}' not found or is not a valid builder");
        }

        // Build and start the application
        var app = builder.Build();

        // Store the app so we can access it later for shutdown (thread-safe)
        _variables[$"{instruction.BuilderName}_app"] = app;

        // Track the app for graceful shutdown
        lock (_appsLock)
        {
            _runningApps.Add(app);
        }

        try
        {
            // Start the application and wait for startup to complete
            // This will throw if startup fails (e.g., port conflict)
            await app.StartAsync(cancellationToken);

            // The app is now running in the background.
            // When the server shuts down, DisposeAsync will stop all running apps.

            return new { success = true, builderName = instruction.BuilderName, status = "running" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application startup failed: {ex.Message}");

            // Remove from tracking since it failed to start
            lock (_appsLock)
            {
                _runningApps.Remove(app);
            }

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

        // Get the source object from variables (thread-safe)
        if (!_variables.TryGetValue(instruction.Source, out var sourceObj))
        {
            throw new InvalidOperationException($"Source variable '{instruction.Source}' not found");
        }

        // Load the assembly (cached)
        var assembly = _assemblyCache.GetOrAdd(instruction.MethodAssembly,
            assemblyName => System.Reflection.Assembly.Load(assemblyName));

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
            var providedArgNames = instruction.Args.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Find all methods with the matching name
            // First, check if any method has a PolyglotMethodNameAttribute that matches
            // This allows polyglot SDKs to use unique names for overloads
            var candidateMethods = type.GetMethods()
                .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                .Where(m =>
                {
                    // Check for PolyglotMethodNameAttribute first (using reflection to avoid type dependency)
                    var polyglotAttr = m.GetCustomAttributesData()
                        .FirstOrDefault(a => a.AttributeType.Name == "PolyglotMethodNameAttribute");

                    if (polyglotAttr != null)
                    {
                        // Get the MethodName from the constructor argument
                        var methodName = polyglotAttr.ConstructorArguments.FirstOrDefault().Value as string;
                        if (methodName != null)
                        {
                            // Match by polyglot name (case-insensitive)
                            return string.Equals(methodName, instruction.MethodName, StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    // Fall back to C# method name
                    return m.Name == instruction.MethodName;
                })
                .ToList();

            // Score each candidate based on argument name matching and source type compatibility
            var scoredCandidates = new List<(System.Reflection.MethodInfo Method, int Score, bool SourceTypeMatches)>();

            foreach (var candidate in candidateMethods)
            {
                var parameters = candidate.GetParameters();
                if (parameters.Length == 0)
                {
                    continue;
                }

                // Check if the source type matches the first parameter (extension method target)
                var firstParamType = parameters[0].ParameterType;
                var sourceTypeMatches = false;

                if (firstParamType.IsGenericType)
                {
                    var genericTypeDef = firstParamType.GetGenericTypeDefinition();
                    var sourceInterfaces = sourceType.GetInterfaces();
                    sourceTypeMatches = sourceInterfaces.Any(iface =>
                        iface.IsGenericType && iface.GetGenericTypeDefinition() == genericTypeDef);
                }
                else
                {
                    sourceTypeMatches = firstParamType.IsAssignableFrom(sourceType);
                }

                if (!sourceTypeMatches)
                {
                    continue;
                }

                // Score based on how many provided argument names match method parameter names
                // Skip the first parameter (this) for extension methods
                var methodParamNames = parameters.Skip(1).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var matchingArgs = providedArgNames.Count(argName => methodParamNames.Contains(argName));
                var missingRequiredArgs = parameters.Skip(1).Count(p => !p.HasDefaultValue && !providedArgNames.Contains(p.Name!));

                // Higher score = better match
                // Penalize methods that have required arguments we didn't provide
                var score = matchingArgs * 10 - missingRequiredArgs * 100;

                scoredCandidates.Add((candidate, score, sourceTypeMatches));
            }

            // Pick the best scoring method
            method = scoredCandidates
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Method.GetParameters().Length) // Prefer simpler methods if tied
                .Select(x => x.Method)
                .FirstOrDefault();

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
                var paramType = methodParameters[i].ParameterType;

                // Convert JsonElement to the appropriate type if needed
                if (argValue is JsonElement jsonElement)
                {
                    // Check if this is a callback parameter (delegate type with string callbackId)
                    if (IsDelegateType(paramType) && jsonElement.ValueKind == JsonValueKind.String)
                    {
                        var callbackId = jsonElement.GetString()!;
                        var proxy = CreateCallbackProxy(callbackId, paramType);
                        if (proxy != null)
                        {
                            arguments[i] = proxy;
                            continue;
                        }
                    }

                    try
                    {
                        arguments[i] = JsonSerializer.Deserialize(jsonElement.GetRawText(), paramType, _jsonOptions);
                    }
                    catch (Exception ex)
                    {
                        if (jsonElement.ValueKind == JsonValueKind.String
                            && _variables.TryGetValue(jsonElement.GetString()!, out var varValue)
                            && varValue != null
                            )
                        {
                            // Check the type compatibility. This may be an error if the wrong extension method was picked by the code generation.
                            if (!paramType.IsAssignableFrom(varValue.GetType()))
                            {
                                throw new InvalidOperationException($"Failed to convert argument '{paramName}' to type '{paramType}': {ex.Message}");
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
            else if (methodParameters[i].HasDefaultValue)
            {
                // Use the default value for optional parameters
                arguments[i] = methodParameters[i].DefaultValue;
            }
            else
            {
                throw new InvalidOperationException($"Required argument '{paramName}' not found in instruction args for method parameter at index {i}");
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Get a copy of the running apps to stop
        List<DistributedApplication> appsToStop;
        lock (_appsLock)
        {
            appsToStop = new List<DistributedApplication>(_runningApps);
            _runningApps.Clear();
        }

        Console.WriteLine($"Stopping {appsToStop.Count} running application(s)...");

        // Stop all running applications gracefully with timeout
        foreach (var app in appsToStop)
        {
            try
            {
                Console.WriteLine("Stopping DistributedApplication...");

                // Use a timeout to prevent hanging indefinitely
                using var cts = new CancellationTokenSource(ShutdownTimeout);
                try
                {
                    await app.StopAsync(cts.Token);
                    Console.WriteLine("DistributedApplication stopped.");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Warning: DistributedApplication stop timed out after {ShutdownTimeout.TotalSeconds}s");
                }

                // Dispose the app to clean up resources (no timeout - dispose should be quick)
                await app.DisposeAsync();
                Console.WriteLine("DistributedApplication disposed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping application: {ex.Message}");
            }
        }

        // Clear all variables
        _variables.Clear();
        _assemblyCache.Clear();

        Console.WriteLine("InstructionProcessor disposed.");
    }
}
