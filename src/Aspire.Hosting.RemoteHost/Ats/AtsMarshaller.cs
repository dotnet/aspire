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
internal sealed class AtsMarshaller
{
    private readonly HandleRegistry _handles;
    private readonly Hosting.Ats.AtsContext _context;
    private readonly CancellationTokenRegistry _cancellationTokenRegistry;
    private readonly Lazy<AtsCallbackProxyFactory> _callbackProxyFactory;

    /// <summary>
    /// Creates a new marshaller instance.
    /// </summary>
    /// <param name="handles">The handle registry for marshalling handles.</param>
    /// <param name="context">The ATS context for type classification.</param>
    /// <param name="cancellationTokenRegistry">The cancellation token registry.</param>
    /// <param name="callbackProxyFactory">Lazy callback proxy factory to break circular dependency.</param>
    public AtsMarshaller(
        HandleRegistry handles,
        Hosting.Ats.AtsContext context,
        CancellationTokenRegistry cancellationTokenRegistry,
        Lazy<AtsCallbackProxyFactory> callbackProxyFactory)
    {
        _handles = handles;
        _context = context;
        _cancellationTokenRegistry = cancellationTokenRegistry;
        _callbackProxyFactory = callbackProxyFactory;
    }

    /// <summary>
    /// Context for unmarshalling operations, providing error context info.
    /// </summary>
    internal sealed class UnmarshalContext
    {
        public string? CapabilityId { get; init; }
        public string? ParameterName { get; init; }
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
        typeof(DateOnly),
        typeof(TimeOnly),
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
    /// Marshals a .NET object to JSON for sending to the guest using type metadata.
    /// Uses the scanner's type classification instead of runtime type inspection.
    /// </summary>
    /// <param name="value">The value to marshal.</param>
    /// <param name="typeRef">The type metadata from the scanner.</param>
    /// <returns>The JSON representation, or null if the value is null.</returns>
    public JsonNode? MarshalToJson(object? value, Hosting.Ats.AtsTypeRef typeRef)
    {
        if (value == null)
        {
            return null;
        }

        // Handle 'any' type - fall back to runtime type inspection
        if (typeRef.TypeId == Hosting.Ats.AtsConstants.Any)
        {
            return MarshalToJson(value);
        }

        return typeRef.Category switch
        {
            Hosting.Ats.AtsTypeCategory.Handle => _handles.Marshal(value, typeRef.TypeId),
            Hosting.Ats.AtsTypeCategory.Primitive => SerializePrimitive(value),
            Hosting.Ats.AtsTypeCategory.Enum => JsonValue.Create(value.ToString()),
            Hosting.Ats.AtsTypeCategory.Dto => SerializeDto(value),
            Hosting.Ats.AtsTypeCategory.Array => SerializeArray(value, typeRef.ElementType),
            Hosting.Ats.AtsTypeCategory.List => _handles.Marshal(value, typeRef.TypeId),
            Hosting.Ats.AtsTypeCategory.Dict => _handles.Marshal(value, typeRef.TypeId),
            _ => throw new InvalidOperationException($"Unknown type category: {typeRef.Category}")
        };
    }

    private static JsonNode? SerializePrimitive(object value)
    {
        var type = value.GetType();

        // TimeSpan is serialized as total milliseconds for easy JS interop
        if (type == typeof(TimeSpan))
        {
            return JsonValue.Create(((TimeSpan)value).TotalMilliseconds);
        }

        // DateOnly is serialized as ISO date string (yyyy-MM-dd)
        if (type == typeof(DateOnly))
        {
            return JsonValue.Create(((DateOnly)value).ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        }

        // TimeOnly is serialized as ISO time string (HH:mm:ss.fffffff)
        if (type == typeof(TimeOnly))
        {
            return JsonValue.Create(((TimeOnly)value).ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        }

        return JsonValue.Create(value);
    }

    private static JsonNode? SerializeDto(object value)
    {
        var json = JsonSerializer.Serialize(value, s_jsonOptions);
        return JsonNode.Parse(json);
    }

    private JsonNode? SerializeArray(object value, Hosting.Ats.AtsTypeRef? elementType)
    {
        var jsonArray = new JsonArray();
        foreach (var item in (IEnumerable)value)
        {
            if (elementType != null)
            {
                jsonArray.Add(MarshalToJson(item, elementType));
            }
            else
            {
                jsonArray.Add(MarshalToJson(item));
            }
        }
        return jsonArray;
    }

    /// <summary>
    /// Marshals a .NET object to JSON for sending to the guest.
    /// Uses runtime type inspection based on scanned AtsContext.
    /// </summary>
    /// <param name="value">The value to marshal.</param>
    /// <returns>The JSON representation, or null if the value is null.</returns>
    public JsonNode? MarshalToJson(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();
        var category = _context.GetCategory(type);

        return category switch
        {
            Hosting.Ats.AtsTypeCategory.Primitive => SerializePrimitive(value),
            Hosting.Ats.AtsTypeCategory.Enum => JsonValue.Create(value.ToString()),
            Hosting.Ats.AtsTypeCategory.Dto => SerializeDto(value),
            Hosting.Ats.AtsTypeCategory.Array => SerializeArrayRuntime(value),
            Hosting.Ats.AtsTypeCategory.List => MarshalListHandle(value, type),
            Hosting.Ats.AtsTypeCategory.Dict => MarshalDictHandle(value, type),
            Hosting.Ats.AtsTypeCategory.Handle => _handles.Marshal(value, Hosting.Ats.AtsTypeMapping.DeriveTypeId(type)),
            _ => _handles.Marshal(value, Hosting.Ats.AtsTypeMapping.DeriveTypeId(type))
        };
    }

    private JsonNode? SerializeArrayRuntime(object value)
    {
        var jsonArray = new JsonArray();
        foreach (var item in (IEnumerable)value)
        {
            jsonArray.Add(MarshalToJson(item));
        }
        return jsonArray;
    }

    private JsonNode? MarshalListHandle(object value, Type type)
    {
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length == 1)
            {
                var typeId = $"Aspire.Hosting/List<{genericArgs[0].Name}>";
                return _handles.Marshal(value, typeId);
            }
        }
        // Fallback for non-generic lists
        return _handles.Marshal(value, Hosting.Ats.AtsTypeMapping.DeriveTypeId(type));
    }

