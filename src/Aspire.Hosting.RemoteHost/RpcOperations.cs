// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Core RPC operations - testable without JSON-RPC transport.
/// Handles object marshalling, method invocation, and property access.
/// </summary>
internal sealed class RpcOperations : IAsyncDisposable
{
    private readonly ObjectRegistry _objectRegistry;
    private readonly TypeResolver _typeResolver;
    private readonly CallbackProxyFactory _callbackProxyFactory;
    private readonly ICallbackInvoker _callbackInvoker;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public RpcOperations(ObjectRegistry objectRegistry, ICallbackInvoker callbackInvoker)
    {
        _objectRegistry = objectRegistry;
        _callbackInvoker = callbackInvoker;
        _typeResolver = new TypeResolver();
        _callbackProxyFactory = new CallbackProxyFactory(callbackInvoker, objectRegistry);
    }

    /// <summary>
    /// Gets the object registry used by this processor.
    /// </summary>
    public ObjectRegistry ObjectRegistry => _objectRegistry;

    #region Object Lifecycle

    /// <summary>
    /// Creates an object instance.
    /// </summary>
    public object? CreateObject(string assemblyName, string typeName, JsonElement? args)
    {
        var type = _typeResolver.ResolveType(assemblyName, typeName);
        var argDict = MethodResolver.ParseArgs(args);

        var ctor = MethodResolver.FindBestConstructor(type, argDict);
        if (ctor == null)
        {
            throw new InvalidOperationException($"No suitable constructor found for type '{typeName}'");
        }

        var arguments = BuildArgumentArray(ctor, argDict);
        var result = ctor.Invoke(arguments);

        return MarshalResult(result);
    }

    /// <summary>
    /// Unregisters an object from the registry.
    /// </summary>
    public void UnregisterObject(string objectId)
    {
        _objectRegistry.Unregister(objectId);
    }

    #endregion

    #region Instance Operations

    /// <summary>
    /// Invokes a method on a registered object.
    /// </summary>
    public object? InvokeMethod(string objectId, string methodName, JsonElement? args)
    {
        var obj = _objectRegistry.Get(objectId);
        var type = obj.GetType();
        var argDict = MethodResolver.ParseArgs(args);

        var candidates = MethodResolver.GetInstanceMethods(type, methodName);
        var method = MethodResolver.FindBestMethod(candidates, argDict);

        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found on type '{type.Name}'");
        }

        var arguments = BuildArgumentArray(method, argDict);
        var result = method.Invoke(obj, arguments);

