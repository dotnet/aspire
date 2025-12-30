// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Manages registration and lookup of .NET objects for JSON-RPC remoting.
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
    /// Marshals a .NET object to a dictionary representation for JSON-RPC transport.
    /// </summary>
    /// <param name="obj">The object to marshal.</param>
    /// <returns>A dictionary containing the object's ID and type.</returns>
    public Dictionary<string, object?> Marshal(object obj)
    {
        var type = obj.GetType();
        var objectId = Register(obj);

        return new Dictionary<string, object?>
        {
            ["$id"] = objectId,
            ["$type"] = type.FullName
        };
    }

    /// <summary>
    /// Resolves a JSON element that might be a proxy reference (with $id) to the actual .NET object.
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
}
