// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.RemoteHost.Ats;

/// <summary>
/// Shared marshalling logic for converting between .NET objects and JSON in the ATS type system.
/// </summary>
internal static class AtsMarshaller
{
    /// <summary>
    /// Context for unmarshalling operations, providing access to registries and factories.
    /// </summary>
    internal sealed class UnmarshalContext
    {
        public required HandleRegistry Handles { get; init; }
        public AtsCallbackProxyFactory? CallbackProxyFactory { get; init; }
        public string? CapabilityId { get; init; }
        public string? ParameterName { get; init; }
        /// <summary>
        /// Optional callback ID from [AspireCallback] on the parameter.
        /// Used when the delegate type doesn't have the attribute.
        /// </summary>
        public string? CallbackId { get; init; }
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Simple types that can be serialized directly as JSON primitives.
    /// </summary>
    private static readonly HashSet<Type> s_simpleTypes =
    [
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(float),
        typeof(decimal),
        typeof(byte),
        typeof(short),
        typeof(uint),
        typeof(ulong),
        typeof(ushort),
        typeof(sbyte),
        typeof(char),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Uri)
    ];

    /// <summary>
    /// Gets the shared JSON serializer options.
    /// </summary>
    public static JsonSerializerOptions JsonOptions => s_jsonOptions;

    /// <summary>
    /// Checks if a type is a simple/primitive type that can be serialized directly.
    /// </summary>
    public static bool IsSimpleType(Type type)
    {
        if (s_simpleTypes.Contains(type))
        {
            return true;
        }

        // Enums are serialized as strings
        if (type.IsEnum)
        {
            return true;
        }

        // Nullable<T> where T is simple
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return IsSimpleType(underlying);
        }

