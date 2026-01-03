// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.RemoteHost.Ats;

/// <summary>
/// Delegate for capability implementations.
/// </summary>
/// <param name="args">The arguments as a JSON object.</param>
/// <param name="handles">The handle registry for resolving/registering handles.</param>
/// <param name="typeHierarchy">The type hierarchy for type validation.</param>
/// <returns>The result as JSON, or null for void operations.</returns>
internal delegate JsonNode? CapabilityHandler(
    JsonObject? args,
    HandleRegistry handles,
    TypeHierarchy typeHierarchy);

/// <summary>
/// Dispatches capability invocations to their implementations.
/// Scans provided assemblies for [AspireExport] attributes.
/// </summary>
internal sealed class CapabilityDispatcher
{
    private readonly ConcurrentDictionary<string, CapabilityRegistration> _capabilities = new();
    private readonly HandleRegistry _handles;
    private readonly TypeHierarchy _typeHierarchy;
    private readonly AtsCallbackProxyFactory? _callbackProxyFactory;

    /// <summary>
    /// Represents a registered capability.
    /// </summary>
    private sealed class CapabilityRegistration
    {
        public required string CapabilityId { get; init; }
        public required CapabilityHandler Handler { get; init; }
        public string? AppliesTo { get; init; }
        public string? Description { get; init; }
    }

    /// <summary>
    /// Creates a new CapabilityDispatcher.
    /// </summary>
    /// <param name="handles">The handle registry for resolving handle references.</param>
    /// <param name="typeHierarchy">The type hierarchy for AppliesTo validation.</param>
    /// <param name="assemblies">The assemblies to scan for [AspireExport] attributes.</param>
    /// <param name="callbackProxyFactory">Optional factory for creating callback proxies.</param>
    public CapabilityDispatcher(
        HandleRegistry handles,
        TypeHierarchy typeHierarchy,
        IEnumerable<Assembly> assemblies,
        AtsCallbackProxyFactory? callbackProxyFactory = null)
    {
        _handles = handles;
        _typeHierarchy = typeHierarchy;
        _callbackProxyFactory = callbackProxyFactory;

        // Scan for capabilities on initialization
        ScanAssemblies(assemblies);
    }

