// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Manages registration and lookup of .NET objects for marshalling to/from TypeScript.
/// Objects are registered with unique IDs and can be retrieved by ID for method invocations.
/// </summary>
internal sealed class ObjectRegistry
{
    private readonly ConcurrentDictionary<string, object> _objects = new();
    private long _idCounter;

    /// <summary>
    /// Registers an object and returns its unique ID.
    /// </summary>
    /// <param name="obj">The object to register.</param>
    /// <returns>The unique ID assigned to the object.</returns>
    public string Register(object obj)
    {
        var id = $"obj_{Interlocked.Increment(ref _idCounter)}";
        _objects[id] = obj;
        return id;
    }

    /// <summary>
    /// Tries to get an object by its ID.
    /// </summary>
    /// <param name="objectId">The object ID.</param>
    /// <param name="obj">The object if found.</param>
    /// <returns>True if the object was found, false otherwise.</returns>
    public bool TryGet(string objectId, out object? obj)
    {
        return _objects.TryGetValue(objectId, out obj);
    }

    /// <summary>
    /// Gets an object by its ID.
    /// </summary>
    /// <param name="objectId">The object ID.</param>
    /// <returns>The registered object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the object is not found.</exception>
    public object Get(string objectId)
    {
        if (!_objects.TryGetValue(objectId, out var obj))
        {
            throw new InvalidOperationException($"Object '{objectId}' not found in registry");
        }
        return obj;
    }

    /// <summary>
    /// Unregisters an object from the registry.
    /// </summary>
    /// <param name="objectId">The object ID to unregister.</param>
    /// <returns>True if the object was removed, false if it wasn't found.</returns>
    public bool Unregister(string objectId)
    {
        return _objects.TryRemove(objectId, out _);
    }

    /// <summary>
    /// Clears all registered objects.
    /// </summary>
    public void Clear()
    {
        _objects.Clear();
    }

    /// <summary>
    /// Gets the count of registered objects.
    /// </summary>
    public int Count => _objects.Count;

    /// <summary>
    /// Checks if a type is a simple/primitive type that can be serialized directly.
    /// </summary>
    public static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               type.IsEnum ||
               (Nullable.GetUnderlyingType(type) is { } underlying && IsSimpleType(underlying));
    }

    /// <summary>
    /// Marshals a .NET object to a dictionary representation that can be sent to TypeScript.
    /// </summary>
    /// <param name="obj">The object to marshal.</param>
    /// <returns>A dictionary containing the object's ID, type info, and simple property values.</returns>
    public Dictionary<string, object?> Marshal(object obj)
    {
        var type = obj.GetType();
        var objectId = Register(obj);

        var result = new Dictionary<string, object?>
        {
            ["$id"] = objectId,
            ["$type"] = type.Name,
            ["$fullType"] = type.FullName
        };

        // Include simple property values directly
        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            try
            {
                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var propType = prop.PropertyType;

                // Skip problematic types
                if (propType.FullName?.StartsWith("System.Reflection") == true ||
                    typeof(Delegate).IsAssignableFrom(propType))
                {
                    continue;
                }

                var value = prop.GetValue(obj);

                if (value == null || IsSimpleType(propType))
                {
                    result[prop.Name] = value;
                }
                else
                {
                    // For complex nested objects, just include type info - they can be fetched via getProperty
                    result[prop.Name + "$type"] = value.GetType().Name;
                }
            }
            catch
            {
                // Ignore properties that can't be read
            }
        }

        // Include available methods
        var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => !m.IsSpecialName) // Exclude property getters/setters
            .Where(m => m.DeclaringType != typeof(object)) // Exclude Object methods
            .Select(m => new
            {
                name = m.Name,
                parameters = m.GetParameters().Select(p => new { name = p.Name, type = p.ParameterType.Name }).ToArray()
            })
            .GroupBy(m => m.name)
            .Select(g => g.First()) // Just include one overload for now
            .ToArray();

        result["$methods"] = methods;

        return result;
    }

    /// <summary>
    /// Resolves a JsonElement value that might be a proxy reference (with $id) to the actual .NET object.
    /// </summary>
    /// <param name="element">The JSON element to resolve.</param>
    /// <returns>The resolved value (either the referenced object or the primitive value).</returns>
    public object? ResolveValue(JsonElement element)
    {
        // Check if it's a proxy reference (object with $id)
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("$id", out var idProp))
        {
            var refId = idProp.GetString();
            if (!string.IsNullOrEmpty(refId) && TryGet(refId, out var refObj))
            {
                return refObj;
            }
        }

        // Handle primitives
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText() // fallback
        };
    }

    /// <summary>
    /// Resolves a value that might be a proxy reference (with $id) to the actual .NET object.
    /// </summary>
    /// <param name="value">The value to resolve.</param>
    /// <returns>The resolved value.</returns>
    public object? ResolveValueObject(object? value)
    {
        if (value == null)
        {
            return null;
        }

        // Check if it's a dictionary with $id (a proxy reference)
        if (value is System.Collections.IDictionary dict && dict.Contains("$id"))
        {
            var refId = dict["$id"]?.ToString();
            if (!string.IsNullOrEmpty(refId) && TryGet(refId, out var refObj))
            {
                return refObj;
            }
        }

        // Check if it's a JsonElement
        if (value is JsonElement jsonElement)
        {
            return ResolveValue(jsonElement);
        }

        return value;
    }
}