        return false;
    }

    /// <summary>
    /// Marshals a .NET object to JSON for sending to the guest.
    /// Handles: intrinsic Aspire types, primitives, arrays, lists, dictionaries, [AspireHandle] types, [AspireDto] types.
    /// </summary>
    /// <param name="value">The value to marshal.</param>
    /// <param name="handles">The handle registry for marshalling handles.</param>
    /// <returns>The JSON representation, or null if the value is null.</returns>
    public static JsonNode? MarshalToJson(object? value, HandleRegistry handles)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check for intrinsic Aspire types first (IDistributedApplicationBuilder, IResourceBuilder<T>, etc.)
        var intrinsicTypeId = AtsIntrinsics.GetTypeId(type);
        if (intrinsicTypeId != null)
        {
            return handles.Marshal(value, intrinsicTypeId);
        }

        // Check if it's a handle type (marked with [AspireHandle])
        var handleAttr = type.GetCustomAttribute<AspireHandleAttribute>();
        if (handleAttr != null)
        {
            // Get the inner object (assume there's an Inner property)
            var innerProp = type.GetProperty("Inner", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var innerObj = innerProp?.GetValue(value) ?? value;
            return handles.Marshal(innerObj, handleAttr.HandleTypeId);
        }

        // Primitives - serialize directly
        if (IsSimpleType(type))
        {
            // Enums should be serialized as their string names
            if (type.IsEnum)
            {
                return JsonValue.Create(value.ToString());
            }

            // TimeSpan is serialized as total milliseconds for easy JS interop
            if (type == typeof(TimeSpan))
            {
                return JsonValue.Create(((TimeSpan)value).TotalMilliseconds);
            }

            return JsonValue.Create(value);
        }

        // Arrays - marshal each element recursively
        if (type.IsArray)
        {
            var jsonArray = new JsonArray();
            foreach (var item in (Array)value)
            {
                jsonArray.Add(MarshalToJson(item, handles));
            }
            return jsonArray;
        }

        // IList<T> (including List<T>) - marshal as a handle to support mutation
        if (value is IList && type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length == 1)
            {
                // Marshal as a handle so list operations can mutate it
                var elementTypeName = genericArgs[0].Name;
                return handles.Marshal(value, $"aspire/List<{elementTypeName}>");
            }
        }

        // IDictionary<string, T> - marshal as a handle to support mutation
        if (value is IDictionary && type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length == 2 && genericArgs[0] == typeof(string))
            {
                // Marshal as a handle so dictionary operations can mutate it
                var valueTypeName = genericArgs[1].Name;
                return handles.Marshal(value, $"aspire/Dictionary<{valueTypeName}>");
            }
        }

        // DTOs - must have [AspireDto] attribute to be serialized as JSON
        var dtoAttr = type.GetCustomAttribute<AspireDtoAttribute>();
        if (dtoAttr != null)
        {
            var json = JsonSerializer.Serialize(value, s_jsonOptions);
            return JsonNode.Parse(json);
        }

        // Non-DTO complex objects are marshaled as handles
        // This ensures only explicitly marked types cross the serialization boundary
        var typeId = $"aspire.core/{type.Name}";
        return handles.Marshal(value, typeId);
    }

    /// <summary>
    /// Unmarshals a JSON node to a .NET object of the specified type.
    /// Handles: primitives, arrays, lists, dictionaries, [AspireHandle] types, [AspireDto] types, [AspireCallback] delegates.
    /// </summary>
    /// <param name="node">The JSON node to unmarshal.</param>
    /// <param name="targetType">The target .NET type.</param>
    /// <param name="context">The unmarshalling context with registries and error info.</param>
    /// <returns>The unmarshalled .NET object.</returns>
    public static object? UnmarshalFromJson(JsonNode? node, Type targetType, UnmarshalContext context)
    {
        if (node == null)
        {
            return null;
        }

        var capabilityId = context.CapabilityId ?? "unknown";
        var paramName = context.ParameterName ?? "unknown";

        // Check for handle reference
        var handleRef = HandleRef.FromJsonNode(node);
        if (handleRef != null)
        {
            if (!context.Handles.TryGet(handleRef.HandleId, out var handleObj, out _))
            {
                throw CapabilityException.HandleNotFound(handleRef.HandleId, capabilityId);
            }
            return handleObj;
        }

        // Handle callbacks - delegate types with [AspireCallback] attribute (on type or parameter)
        if (typeof(Delegate).IsAssignableFrom(targetType))
        {
            var callbackAttr = targetType.GetCustomAttribute<AspireCallbackAttribute>();
            // Check if callback is marked (either on delegate type or via context from parameter)
            if (callbackAttr != null || context.CallbackId != null)
            {
                // Callback ID is passed as a string
                if (node is JsonValue callbackValue && callbackValue.TryGetValue<string>(out var callbackId))
                {
                    if (context.CallbackProxyFactory == null)
                    {
                        throw CapabilityException.InvalidArgument(
                            capabilityId, paramName,
                            "Callbacks are not supported (no callback proxy factory configured)");
                    }

                    var proxy = context.CallbackProxyFactory.CreateProxy(callbackId, targetType);
                    if (proxy == null)
                    {
                        throw CapabilityException.InvalidArgument(
                            capabilityId, paramName,
                            $"Failed to create callback proxy for type '{targetType.Name}'");
                    }
                    return proxy;
                }
                else
                {
                    throw CapabilityException.InvalidArgument(
                        capabilityId, paramName,
                        "Callback parameter must be a string callback ID");
                }
            }
        }

        // Handle primitives
        if (node is JsonValue value)
        {
            return ConvertPrimitive(value, targetType);
        }

        // Handle JsonArray -> Array or List<T>
        if (node is JsonArray array)
        {
            // T[] arrays
            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType()!;
                var converted = Array.CreateInstance(elementType, array.Count);
                for (int i = 0; i < array.Count; i++)
                {
                    var elementContext = new UnmarshalContext
                    {
                        Handles = context.Handles,
                        CallbackProxyFactory = context.CallbackProxyFactory,
                        CapabilityId = context.CapabilityId,
                        ParameterName = $"{paramName}[{i}]"
                    };
                    converted.SetValue(UnmarshalFromJson(array[i], elementType, elementContext), i);
                }
                return converted;
            }

            // List<T> or IList<T>
            if (targetType.IsGenericType)
            {
                var genericDef = targetType.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || genericDef == typeof(IList<>) || genericDef == typeof(IEnumerable<>) || genericDef == typeof(ICollection<>) || genericDef == typeof(IReadOnlyList<>) || genericDef == typeof(IReadOnlyCollection<>))
                {
                    var elementType = targetType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = (IList)Activator.CreateInstance(listType)!;
                    for (int i = 0; i < array.Count; i++)
                    {
                        var elementContext = new UnmarshalContext
                        {
                            Handles = context.Handles,
                            CallbackProxyFactory = context.CallbackProxyFactory,
                            CapabilityId = context.CapabilityId,
                            ParameterName = $"{paramName}[{i}]"
                        };
                        list.Add(UnmarshalFromJson(array[i], elementType, elementContext));
                    }
                    return list;
                }
            }
        }

        // Handle JsonObject -> Dictionary<string, T> or DTO
        if (node is JsonObject jsonObj)
        {
            // Dictionary<string, T> or IDictionary<string, T>
            if (targetType.IsGenericType)
            {
                var genericDef = targetType.GetGenericTypeDefinition();
                if (genericDef == typeof(Dictionary<,>) || genericDef == typeof(IDictionary<,>) || genericDef == typeof(IReadOnlyDictionary<,>))
                {
                    var genericArgs = targetType.GetGenericArguments();
                    if (genericArgs[0] == typeof(string))
                    {
                        var valueType = genericArgs[1];
                        var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
                        var dict = (IDictionary)Activator.CreateInstance(dictType)!;
                        foreach (var prop in jsonObj)
                        {
                            var valueContext = new UnmarshalContext
                            {
                                Handles = context.Handles,
                                CallbackProxyFactory = context.CallbackProxyFactory,
                                CapabilityId = context.CapabilityId,
                                ParameterName = $"{paramName}[{prop.Key}]"
                            };
                            dict[prop.Key] = UnmarshalFromJson(prop.Value, valueType, valueContext);
                        }
                        return dict;
                    }
                }
            }

            // DTOs - must have [AspireDto] attribute
            var dtoAttr = targetType.GetCustomAttribute<AspireDtoAttribute>();
            if (dtoAttr == null)
            {
                throw CapabilityException.InvalidArgument(
                    capabilityId, paramName,
                    $"Parameter type '{targetType.Name}' must have [AspireDto] attribute to be deserialized from JSON");
            }
            return JsonSerializer.Deserialize(jsonObj.ToJsonString(), targetType, s_jsonOptions);
        }

        return null;
    }

    /// <summary>
    /// Converts a JSON primitive to the target .NET type.
    /// </summary>
    public static object? ConvertPrimitive(JsonValue value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string))
        {
            return value.GetValue<string>();
        }

        if (underlyingType == typeof(bool))
        {
            return value.GetValue<bool>();
        }

        if (underlyingType == typeof(int))
        {
            return value.GetValue<int>();
        }

        if (underlyingType == typeof(long))
        {
            return value.GetValue<long>();
        }

        if (underlyingType == typeof(double))
        {
            return value.GetValue<double>();
        }

        if (underlyingType == typeof(float))
        {
            return (float)value.GetValue<double>();
        }

        if (underlyingType == typeof(decimal))
        {
            return value.GetValue<decimal>();
        }

        // TimeSpan: accept milliseconds (number) or parseable string
        if (underlyingType == typeof(TimeSpan))
        {
            if (value.TryGetValue<double>(out var ms))
            {
                return TimeSpan.FromMilliseconds(ms);
            }
            if (value.TryGetValue<string>(out var str))
            {
                // Support standard TimeSpan format (e.g., "01:30:00") or ISO 8601 duration
                return TimeSpan.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return JsonSerializer.Deserialize(value.ToJsonString(), targetType, s_jsonOptions);
    }
}