    /// <summary>
    /// Scans the provided assemblies for [AspireExport] and [AspireContextType] attributes.
    /// </summary>
    private void ScanAssemblies(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    // Check for [AspireContextType] - auto-register property accessors
                    var contextAttr = type.GetCustomAttribute<AspireContextTypeAttribute>();
                    if (contextAttr != null)
                    {
                        RegisterContextTypeProperties(type, contextAttr);
                    }

                    // Check for [AspireExport] on static methods
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var attr = method.GetCustomAttribute<AspireExportAttribute>();
                        if (attr != null)
                        {
                            RegisterFromAttribute(attr, method);
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
            }
        }
    }

    /// <summary>
    /// Registers property accessors for a context type.
    /// </summary>
    private void RegisterContextTypeProperties(Type contextType, AspireContextTypeAttribute attr)
    {
        var version = attr.Version;

        foreach (var property in contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Skip properties without getters
            if (!property.CanRead)
            {
                continue;
            }

            // Skip non-ATS-compatible properties
            if (!AtsIntrinsics.IsAtsCompatible(property.PropertyType))
            {
                continue;
            }

            // Generate capability ID: aspire/EnvironmentContext.environmentVariables@1
            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            var capabilityId = $"{attr.Id}.{propertyName}@{version}";

            // Create a handler that gets the property value
            var prop = property; // Capture for closure
            CapabilityHandler handler = (args, handles, hierarchy) =>
            {
                // The context object is passed as the first argument
                if (args == null || !args.TryGetPropertyValue("context", out var contextNode))
                {
                    throw CapabilityException.InvalidArgument(capabilityId, "context", "Missing required argument 'context'");
                }

                var handleRef = HandleRef.FromJsonNode(contextNode);
                if (handleRef == null)
                {
                    throw CapabilityException.InvalidArgument(capabilityId, "context", "Argument 'context' must be a handle reference");
                }

                if (!handles.TryGet(handleRef.HandleId, out var contextObj, out _))
                {
                    throw CapabilityException.HandleNotFound(handleRef.HandleId, capabilityId);
                }

                var value = prop.GetValue(contextObj);
                return AtsMarshaller.MarshalToJson(value, handles);
            };

            _capabilities[capabilityId] = new CapabilityRegistration
            {
                CapabilityId = capabilityId,
                Handler = handler,
                AppliesTo = attr.Id,
                Description = $"Gets the {property.Name} property"
            };
        }
    }

    /// <summary>
    /// Registers an export from its attribute and method.
    /// </summary>
    private void RegisterFromAttribute(AspireExportAttribute attr, MethodInfo method)
    {
        var parameters = method.GetParameters();

        // Check if this is an extension method
        var isExtensionMethod = method.IsDefined(typeof(ExtensionAttribute), false) && parameters.Length > 0;

        // Auto-derive AppliesTo if not explicitly specified
        var appliesTo = attr.AppliesTo ?? DeriveAppliesTo(method, parameters, isExtensionMethod);

        // Create a handler that invokes the method via reflection
        CapabilityHandler handler = (args, handles, hierarchy) =>
        {
            var methodArgs = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramName = param.Name ?? $"arg{i}";

                if (args != null && args.TryGetPropertyValue(paramName, out var argNode))
                {
                    // Check for [AspireCallback] on the parameter
                    var callbackAttr = param.GetCustomAttribute<AspireCallbackAttribute>();
                    var context = new AtsMarshaller.UnmarshalContext
                    {
                        Handles = handles,
                        CallbackProxyFactory = _callbackProxyFactory,
                        CapabilityId = attr.Id,
                        ParameterName = paramName,
                        CallbackId = callbackAttr?.CallbackId
                    };
                    methodArgs[i] = AtsMarshaller.UnmarshalFromJson(argNode, param.ParameterType, context);
                }
                else if (param.HasDefaultValue)
                {
                    methodArgs[i] = param.DefaultValue;
                }
                else
                {
                    throw CapabilityException.InvalidArgument(
                        attr.Id, paramName, $"Missing required argument '{paramName}'");
                }
            }

            var result = method.Invoke(null, methodArgs);

            // Handle async methods
            if (result is Task task)
            {
                task.GetAwaiter().GetResult();
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    var resultProperty = taskType.GetProperty("Result");
                    result = resultProperty?.GetValue(task);
                }
                else
                {
                    result = null;
                }
            }

            return ConvertResult(result, handles);
        };

        _capabilities[attr.Id] = new CapabilityRegistration
        {
            CapabilityId = attr.Id,
            Handler = handler,
            AppliesTo = appliesTo,
            Description = attr.Description
        };
    }

    /// <summary>
    /// Derives the AppliesTo constraint from method signature.
    /// For extension methods, uses the first parameter type.
    /// For generic methods, uses constraints on type parameters.
    /// </summary>
    private static string? DeriveAppliesTo(MethodInfo method, ParameterInfo[] parameters, bool isExtensionMethod)
    {
        // For extension methods on IResourceBuilder<T>, derive from T
        if (isExtensionMethod && parameters.Length > 0)
        {
            var firstParamType = parameters[0].ParameterType;

            // Check for IResourceBuilder<T>
            var resourceType = AtsIntrinsics.GetResourceType(firstParamType);
            if (resourceType != null)
            {
                // For generic methods with constraints like "where T : JavaScriptAppResource",
                // we want to use the constraint type, not the generic parameter
                if (method.IsGenericMethod)
                {
                    var constraintType = GetResourceConstraintFromMethod(method);
                    if (constraintType != null)
                    {
                        return AtsIntrinsics.GetResourceTypeId(constraintType);
                    }
                }

                // For concrete types like IResourceBuilder<RedisResource>
                if (!resourceType.IsGenericParameter)
                {
                    return AtsIntrinsics.GetResourceTypeId(resourceType);
                }

                // For IResourceBuilder<T> without constraints, use base interface
                return "aspire/IResource";
            }

            // Check for other intrinsic types (IDistributedApplicationBuilder, etc.)
            var typeId = AtsIntrinsics.GetTypeId(firstParamType);
            if (typeId != null)
            {
                // Don't set AppliesTo for builder methods - they're entry points
                if (typeId == "aspire/Builder")
                {
                    return null;
                }
                return typeId;
            }
        }

        // For generic methods, check type parameter constraints
        if (method.IsGenericMethod)
        {
            var constraintType = GetResourceConstraintFromMethod(method);
            if (constraintType != null)
            {
                return AtsIntrinsics.GetResourceTypeId(constraintType);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the most specific resource constraint from a generic method's type parameters.
    /// </summary>
    private static Type? GetResourceConstraintFromMethod(MethodInfo method)
    {
        var genericArgs = method.GetGenericArguments();
        foreach (var typeParam in genericArgs)
        {
            var constraints = typeParam.GetGenericParameterConstraints();
            foreach (var constraint in constraints)
            {
                // Look for resource type constraints (e.g., "where T : JavaScriptAppResource")
                if (typeof(IResource).IsAssignableFrom(constraint) && constraint != typeof(IResource))
                {
                    return constraint;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Converts a result to JSON using the shared marshaller.
    /// </summary>
    private static JsonNode? ConvertResult(object? result, HandleRegistry handles)
    {
        return AtsMarshaller.MarshalToJson(result, handles);
    }

    /// <summary>
    /// Registers a capability with its handler.
    /// </summary>
    /// <param name="capabilityId">The capability ID (e.g., "aspire.redis/addRedis@1").</param>
    /// <param name="handler">The handler that implements the capability.</param>
    /// <param name="appliesTo">Optional type constraint for the first handle argument.</param>
    /// <param name="description">Optional description of the capability.</param>
    public void Register(
        string capabilityId,
        CapabilityHandler handler,
        string? appliesTo = null,
        string? description = null)
    {
        _capabilities[capabilityId] = new CapabilityRegistration
        {
            CapabilityId = capabilityId,
            Handler = handler,
            AppliesTo = appliesTo,
            Description = description
        };
    }

    /// <summary>
    /// Invokes a capability by ID with the given arguments.
    /// </summary>
    /// <param name="capabilityId">The capability ID.</param>
    /// <param name="args">The arguments as a JSON object.</param>
    /// <returns>The result as JSON, or null for void methods.</returns>
    public JsonNode? Invoke(string capabilityId, JsonObject? args)
    {
        // Look up the capability
        if (!_capabilities.TryGetValue(capabilityId, out var registration))
        {
            throw CapabilityException.CapabilityNotFound(capabilityId);
        }

        args ??= new JsonObject();

        // Validate AppliesTo constraint if present
        if (!string.IsNullOrEmpty(registration.AppliesTo))
        {
            ValidateAppliesTo(capabilityId, args, registration.AppliesTo);
        }

        try
        {
            return registration.Handler(args, _handles, _typeHierarchy);
        }
        catch (CapabilityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw CapabilityException.InternalError(capabilityId, ex.Message, ex);
        }
    }

    /// <summary>
    /// Validates the AppliesTo constraint for a capability.
    /// </summary>
    private void ValidateAppliesTo(string capabilityId, JsonObject args, string appliesTo)
    {
        // Find the first handle argument
        foreach (var prop in args)
        {
            var handleRef = HandleRef.FromJsonNode(prop.Value);
            if (handleRef != null)
            {
                if (!_handles.TryGet(handleRef.HandleId, out _, out var typeId))
                {
                    throw CapabilityException.HandleNotFound(handleRef.HandleId, capabilityId);
                }

                if (!string.IsNullOrEmpty(typeId) && !_typeHierarchy.IsAssignableTo(typeId, appliesTo))
                {
                    throw CapabilityException.TypeMismatch(capabilityId, prop.Key, appliesTo, typeId);
                }

                // Only validate the first handle
                return;
            }
        }
    }

    /// <summary>
    /// Gets all registered capability IDs.
    /// </summary>
    public IEnumerable<string> GetCapabilityIds() => _capabilities.Keys;

    /// <summary>
    /// Checks if a capability is registered.
    /// </summary>
    public bool HasCapability(string capabilityId) => _capabilities.ContainsKey(capabilityId);
}

/// <summary>
/// Extension methods for working with JSON in capability handlers.
/// </summary>
internal static class CapabilityJsonExtensions
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets a required string argument.
    /// </summary>
    public static string GetRequiredString(this JsonObject args, string name, string capabilityId)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node is not JsonValue value)
        {
            throw CapabilityException.InvalidArgument(capabilityId, name, $"Missing required argument '{name}'");
        }

        return value.GetValue<string>() ??
            throw CapabilityException.InvalidArgument(capabilityId, name, $"Argument '{name}' cannot be null");
    }

    /// <summary>
    /// Gets an optional string argument.
    /// </summary>
    public static string? GetOptionalString(this JsonObject args, string name)
    {
        if (args.TryGetPropertyValue(name, out var node) && node is JsonValue value)
        {
            return value.GetValue<string>();
        }
        return null;
    }

    /// <summary>
    /// Gets an optional int argument.
    /// </summary>
    public static int? GetOptionalInt(this JsonObject args, string name)
    {
        if (args.TryGetPropertyValue(name, out var node) && node is JsonValue value)
        {
            return value.GetValue<int>();
        }
        return null;
    }

    /// <summary>
    /// Gets a required handle reference.
    /// </summary>
    public static T GetRequiredHandle<T>(
        this JsonObject args,
        string name,
        string capabilityId,
        HandleRegistry handles) where T : class
    {
        if (!args.TryGetPropertyValue(name, out var node))
        {
            throw CapabilityException.InvalidArgument(capabilityId, name, $"Missing required argument '{name}'");
        }

        var handleRef = HandleRef.FromJsonNode(node) ??
            throw CapabilityException.InvalidArgument(capabilityId, name, $"Argument '{name}' must be a handle reference");

        if (!handles.TryGet(handleRef.HandleId, out var obj, out _))
        {
            throw CapabilityException.HandleNotFound(handleRef.HandleId, capabilityId);
        }

        if (obj is not T typed)
        {
            throw CapabilityException.TypeMismatch(
                capabilityId, name, typeof(T).Name, obj?.GetType().Name ?? "null");
        }

        return typed;
    }

    /// <summary>
    /// Deserializes a DTO from a JSON argument.
    /// </summary>
    public static T? GetDto<T>(this JsonObject args, string name) where T : class
    {
        if (args.TryGetPropertyValue(name, out var node) && node is JsonObject obj)
        {
            return JsonSerializer.Deserialize<T>(obj.ToJsonString(), s_jsonOptions);
        }
        return null;
    }

    /// <summary>
    /// Creates a handle result for returning from a capability.
    /// </summary>
    public static JsonObject CreateHandleResult(this HandleRegistry handles, object obj, string typeId)
    {
        return handles.Marshal(obj, typeId);
    }
}
