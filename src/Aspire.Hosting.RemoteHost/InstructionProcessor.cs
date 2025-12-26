// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Processes instructions from a polyglot AppHost client (e.g., TypeScript).
/// This class handles object marshalling, method invocation, and instruction execution.
/// </summary>
internal sealed class InstructionProcessor : IAsyncDisposable
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

    private readonly ObjectRegistry _objectRegistry;
    private readonly ICallbackInvoker _callbackInvoker;

    private static readonly TimeSpan s_shutdownTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates a new InstructionProcessor with the specified dependencies.
    /// </summary>
    /// <param name="objectRegistry">The object registry for managing marshalled objects.</param>
    /// <param name="callbackInvoker">The callback invoker for invoking client callbacks.</param>
    public InstructionProcessor(ObjectRegistry objectRegistry, ICallbackInvoker callbackInvoker)
    {
        _objectRegistry = objectRegistry;
        _callbackInvoker = callbackInvoker;
    }

    /// <summary>
    /// Gets the object registry used by this processor.
    /// </summary>
    public ObjectRegistry ObjectRegistry => _objectRegistry;

    /// <summary>
    /// Gets the callback invoker used by this processor.
    /// </summary>
    public ICallbackInvoker CallbackInvoker => _callbackInvoker;

    #region Object Marshalling

    /// <summary>
    /// Unregisters an object from the registry.
    /// </summary>
    public void UnregisterObject(string objectId)
    {
        _objectRegistry.Unregister(objectId);
    }

    #region Service Resolution

    // Well-known types mapped to their actual Type objects for fast lookup
    private static readonly Dictionary<string, Type> s_wellKnownTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IConfiguration"] = typeof(Microsoft.Extensions.Configuration.IConfiguration),
        ["IHostEnvironment"] = typeof(Microsoft.Extensions.Hosting.IHostEnvironment),
        ["ILoggerFactory"] = typeof(Microsoft.Extensions.Logging.ILoggerFactory),
    };

    /// <summary>
    /// Resolves a service from an IServiceProvider by type name.
    /// Supports well-known type aliases (e.g., "IConfiguration") or assembly-qualified type names.
    /// </summary>
    public object? GetService(string serviceProviderObjectId, string typeName)
    {
        var serviceProvider = _objectRegistry.Get(serviceProviderObjectId) as IServiceProvider
            ?? throw new InvalidOperationException($"Object '{serviceProviderObjectId}' is not an IServiceProvider");

        var type = ResolveType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found. Use a well-known alias (IConfiguration, IHostEnvironment, ILoggerFactory) or an assembly-qualified type name.");

        var service = serviceProvider.GetService(type);
        if (service != null && !ObjectRegistry.IsSimpleType(service.GetType()))
        {
            return _objectRegistry.Marshal(service);
        }
        return service;
    }

    /// <summary>
    /// Resolves a required service from an IServiceProvider by type name.
    /// Throws if the service is not found.
    /// </summary>
    public object? GetRequiredService(string serviceProviderObjectId, string typeName)
    {
        var serviceProvider = _objectRegistry.Get(serviceProviderObjectId) as IServiceProvider
            ?? throw new InvalidOperationException($"Object '{serviceProviderObjectId}' is not an IServiceProvider");

        var type = ResolveType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found. Use a well-known alias (IConfiguration, IHostEnvironment, ILoggerFactory) or an assembly-qualified type name.");

        var service = serviceProvider.GetService(type)
            ?? throw new InvalidOperationException($"Required service of type '{typeName}' was not found in the service provider.");

        if (!ObjectRegistry.IsSimpleType(service.GetType()))
        {
            return _objectRegistry.Marshal(service);
        }
        return service;
    }

    /// <summary>
    /// Resolves a type by name. Supports well-known aliases or assembly-qualified type names.
    /// </summary>
    private static Type? ResolveType(string typeName)
    {
        // Check for well-known type alias first (fast path)
        if (s_wellKnownTypes.TryGetValue(typeName, out var knownType))
        {
            return knownType;
        }

        // For everything else, require assembly-qualified name
        return Type.GetType(typeName);
    }

    #endregion

    /// <summary>
    /// Invokes a method on a registered object. Called by TypeScript via JSON-RPC.
    /// </summary>
    public object? InvokeMethod(string objectId, string methodName, JsonElement? args)
    {
        var obj = _objectRegistry.Get(objectId);

        var type = obj.GetType();
        var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (methods.Count == 0)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found on type '{type.Name}'");
        }

        // Parse arguments
        var argDict = new Dictionary<string, JsonElement>();
        if (args.HasValue && args.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in args.Value.EnumerateObject())
            {
                argDict[prop.Name] = prop.Value;
            }
        }

        // Find best matching method based on argument count/names
        System.Reflection.MethodInfo? bestMethod = null;
        var bestScore = -1;

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var score = 0;

            // Check if all provided args match parameter names
            var paramNames = parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var argName in argDict.Keys)
            {
                if (paramNames.Contains(argName))
                {
                    score += 10;
                }
            }

            // Penalize for missing required parameters
            foreach (var param in parameters)
            {
                if (!param.HasDefaultValue && !argDict.ContainsKey(param.Name!))
                {
                    score -= 100;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMethod = method;
            }
        }

        if (bestMethod == null)
        {
            bestMethod = methods[0];
        }

        // Build argument array
        var parameters2 = bestMethod.GetParameters();
        var arguments = new object?[parameters2.Length];

        for (int i = 0; i < parameters2.Length; i++)
        {
            var param = parameters2[i];
            if (argDict.TryGetValue(param.Name!, out var argValue))
            {
                arguments[i] = DeserializeArgument(argValue, param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                arguments[i] = param.DefaultValue;
            }
            else
            {
                throw new InvalidOperationException($"Required argument '{param.Name}' not provided for method '{methodName}'");
            }
        }

        // Invoke the method
        var result = bestMethod.Invoke(obj, arguments);

        // If result is a complex object, register it and return a proxy representation
        if (result != null && !ObjectRegistry.IsSimpleType(result.GetType()))
        {
            return _objectRegistry.Marshal(result);
        }

        return result;
    }

    /// <summary>
    /// Gets a property value from a registered object. Called by TypeScript via JSON-RPC.
    /// </summary>
    public object? GetProperty(string objectId, string propertyName)
    {
        var obj = _objectRegistry.Get(objectId);

        var type = obj.GetType();
        var property = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on type '{type.Name}'");
        }

        var value = property.GetValue(obj);

        // If value is a complex object, register it and return a proxy representation
        if (value != null && !ObjectRegistry.IsSimpleType(value.GetType()))
        {
            return _objectRegistry.Marshal(value);
        }

        return value;
    }

    /// <summary>
    /// Sets a property value on a registered object. Called by TypeScript via JSON-RPC.
    /// </summary>
    public void SetProperty(string objectId, string propertyName, JsonElement value)
    {
        var obj = _objectRegistry.Get(objectId);

        var type = obj.GetType();
        var property = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on type '{type.Name}'");
        }

        if (!property.CanWrite)
        {
            throw new InvalidOperationException($"Property '{propertyName}' is read-only");
        }

        var deserializedValue = DeserializeArgument(value, property.PropertyType);
        property.SetValue(obj, deserializedValue);
    }

    /// <summary>
    /// Gets an item from an indexer (e.g., dictionary[key]). Called by TypeScript via JSON-RPC.
    /// </summary>
    public object? GetIndexer(string objectId, JsonElement key)
    {
        var obj = _objectRegistry.Get(objectId);

        // Handle dictionary-like objects
        if (obj is System.Collections.IDictionary dict)
        {
            var keyValue = key.ValueKind == JsonValueKind.String ? key.GetString() : key.ToString();
            if (dict.Contains(keyValue!))
            {
                var value = dict[keyValue!];
                if (value != null && !ObjectRegistry.IsSimpleType(value.GetType()))
                {
                    return _objectRegistry.Marshal(value);
                }
                return value;
            }
            return null;
        }

        // Handle list-like objects (IList)
        if (obj is System.Collections.IList list)
        {
            if (key.ValueKind == JsonValueKind.Number && key.TryGetInt32(out var index))
            {
                if (index < 0 || index >= list.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(key), $"Index {index} is out of range for list of count {list.Count}");
                }
                var value = list[index];
                if (value != null && !ObjectRegistry.IsSimpleType(value.GetType()))
                {
                    return _objectRegistry.Marshal(value);
                }
                return value;
            }
            throw new InvalidOperationException($"List indexer requires a numeric index, got: {key.ValueKind}");
        }

        // Handle indexed properties
        var type = obj.GetType();
        var indexer = type.GetProperties().FirstOrDefault(p => p.GetIndexParameters().Length > 0);

        if (indexer != null)
        {
            var indexParam = indexer.GetIndexParameters()[0];
            var keyValue = DeserializeArgument(key, indexParam.ParameterType);
            var value = indexer.GetValue(obj, [keyValue]);

            if (value != null && !ObjectRegistry.IsSimpleType(value.GetType()))
            {
                return _objectRegistry.Marshal(value);
            }
            return value;
        }

        throw new InvalidOperationException($"Object '{objectId}' does not support indexing");
    }

    /// <summary>
    /// Sets an item via an indexer (e.g., dictionary[key] = value or list[index] = value). Called by TypeScript via JSON-RPC.
    /// </summary>
    public void SetIndexer(string objectId, JsonElement key, JsonElement value)
    {
        var obj = _objectRegistry.Get(objectId);

        // Resolve the value if it's a proxy reference
        var resolvedValue = _objectRegistry.ResolveValue(value);

        // Handle dictionary-like objects
        if (obj is System.Collections.IDictionary dict)
        {
            var keyValue = key.ValueKind == JsonValueKind.String ? key.GetString() : key.ToString();
            dict[keyValue!] = resolvedValue;
            return;
        }

        // Handle list-like objects (IList)
        if (obj is System.Collections.IList list)
        {
            if (key.ValueKind == JsonValueKind.Number && key.TryGetInt32(out var index))
            {
                // Get the list's element type and convert the value appropriately
                var listType = obj.GetType();
                var elementType = listType.IsGenericType
                    ? listType.GetGenericArguments()[0]
                    : typeof(object);

                var typedValue = resolvedValue;
                if (resolvedValue != null && elementType != typeof(object))
                {
                    typedValue = Convert.ChangeType(resolvedValue, elementType, System.Globalization.CultureInfo.InvariantCulture);
                }
                list[index] = typedValue;
                return;
            }
            throw new InvalidOperationException($"List indexer requires a numeric index, got: {key.ValueKind}");
        }

        // Handle indexed properties via reflection
        var type = obj.GetType();
        var indexer = type.GetProperties().FirstOrDefault(p => p.GetIndexParameters().Length > 0);

        if (indexer != null)
        {
            var indexParam = indexer.GetIndexParameters()[0];
            var keyValue = DeserializeArgument(key, indexParam.ParameterType);
            var valueType = indexer.PropertyType;
            var typedValue = resolvedValue != null ? Convert.ChangeType(resolvedValue, valueType, System.Globalization.CultureInfo.InvariantCulture) : null;
            indexer.SetValue(obj, typedValue, [keyValue]);
            return;
        }

        throw new InvalidOperationException($"Object '{objectId}' does not support indexed assignment");
    }

    /// <summary>
    /// Sets an item via an indexer with a string key. Called by TypeScript via JSON-RPC.
    /// </summary>
    public void SetIndexerByStringKey(string objectId, string key, object? value)
    {
        var obj = _objectRegistry.Get(objectId);

        // Resolve the value if it's a proxy reference
        var resolvedValue = _objectRegistry.ResolveValueObject(value);

        // Handle dictionary-like objects
        if (obj is System.Collections.IDictionary dict)
        {
            dict[key] = resolvedValue;
            return;
        }

        // Handle list-like objects with numeric string key
        if (obj is System.Collections.IList list && int.TryParse(key, out var index))
        {
            list[index] = resolvedValue;
            return;
        }

        throw new InvalidOperationException($"Object '{objectId}' does not support indexed assignment");
    }

    /// <summary>
    /// Gets an item from an indexer with a string key.
    /// </summary>
    public object? GetIndexerByStringKey(string objectId, string key)
    {
        var obj = _objectRegistry.Get(objectId);

        // Handle dictionary-like objects
        if (obj is System.Collections.IDictionary dict)
        {
            if (dict.Contains(key))
            {
                var value = dict[key];
                if (value != null && !ObjectRegistry.IsSimpleType(value.GetType()))
                {
                    return _objectRegistry.Marshal(value);
                }
                return value;
            }
            return null;
        }

        throw new InvalidOperationException($"Object '{objectId}' does not support indexing");
    }

    /// <summary>
    /// Deserializes a JSON argument to the target type.
    /// </summary>
    private object? DeserializeArgument(JsonElement element, Type targetType)
    {
        // Handle object references (proxied objects from TypeScript)
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("$id", out var idProp))
        {
            var refId = idProp.GetString();
            if (refId != null && _objectRegistry.TryGet(refId, out var refObj))
            {
                return refObj;
            }
        }

        // Handle ReferenceExpression from TypeScript
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("$referenceExpression", out var refExprMarker) &&
            refExprMarker.GetBoolean() &&
            targetType.FullName == "Aspire.Hosting.ApplicationModel.ReferenceExpression")
        {
            return DeserializeReferenceExpression(element);
        }

        // Handle null
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        // Handle callback parameters (delegate type with string callbackId)
        if (IsDelegateType(targetType) && element.ValueKind == JsonValueKind.String)
        {
            var callbackId = element.GetString()!;
            var proxy = CreateCallbackProxy(callbackId, targetType);
            if (proxy != null)
            {
                return proxy;
            }
        }

        // Handle primitives
        if (targetType == typeof(string))
        {
            return element.GetString();
        }
        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            return element.GetInt32();
        }
        if (targetType == typeof(long) || targetType == typeof(long?))
        {
            return element.GetInt64();
        }
        if (targetType == typeof(double) || targetType == typeof(double?))
        {
            return element.GetDouble();
        }
        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return element.GetBoolean();
        }

        // Fall back to JSON deserialization
        return JsonSerializer.Deserialize(element.GetRawText(), targetType, _jsonOptions);
    }

    /// <summary>
    /// Deserializes a ReferenceExpression from a TypeScript-constructed format.
    /// Format: { "$referenceExpression": true, "format": "Host={obj_1};Password={obj_2}" }
    /// The {obj_N} placeholders reference objects in the registry.
    /// </summary>
    private object DeserializeReferenceExpression(JsonElement element)
    {
        if (!element.TryGetProperty("format", out var formatElement))
        {
            throw new InvalidOperationException("Invalid ReferenceExpression format: missing 'format' property");
        }

        var format = formatElement.GetString() ?? "";

        // Use ReferenceExpressionBuilder to construct the expression
        var builder = new Aspire.Hosting.ApplicationModel.ReferenceExpressionBuilder();

        // Parse the format string, finding {obj_N} placeholders
        var regex = new System.Text.RegularExpressions.Regex(@"\{(obj_\d+)\}");
        var lastIndex = 0;

        foreach (System.Text.RegularExpressions.Match match in regex.Matches(format))
        {
            // Append literal text before this placeholder
            if (match.Index > lastIndex)
            {
                builder.AppendLiteral(format[lastIndex..match.Index]);
            }

            // Look up the object in the registry and append it
            var objectId = match.Groups[1].Value;
            if (_objectRegistry.TryGet(objectId, out var obj) && obj != null)
            {
                builder.AppendValueProvider(obj);
            }
            else
            {
                // Object not found - include the placeholder as literal
                builder.AppendLiteral(match.Value);
            }

            lastIndex = match.Index + match.Length;
        }

        // Append any remaining literal text
        if (lastIndex < format.Length)
        {
            builder.AppendLiteral(format[lastIndex..]);
        }

        return builder.Build();
    }

    #endregion

    #region Callback Invocation

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
        // Convert complex objects to a marshalled representation with object IDs
        var serializableArgs = args != null && !ObjectRegistry.IsSimpleType(args.GetType())
            ? _objectRegistry.Marshal(args)
            : args;

        return await _callbackInvoker.InvokeAsync<TResult>(callbackId, serializableArgs, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes a callback that returns no value.
    /// </summary>
    public async Task InvokeCallbackAsync(string callbackId, object? args, CancellationToken cancellationToken = default)
    {
        await InvokeCallbackAsync<object?>(callbackId, args, cancellationToken).ConfigureAwait(false);
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
                // Use Task.Run to avoid blocking the RPC dispatcher thread.
                // Without this, if the callback calls back to .NET, we'd deadlock
                // because the dispatcher can't process the response while blocked.
                Task.Run(() => InvokeCallbackAsync(callbackId, null)).GetAwaiter().GetResult();
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
        // Use Task.Run to avoid blocking the RPC dispatcher thread
        return arg => Task.Run(() => InvokeCallbackAsync(callbackId, arg)).GetAwaiter().GetResult();
    }

    private Func<T, Task> CreateAsyncActionProxy<T>(string callbackId)
    {
        return arg => InvokeCallbackAsync(callbackId, arg);
    }

    private Func<T, TResult> CreateFuncProxy<T, TResult>(string callbackId)
    {
        // Use Task.Run to avoid blocking the RPC dispatcher thread
        return arg => Task.Run(() => InvokeCallbackAsync<TResult>(callbackId, arg)).GetAwaiter().GetResult();
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

    #endregion

    #region Instruction Execution

    public async Task<object?> ExecuteInstructionAsync(string instructionJson, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var jsonDocument = JsonDocument.Parse(instructionJson);
        var instructionName = jsonDocument.RootElement.GetProperty("name").GetString();

        return instructionName switch
        {
            "CREATE_BUILDER" => await ExecuteCreateBuilderAsync(instructionJson, cancellationToken).ConfigureAwait(false),
            "RUN_BUILDER" => await ExecuteRunBuilderAsync(instructionJson, cancellationToken).ConfigureAwait(false),
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

        // Register the builder in the ObjectRegistry so it can be accessed via proxy
        // This enables access to Configuration, Environment, Services properties
        var marshalledBuilder = _objectRegistry.Marshal(builder);

        return Task.FromResult<object>(new { success = true, builderName = instruction.BuilderName, builder = marshalledBuilder });
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

        // Register the app in the ObjectRegistry for property access (Services, etc.)
        var marshalledApp = _objectRegistry.Marshal(app);

        // Track the app for graceful shutdown
        lock (_appsLock)
        {
            _runningApps.Add(app);
        }

        try
        {
            // Start the application and wait for startup to complete
            // This will throw if startup fails (e.g., port conflict)
            await app.StartAsync(cancellationToken).ConfigureAwait(false);

            // The app is now running in the background.
            // When the server shuts down, DisposeAsync will stop all running apps.

            return new { success = true, builderName = instruction.BuilderName, status = "running", app = marshalledApp };
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
        var assembly = _assemblyCache.GetOrAdd(instruction.MethodAssembly, System.Reflection.Assembly.Load);

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
                var matchingArgs = providedArgNames.Count(methodParamNames.Contains);
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

        // Marshal the result so TypeScript can use it as a DotNetProxy
        var marshalledResult = result != null ? _objectRegistry.Marshal(result) : null;

        return new {
            success = true,
            source = instruction.Source,
            target = instruction.Target,
            methodName = instruction.MethodName,
            result = marshalledResult
        };
    }

    #endregion

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
                using var cts = new CancellationTokenSource(s_shutdownTimeout);
                try
                {
                    await app.StopAsync(cts.Token).ConfigureAwait(false);
                    Console.WriteLine("DistributedApplication stopped.");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Warning: DistributedApplication stop timed out after {s_shutdownTimeout.TotalSeconds}s");
                }

                // Dispose the app to clean up resources (no timeout - dispose should be quick)
                await app.DisposeAsync().ConfigureAwait(false);
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
        _objectRegistry.Clear();

        Console.WriteLine("InstructionProcessor disposed.");
    }
}