        return MarshalResult(result);
    }

    /// <summary>
    /// Gets a property value from a registered object.
    /// </summary>
    public object? GetProperty(string objectId, string propertyName)
    {
        var obj = _objectRegistry.Get(objectId);
        var type = obj.GetType();

        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on type '{type.Name}'");
        }

        var result = property.GetValue(obj);
        return MarshalResult(result);
    }

    /// <summary>
    /// Sets a property value on a registered object.
    /// </summary>
    public void SetProperty(string objectId, string propertyName, JsonElement value)
    {
        var obj = _objectRegistry.Get(objectId);
        var type = obj.GetType();

        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
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
    /// Gets an indexed value from a registered object (list or dictionary).
    /// </summary>
    public object? GetIndexer(string objectId, JsonElement key)
    {
        var obj = _objectRegistry.Get(objectId);

        // Handle dictionaries
        if (obj is System.Collections.IDictionary dict)
        {
            var keyValue = key.ValueKind == JsonValueKind.String ? key.GetString() : key.ToString();
            if (dict.Contains(keyValue!))
            {
                return MarshalResult(dict[keyValue!]);
            }
            return null;
        }

        // Handle lists
        if (obj is System.Collections.IList list)
        {
            int index;
            if (key.ValueKind == JsonValueKind.Number)
            {
                index = key.GetInt32();
            }
            else
            {
                var keyString = key.GetString()!;
                if (!int.TryParse(keyString, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out index))
                {
                    throw new InvalidOperationException($"Cannot convert key '{keyString}' to integer index for list");
                }
            }

            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(key), $"Index {index} is out of range for list with {list.Count} items");
            }

            return MarshalResult(list[index]);
        }

        throw new InvalidOperationException($"Object '{objectId}' does not support indexing");
    }

    /// <summary>
    /// Sets an indexed value on a registered object (list or dictionary).
    /// </summary>
    public void SetIndexer(string objectId, JsonElement key, JsonElement value)
    {
        var obj = _objectRegistry.Get(objectId);

        // Handle dictionaries
        if (obj is System.Collections.IDictionary dict)
        {
            var keyValue = key.ValueKind == JsonValueKind.String ? key.GetString() : key.ToString();
            var resolvedValue = _objectRegistry.ResolveValue(value);
            dict[keyValue!] = resolvedValue;
            return;
        }

        // Handle lists
        if (obj is System.Collections.IList list)
        {
            var index = key.ValueKind == JsonValueKind.Number
                ? key.GetInt32()
                : int.Parse(key.GetString()!, System.Globalization.CultureInfo.InvariantCulture);

            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(key), $"Index {index} is out of range for list with {list.Count} items");
            }

            var resolvedValue = _objectRegistry.ResolveValue(value);

            // Convert to the list's element type if needed
            var elementType = list.GetType().IsGenericType
                ? list.GetType().GetGenericArguments()[0]
                : typeof(object);

            if (resolvedValue != null && elementType != typeof(object) && resolvedValue.GetType() != elementType)
            {
                resolvedValue = Convert.ChangeType(resolvedValue, elementType, System.Globalization.CultureInfo.InvariantCulture);
            }

            list[index] = resolvedValue;
            return;
        }

        throw new InvalidOperationException($"Object '{objectId}' does not support indexing");
    }

    #endregion

    #region Static Operations

    /// <summary>
    /// Invokes a static method on a type.
    /// </summary>
    public object? InvokeStaticMethod(string assemblyName, string typeName, string methodName, JsonElement? args)
    {
        var type = _typeResolver.ResolveType(assemblyName, typeName);
        var argDict = MethodResolver.ParseArgs(args);

        var candidates = MethodResolver.GetStaticMethods(type, methodName, includeExtensions: true);
        var method = MethodResolver.FindBestMethod(candidates, argDict);

        if (method == null)
        {
            throw new InvalidOperationException($"Static method '{methodName}' not found on type '{typeName}'");
        }

        var arguments = BuildArgumentArray(method, argDict);

        // Handle generic methods
        if (method.ContainsGenericParameters)
        {
            method = MethodResolver.MakeGenericMethodFromArgs(method, arguments);
        }

        var result = method.Invoke(null, arguments);
        return MarshalResult(result);
    }

    /// <summary>
    /// Gets a static property value from a type.
    /// </summary>
    public object? GetStaticProperty(string assemblyName, string typeName, string propertyName)
    {
        var type = _typeResolver.ResolveType(assemblyName, typeName);

        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        if (property == null)
        {
            throw new InvalidOperationException($"Static property '{propertyName}' not found on type '{typeName}'");
        }

        var result = property.GetValue(null);
        return MarshalResult(result);
    }

    /// <summary>
    /// Sets a static property value on a type.
    /// </summary>
    public void SetStaticProperty(string assemblyName, string typeName, string propertyName, JsonElement value)
    {
        var type = _typeResolver.ResolveType(assemblyName, typeName);

        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        if (property == null)
        {
            throw new InvalidOperationException($"Static property '{propertyName}' not found on type '{typeName}'");
        }

        if (!property.CanWrite)
        {
            throw new InvalidOperationException($"Static property '{propertyName}' is read-only");
        }

        var deserializedValue = DeserializeArgument(value, property.PropertyType);
        property.SetValue(null, deserializedValue);
    }

    #endregion

    #region Helpers

    private object?[] BuildArgumentArray(MethodBase method, IReadOnlyDictionary<string, JsonElement> args)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (args.TryGetValue(param.Name!, out var argValue))
            {
                arguments[i] = DeserializeArgument(argValue, param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                arguments[i] = param.DefaultValue;
            }
            else
            {
                throw new InvalidOperationException($"Required argument '{param.Name}' not provided");
            }
        }

        return arguments;
    }

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
            var proxy = _callbackProxyFactory.CreateProxy(callbackId, targetType);
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

        // Handle arrays
        if (targetType.IsArray && element.ValueKind == JsonValueKind.Array)
        {
            var elementType = targetType.GetElementType()!;
            var items = element.EnumerateArray().ToList();
            var array = Array.CreateInstance(elementType, items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                array.SetValue(DeserializeArgument(items[i], elementType), i);
            }
            return array;
        }

        // Fall back to JSON deserialization
        return JsonSerializer.Deserialize(element.GetRawText(), targetType, _jsonOptions);
    }

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
        var regex = new Regex(@"\{(obj_\d+)\}");
        var lastIndex = 0;

        foreach (Match match in regex.Matches(format))
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

    private object? MarshalResult(object? result)
    {
        if (result == null)
        {
            return null;
        }

        if (ObjectRegistry.IsSimpleType(result.GetType()))
        {
            return result;
        }

        return _objectRegistry.Marshal(result);
    }

    private static bool IsDelegateType(Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type);
    }

    #endregion

    #region Convenience Methods

    /// <summary>
    /// Gets an indexed value using a string key.
    /// </summary>
    public object? GetIndexerByStringKey(string objectId, string key)
    {
        var obj = _objectRegistry.Get(objectId);

        // Handle dictionaries
        if (obj is System.Collections.IDictionary dict)
        {
            if (dict.Contains(key))
            {
                return MarshalResult(dict[key]);
            }
            return null;
        }

        // Handle lists with numeric string key
        if (obj is System.Collections.IList list)
        {
            var index = int.Parse(key, System.Globalization.CultureInfo.InvariantCulture);

            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(key), $"Index {index} is out of range for list with {list.Count} items");
            }

            return MarshalResult(list[index]);
        }

        throw new InvalidOperationException($"Object '{objectId}' does not support indexing");
    }

    /// <summary>
    /// Sets an indexed value using a string key.
    /// </summary>
    public void SetIndexerByStringKey(string objectId, string key, JsonElement value)
    {
        var obj = _objectRegistry.Get(objectId);

        // Handle dictionaries
        if (obj is System.Collections.IDictionary dict)
        {
            var resolvedValue = _objectRegistry.ResolveValue(value);
            dict[key] = resolvedValue;
            return;
        }

        // Handle lists with numeric string key
        if (obj is System.Collections.IList list)
        {
            var index = int.Parse(key, System.Globalization.CultureInfo.InvariantCulture);

            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(key), $"Index {index} is out of range for list with {list.Count} items");
            }

            var resolvedValue = _objectRegistry.ResolveValue(value);
            list[index] = resolvedValue;
            return;
        }

        throw new InvalidOperationException($"Object '{objectId}' does not support indexing");
    }

    #endregion

    public ValueTask DisposeAsync()
    {
        _typeResolver.Clear();
        _objectRegistry.Clear();
        return ValueTask.CompletedTask;
    }
}
