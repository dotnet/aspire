// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    /// Returns: MarshalledObject { "$id", "$type" } or null
    /// </summary>
    public JsonNode? CreateObject(string assemblyName, string typeName, JsonObject? args)
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
    /// Returns: null | JsonValue (primitive) | JsonObject { "$id", "$type" }
    /// </summary>
    public JsonNode? InvokeMethod(string objectId, string methodName, JsonObject? args)
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

        // Await async results before marshalling
        result = UnwrapAsyncResult(result);

        return MarshalResult(result);
    }

    /// <summary>
    /// Gets a property value from a registered object.
    /// Returns: null | JsonValue (primitive) | JsonObject { "$id", "$type" }
    /// </summary>
    public JsonNode? GetProperty(string objectId, string propertyName)
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
    public void SetProperty(string objectId, string propertyName, JsonNode? value)
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
    /// Returns: null | JsonValue (primitive) | JsonObject { "$id", "$type" }
    /// </summary>
    public JsonNode? GetIndexer(string objectId, JsonNode key)
    {
        var obj = _objectRegistry.Get(objectId);

        // Handle dictionaries
        if (obj is System.Collections.IDictionary dict)
        {
            var keyValue = key is JsonValue v ? v.GetValue<object>()?.ToString() : key.ToString();
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
            if (key is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var intValue))
            {
                index = intValue;
            }
            else
            {
                var keyString = key.ToString();
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
    public void SetIndexer(string objectId, JsonNode key, JsonNode? value)
    {
        var obj = _objectRegistry.Get(objectId);

        // Handle dictionaries
        if (obj is System.Collections.IDictionary dict)
        {
            var keyValue = key is JsonValue v ? v.GetValue<object>()?.ToString() : key.ToString();
            var resolvedValue = _objectRegistry.ResolveValue(value);
            dict[keyValue!] = resolvedValue;
            return;
        }

        // Handle lists
        if (obj is System.Collections.IList list)
        {
            int index;
            if (key is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var intValue))
            {
                index = intValue;
            }
            else
            {
                index = int.Parse(key.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            }

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
    /// Returns: null | JsonValue (primitive) | JsonObject { "$id", "$type" }
    /// </summary>
    public JsonNode? InvokeStaticMethod(string assemblyName, string typeName, string methodName, JsonObject? args)
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

        // Await async results before marshalling
        result = UnwrapAsyncResult(result);

        return MarshalResult(result);
    }

    /// <summary>
    /// Gets a static property value from a type.
    /// Returns: null | JsonValue (primitive) | JsonObject { "$id", "$type" }
    /// </summary>
    public JsonNode? GetStaticProperty(string assemblyName, string typeName, string propertyName)
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
    public void SetStaticProperty(string assemblyName, string typeName, string propertyName, JsonNode? value)
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

    private object?[] BuildArgumentArray(MethodBase method, IReadOnlyDictionary<string, JsonNode?> args)
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

    private object? DeserializeArgument(JsonNode? node, Type targetType)
    {
        if (node == null)
        {
            return null;
        }

        // Handle object references: { "$id": "obj_N" }
        var objectRef = ObjectRef.FromJsonNode(node);
        if (objectRef != null && _objectRegistry.TryGet(objectRef.Id, out var refObj))
        {
            return refObj;
        }

        // Handle ReferenceExpression: { "$referenceExpression": true, "format": "..." }
        if (targetType.FullName == "Aspire.Hosting.ApplicationModel.ReferenceExpression")
        {
            var refExpr = ReferenceExpressionRef.FromJsonNode(node);
            if (refExpr != null)
            {
                return DeserializeReferenceExpression(refExpr);
            }
        }

        // Handle callback parameters: "cb_xyz" string
        if (IsDelegateType(targetType) && node is JsonValue strValue && strValue.TryGetValue<string>(out var callbackId))
        {
            var proxy = _callbackProxyFactory.CreateProxy(callbackId, targetType);
            if (proxy != null)
            {
                return proxy;
            }
        }

        // Handle CancellationToken: { "$cancellationToken": "ct_N" }
        if (targetType == typeof(CancellationToken))
        {
            var tokenRef = CancellationTokenRef.FromJsonNode(node);
            if (tokenRef != null)
            {
                if (_callbackProxyFactory.CancellationTokenRegistry.TryGetToken(tokenRef.TokenId, out var token))
                {
                    return token;
                }
                throw new InvalidOperationException($"CancellationToken '{tokenRef.TokenId}' not found in registry");
            }
            // If no token ref provided, return CancellationToken.None
            return CancellationToken.None;
        }

        // Handle primitives via JsonValue
        if (node is JsonValue value)
        {
            // Get underlying type for nullable
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Strings
            if (actualType == typeof(string))
            {
                return value.GetValue<string>();
            }
            if (actualType == typeof(char))
            {
                var str = value.GetValue<string>();
                return str.Length > 0 ? str[0] : '\0';
            }

            // Boolean
            if (actualType == typeof(bool))
            {
                return value.GetValue<bool>();
            }

            // Integers
            if (actualType == typeof(byte))
            {
                return value.GetValue<byte>();
            }
            if (actualType == typeof(sbyte))
            {
                return value.GetValue<sbyte>();
            }
            if (actualType == typeof(short))
            {
                return value.GetValue<short>();
            }
            if (actualType == typeof(ushort))
            {
                return value.GetValue<ushort>();
            }
            if (actualType == typeof(int))
            {
                return value.GetValue<int>();
            }
            if (actualType == typeof(uint))
            {
                return value.GetValue<uint>();
            }
            if (actualType == typeof(long))
            {
                return value.GetValue<long>();
            }
            if (actualType == typeof(ulong))
            {
                return value.GetValue<ulong>();
            }

            // Floating point
            if (actualType == typeof(float))
            {
                return value.GetValue<float>();
            }
            if (actualType == typeof(double))
            {
                return value.GetValue<double>();
            }
            if (actualType == typeof(decimal))
            {
                return value.GetValue<decimal>();
            }

            // Date/Time (serialized as ISO 8601 strings)
            if (actualType == typeof(DateTime))
            {
                return value.GetValue<DateTime>();
            }
            if (actualType == typeof(DateTimeOffset))
            {
                return value.GetValue<DateTimeOffset>();
            }
            if (actualType == typeof(TimeSpan))
            {
                var str = value.GetValue<string>();
                return TimeSpan.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }
            if (actualType == typeof(DateOnly))
            {
                var str = value.GetValue<string>();
                return DateOnly.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }
            if (actualType == typeof(TimeOnly))
            {
                var str = value.GetValue<string>();
                return TimeOnly.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }

            // Identifiers
            if (actualType == typeof(Guid))
            {
                return value.GetValue<Guid>();
            }
            if (actualType == typeof(Uri))
            {
                var str = value.GetValue<string>();
                return new Uri(str);
            }

            // Enums (serialized as string names)
            if (actualType.IsEnum)
            {
                var str = value.GetValue<string>();
                return Enum.Parse(actualType, str, ignoreCase: true);
            }
        }

        // Handle byte[] as base64 string (special case)
        if (targetType == typeof(byte[]) && node is JsonValue base64Value)
        {
            var base64 = base64Value.GetValue<string>();
            return Convert.FromBase64String(base64);
        }

        // Handle arrays
        if (targetType.IsArray && node is JsonArray arr)
        {
            var elementType = targetType.GetElementType()!;
            var array = Array.CreateInstance(elementType, arr.Count);
            for (var i = 0; i < arr.Count; i++)
            {
                array.SetValue(DeserializeArgument(arr[i], elementType), i);
            }
            return array;
        }

        // Handle List<T>, IList<T>, ICollection<T>, IEnumerable<T>
        if (node is JsonArray jsonArr && targetType.IsGenericType)
        {
            var genericDef = targetType.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>))
            {
                var elementType = targetType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

                foreach (var item in jsonArr)
                {
                    list.Add(DeserializeArgument(item, elementType));
                }

                return list;
            }
        }

        // Fall back to POCO deserialization - validate the type first
        ValidateTypeForPocoDeserialization(targetType);
        return JsonSerializer.Deserialize(node.ToJsonString(), targetType, _jsonOptions);
    }

    private object DeserializeReferenceExpression(ReferenceExpressionRef refExpr)
    {
        var format = refExpr.Format;

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

    /// <summary>
    /// Converts a .NET result to its JSON representation.
    /// Returns: null | JsonValue (primitive) | JsonArray (primitive array) | JsonObject (primitive dict or object ref)
    /// </summary>
    private JsonNode? MarshalResult(object? result)
    {
        if (result == null)
        {
            return null;
        }

        var type = result.GetType();

        // Primitives -> JsonValue
        if (ObjectRegistry.IsSimpleType(type))
        {
            // Enums should be serialized as their string names
            if (type.IsEnum)
            {
                return JsonValue.Create(result.ToString());
            }
            return JsonValue.Create(result);
        }

        // byte[] -> base64 string (special case, before general array handling)
        if (type == typeof(byte[]))
        {
            return JsonValue.Create(Convert.ToBase64String((byte[])result));
        }

        // Arrays of primitives -> JsonArray
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            if (ObjectRegistry.IsSimpleType(elementType))
            {
                var jsonArray = new JsonArray();
                foreach (var item in (Array)result)
                {
                    jsonArray.Add(JsonValue.Create(item));
                }
                return jsonArray;
            }
        }

        // IList<T> where T is primitive -> JsonArray
        if (result is System.Collections.IList list && type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length == 1 && ObjectRegistry.IsSimpleType(genericArgs[0]))
            {
                var jsonArray = new JsonArray();
                foreach (var item in list)
                {
                    jsonArray.Add(item == null ? null : JsonValue.Create(item));
                }
                return jsonArray;
            }
        }

        // IDictionary<string, T> where T is primitive -> JsonObject (plain)
        if (result is System.Collections.IDictionary dict && type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length == 2 && genericArgs[0] == typeof(string) && ObjectRegistry.IsSimpleType(genericArgs[1]))
            {
                var jsonObj = new JsonObject();
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    var key = (string)entry.Key;
                    jsonObj[key] = entry.Value == null ? null : JsonValue.Create(entry.Value);
                }
                return jsonObj;
            }
        }

        // Validate type is supported before registering
        ValidateTypeForMarshalling(type);

        // Complex objects -> JsonObject { "$id", "$type" }
        var objectId = _objectRegistry.Register(result);
        var marshalled = MarshalledObject.Create(objectId, type);

        return new JsonObject
        {
            ["$id"] = marshalled.Id,
            ["$type"] = marshalled.Type
        };
    }

    private static bool IsDelegateType(Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type);
    }

    /// <summary>
    /// Awaits async results (Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt;) and returns the unwrapped result.
    /// For void-returning async methods (Task, ValueTask), returns null.
    /// </summary>
    private static object? UnwrapAsyncResult(object? result)
    {
        return result switch
        {
            null => null,
            Task task => WaitForTask(task),
            ValueTask vt => WaitForValueTask(vt),
            _ when IsValueTaskOfT(result, out var asTask) => WaitForTask(asTask!),
            _ => result
        };

        static object? WaitForTask(Task task)
        {
            task.GetAwaiter().GetResult();
            return task.GetType().IsGenericType
                ? task.GetType().GetProperty("Result")!.GetValue(task)
                : null;
        }

        static object? WaitForValueTask(ValueTask vt)
        {
            vt.GetAwaiter().GetResult();
            return null;
        }

        static bool IsValueTaskOfT(object obj, out Task? task)
        {
            var type = obj.GetType();
            if (type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                task = (Task)type.GetMethod("AsTask")!.Invoke(obj, null)!;
                return true;
            }
            task = null;
            return false;
        }
    }

    /// <summary>
    /// Validates that a type can be deserialized as a POCO.
    /// Throws NotSupportedException for types that cannot be instantiated from JSON.
    /// </summary>
    private static void ValidateTypeForPocoDeserialization(Type targetType)
    {
        // Interfaces cannot be instantiated - likely a missing $id reference
        if (targetType.IsInterface)
        {
            throw new NotSupportedException(
                $"Cannot deserialize JSON to interface type '{targetType.Name}'. " +
                $"Did you mean to pass an object reference? Use {{\"$id\": \"obj_N\"}} format.");
        }

        // Abstract classes cannot be instantiated
        if (targetType.IsAbstract)
        {
            throw new NotSupportedException(
                $"Cannot deserialize JSON to abstract type '{targetType.Name}'. " +
                $"Did you mean to pass an object reference? Use {{\"$id\": \"obj_N\"}} format.");
        }

        // Open generic types (e.g., List<> without type argument)
        if (targetType.IsGenericTypeDefinition)
        {
            throw new NotSupportedException(
                $"Cannot deserialize JSON to open generic type '{targetType.Name}'. " +
                $"A concrete type argument is required.");
        }

        // Delegate types should use callback pattern
        if (typeof(Delegate).IsAssignableFrom(targetType))
        {
            throw new NotSupportedException(
                $"Cannot deserialize JSON to delegate type '{targetType.Name}'. " +
                $"Pass a callback ID string instead (e.g., \"callback_123\").");
        }

        // System reflection types
        if (targetType == typeof(Type) || typeof(MemberInfo).IsAssignableFrom(targetType))
        {
            throw new NotSupportedException(
                $"Cannot deserialize JSON to reflection type '{targetType.Name}'.");
        }

        // Check for accessible constructor
        var constructors = targetType.GetConstructors();
        if (constructors.Length == 0 && !targetType.IsValueType)
        {
            throw new NotSupportedException(
                $"Cannot deserialize JSON to type '{targetType.Name}' - no public constructors. " +
                $"Did you mean to pass an object reference? Use {{\"$id\": \"obj_N\"}} format.");
        }
    }

    /// <summary>
    /// Validates that a type can be marshalled over RPC.
    /// Throws NotSupportedException for types that cannot be serialized.
    /// </summary>
    private static void ValidateTypeForMarshalling(Type type)
    {
        // By-ref and pointer types
        if (type.IsByRef || type.IsPointer)
        {
            throw new NotSupportedException($"Cannot marshal by-ref or pointer type: {type.FullName}");
        }

        // Ref structs (Span<T>, ReadOnlySpan<T>, etc.) - cannot be boxed
        if (type.IsByRefLike)
        {
            throw new NotSupportedException($"Cannot marshal ref struct type: {type.FullName}");
        }

        // Memory<T> and ReadOnlyMemory<T>
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(Memory<>) || genericDef == typeof(ReadOnlyMemory<>))
            {
                throw new NotSupportedException($"Cannot marshal Memory<T> type: {type.FullName}. Use byte[] with base64 encoding instead.");
            }
        }

        // IAsyncEnumerable<T> - no streaming support
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                throw new NotSupportedException($"Cannot marshal IAsyncEnumerable<T> type: {type.FullName}. Streaming is not supported.");
            }
        }

        // Task/ValueTask without awaiting - user probably made a mistake
        if (type == typeof(Task) || type == typeof(ValueTask) ||
            (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) ||
                                    type.GetGenericTypeDefinition() == typeof(ValueTask<>))))
        {
            throw new NotSupportedException($"Cannot marshal unawaited Task/ValueTask: {type.FullName}. Await the task before returning.");
        }
    }

    #endregion

    #region Convenience Methods

    /// <summary>
    /// Gets an indexed value using a string key.
    /// Returns: null | JsonValue (primitive) | JsonObject { "$id", "$type" }
    /// </summary>
    public JsonNode? GetIndexerByStringKey(string objectId, string key)
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
    public void SetIndexerByStringKey(string objectId, string key, JsonNode? value)
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

    /// <summary>
    /// Creates a new CancellationToken that the guest can pass to host methods.
    /// Returns: { "$cancellationToken": "ct_N" }
    /// </summary>
    public JsonObject CreateCancellationToken()
    {
        var (tokenId, _) = _callbackProxyFactory.CancellationTokenRegistry.Create();
        return new CancellationTokenRef { TokenId = tokenId }.ToJsonObject();
    }

    /// <summary>
    /// Cancels a CancellationToken by its ID.
    /// Called by the guest to signal cancellation to the host.
    /// </summary>
    /// <param name="tokenId">The token ID (e.g., "ct_1").</param>
    /// <returns>True if the token was found and cancelled, false otherwise.</returns>
    public bool CancelToken(string tokenId)
    {
        return _callbackProxyFactory.CancellationTokenRegistry.Cancel(tokenId);
    }

    public ValueTask DisposeAsync()
    {
        _typeResolver.Clear();
        _objectRegistry.Clear();
        _callbackProxyFactory.Dispose();
        return ValueTask.CompletedTask;
    }
}