    private JsonNode? MarshalDictHandle(object value, Type type)
    {
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length == 2)
            {
                var typeId = $"Aspire.Hosting/Dict<{genericArgs[0].Name},{genericArgs[1].Name}>";
                return _handles.Marshal(value, typeId);
            }
        }
        // Fallback for non-generic dicts
        return _handles.Marshal(value, Hosting.Ats.AtsTypeMapping.DeriveTypeId(type));
    }

    /// <summary>
    /// Unmarshals a JSON node to a .NET object of the specified type.
    /// Handles: primitives, arrays, lists, dictionaries, [AspireDto] types, and delegate callbacks.
    /// </summary>
    /// <param name="node">The JSON node to unmarshal.</param>
    /// <param name="targetType">The target .NET type.</param>
    /// <param name="context">The unmarshalling context with registries and error info.</param>
    /// <returns>The unmarshalled .NET object.</returns>
    public object? UnmarshalFromJson(JsonNode? node, Type targetType, UnmarshalContext context)
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
            if (!_handles.TryGet(handleRef.HandleId, out var handleObj, out _))
            {
                throw CapabilityException.HandleNotFound(handleRef.HandleId, capabilityId);
            }
            return handleObj;
        }

        // Check for reference expression (similar to handle, but constructs a ReferenceExpression)
        // Format: { "$expr": { "format": "...", "valueProviders": [...] } }
        var exprRef = ReferenceExpressionRef.FromJsonNode(node);
        if (exprRef != null)
        {
            return exprRef.ToReferenceExpression(_handles, capabilityId, paramName);
        }

        // Handle callbacks - any delegate type is treated as a callback
        if (typeof(Delegate).IsAssignableFrom(targetType))
        {
            // Callback ID is passed as a string
            if (node is JsonValue callbackValue && callbackValue.TryGetValue<string>(out var callbackId))
            {
                Delegate? proxy;
                try
                {
                    proxy = _callbackProxyFactory.Value.CreateProxy(callbackId, targetType);
                }
                catch (Exception ex) when (ex is not CapabilityException)
                {
                    throw CapabilityException.InvalidArgument(
                        capabilityId, paramName,
                        $"Callback proxy factory not available: {ex.Message}");
                }
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

        // Handle CancellationToken - token ID is passed as a string, or null/missing for CancellationToken.None
        if (targetType == typeof(CancellationToken))
        {
            // Token ID as string - get or create a token for this ID
            if (node is JsonValue tokenValue && tokenValue.TryGetValue<string>(out var tokenId) && !string.IsNullOrEmpty(tokenId))
            {
                // Get or create a CancellationToken for this guest-provided ID
                // The guest can later cancel this token by calling cancelToken RPC
                return _cancellationTokenRegistry.GetOrCreate(tokenId);
            }
            // null, empty, or not a string means no cancellation
            return CancellationToken.None;
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

        // Handle enums - they come as string names
        if (underlyingType.IsEnum)
        {
            if (value.TryGetValue<string>(out var enumName))
            {
                return Enum.Parse(underlyingType, enumName, ignoreCase: true);
            }
            // Also support numeric enum values
            if (value.TryGetValue<int>(out var enumValue))
            {
                return Enum.ToObject(underlyingType, enumValue);
            }
        }

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

        // DateOnly: accept ISO date string (yyyy-MM-dd)
        if (underlyingType == typeof(DateOnly))
        {
            if (value.TryGetValue<string>(out var str))
            {
                return DateOnly.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        // TimeOnly: accept ISO time string (HH:mm:ss.fffffff)
        if (underlyingType == typeof(TimeOnly))
        {
            if (value.TryGetValue<string>(out var str))
            {
                return TimeOnly.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return JsonSerializer.Deserialize(value.ToJsonString(), targetType, s_jsonOptions);
    }
}
