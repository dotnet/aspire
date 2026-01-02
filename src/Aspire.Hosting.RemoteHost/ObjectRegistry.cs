// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Manages registration and lookup of .NET objects for JSON-RPC remoting.
/// Objects are registered with unique IDs and can be retrieved by ID for method invocations.
/// Disposable objects are disposed when the registry is cleared or disposed.
/// </summary>
internal sealed class ObjectRegistry : IAsyncDisposable
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
    /// Disposes all disposable objects in the registry and clears it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // Take a snapshot of all objects and clear the registry
        var objects = _objects.Values.ToList();
        _objects.Clear();

        Console.WriteLine($"[RPC] ObjectRegistry disposing {objects.Count} objects...");

        // Dispose all disposable objects
        foreach (var obj in objects)
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                Console.WriteLine($"[RPC]   Disposing (async): {obj.GetType().Name}");
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (obj is IDisposable disposable)
            {
                Console.WriteLine($"[RPC]   Disposing: {obj.GetType().Name}");
                disposable.Dispose();
            }
        }

        Console.WriteLine("[RPC] ObjectRegistry disposed.");
    }

    /// <summary>
    /// Gets the count of registered objects.
    /// </summary>
    public int Count => _objects.Count;

    /// <summary>
    /// Explicit allowlist of simple types that serialize directly to JSON primitives.
    /// These types do NOT get registered in the ObjectRegistry.
    /// </summary>
    private static readonly HashSet<Type> s_simpleTypes = new()
    {
        // Strings
        typeof(string),
        typeof(char),

        // Boolean
        typeof(bool),

        // Integers
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),

        // Floating point
        typeof(float),
        typeof(double),
        typeof(decimal),

        // Date/Time
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(DateOnly),
        typeof(TimeOnly),

        // Identifiers
        typeof(Guid),
        typeof(Uri),
    };

    /// <summary>
    /// Checks if a type is a simple/primitive type that can be serialized directly.
    /// Only types in the explicit allowlist are considered simple.
    /// </summary>
    public static bool IsSimpleType(Type type)
    {
        if (s_simpleTypes.Contains(type))
        {
            return true;
        }

        // Enums serialize as their string names
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
    /// Marshals a .NET object to a JsonObject for JSON-RPC transport.
    /// Returns: JsonObject { "$id", "$type" }
    /// </summary>
    /// <param name="obj">The object to marshal.</param>
    /// <returns>A JsonObject containing the object's ID and type.</returns>
    public JsonObject Marshal(object obj)
    {
        var type = obj.GetType();
        var objectId = Register(obj);
        var marshalled = MarshalledObject.Create(objectId, type);

        return new JsonObject
        {
            ["$id"] = marshalled.Id,
            ["$type"] = marshalled.Type
        };
    }

    /// <summary>
    /// Resolves a JSON node that might be a proxy reference (with $id) to the actual .NET object.
    /// </summary>
    /// <param name="node">The JSON node to resolve.</param>
    /// <returns>The resolved value (either the referenced object or the primitive value).</returns>
    public object? ResolveValue(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }

        // Check if it's a proxy reference (object with $id)
        var objectRef = ObjectRef.FromJsonNode(node);
        if (objectRef != null && TryGet(objectRef.Id, out var refObj))
        {
            return refObj;
        }

        // Handle primitives via JsonValue
        if (node is JsonValue)
        {
            return JsonPrimitives.GetValue(node);
        }

        // For arrays/objects without $id, return the node itself
        return node;
    }
}
