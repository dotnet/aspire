// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.Ats;

/// <summary>
/// Wraps <see cref="RoType"/> to implement <see cref="IAtsTypeInfo"/>.
/// </summary>
internal sealed class RoTypeInfoWrapper : IAtsTypeInfo
{
    private readonly RoType _type;

    public RoTypeInfoWrapper(RoType type)
    {
        _type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>Gets the underlying RoType.</summary>
    public RoType UnderlyingType => _type;

    public string FullName => _type.FullName;
    public string Name => _type.Name;
    public bool IsInterface => _type.IsInterface;
    public bool IsGenericType => _type.IsGenericType;
    public bool IsEnum => _type.IsEnum;
    public bool IsGenericParameter => _type.IsGenericParameter;
    public bool IsSealed => _type.IsSealed;
    public bool IsNested => _type.IsNested;
    public bool IsArray => _type.IsArray;
    public string? BaseTypeFullName => _type.BaseType?.FullName;
    public string? GenericTypeDefinitionFullName => _type.GenericTypeDefinition?.FullName;

    public IEnumerable<string> GetInterfaceFullNames()
    {
        foreach (var iface in _type.Interfaces)
        {
            yield return iface.FullName;
        }
    }

    public IEnumerable<IAtsTypeInfo> GetInterfaces()
    {
        // Collect all interfaces including from base types
        var allInterfaces = new HashSet<RoType>();
        CollectAllInterfacesRecursive(_type, allInterfaces);

        foreach (var iface in allInterfaces)
        {
            yield return new RoTypeInfoWrapper(iface);
        }
    }

    private static void CollectAllInterfacesRecursive(RoType type, HashSet<RoType> collected)
    {
        // Add directly implemented interfaces
        foreach (var iface in type.Interfaces)
        {
            if (collected.Add(iface))
            {
                // Also collect interfaces that this interface extends
                CollectAllInterfacesRecursive(iface, collected);
            }
        }

        // Also check base type
        if (type.BaseType != null && type.BaseType.FullName != "System.Object")
        {
            CollectAllInterfacesRecursive(type.BaseType, collected);
        }
    }

    public IEnumerable<string> GetGenericArgumentFullNames()
    {
        foreach (var arg in _type.GetGenericArguments())
        {
            yield return arg.FullName;
        }
    }

    public IEnumerable<IAtsTypeInfo> GetGenericArguments()
    {
        foreach (var arg in _type.GetGenericArguments())
        {
            yield return new RoTypeInfoWrapper(arg);
        }
    }

    public string? GetElementTypeFullName()
    {
        return _type.GetElementType()?.FullName;
    }

    public IEnumerable<string> GetGenericParameterConstraintFullNames()
    {
        if (!_type.IsGenericParameter)
        {
            return [];
        }

        try
        {
            return _type.GetGenericParameterConstraints().Select(c => c.FullName);
        }
        catch (InvalidOperationException)
        {
            // Not a generic parameter
            return [];
        }
    }

    public IEnumerable<IAtsAttributeInfo> GetCustomAttributes()
    {
        IEnumerable<RoCustomAttributeData> attrs;
        try
        {
            attrs = _type.GetCustomAttributes();
        }
        catch
        {
            return [];
        }

        return attrs.Select(attr => (IAtsAttributeInfo)new RoAttributeInfoWrapper(attr));
    }

    public IEnumerable<IAtsMethodInfo> GetMethods()
    {
        IReadOnlyList<RoMethod> methods;
        try
        {
            methods = _type.Methods;
        }
        catch (ArgumentException)
        {
            return [];
        }

        return methods.Select(method => (IAtsMethodInfo)new RoMethodInfoWrapper(method));
    }

    public IEnumerable<IAtsPropertyInfo> GetProperties()
    {
        return _type.Properties.Select(prop => (IAtsPropertyInfo)new RoPropertyInfoWrapper(prop));
    }

    public IEnumerable<string> GetEnumNames()
    {
        return _type.GetEnumNames();
    }
}

/// <summary>
/// Wraps <see cref="RoMethod"/> to implement <see cref="IAtsMethodInfo"/>.
/// </summary>
internal sealed class RoMethodInfoWrapper : IAtsMethodInfo
{
    private readonly RoMethod _method;

    public RoMethodInfoWrapper(RoMethod method)
    {
        _method = method ?? throw new ArgumentNullException(nameof(method));
    }

    /// <summary>Gets the underlying RoMethod.</summary>
    public RoMethod UnderlyingMethod => _method;

    public string Name => _method.Name;
    public bool IsStatic => _method.IsStatic;
    public bool IsPublic => _method.IsPublic;
    public string ReturnTypeFullName => _method.ReturnType.FullName;
    public IAtsTypeInfo ReturnType => new RoTypeInfoWrapper(_method.ReturnType);

    public IEnumerable<IAtsParameterInfo> GetParameters()
    {
        foreach (var param in _method.Parameters)
        {
            yield return new RoParameterInfoWrapper(param);
        }
    }

    public IEnumerable<string> GetGenericArgumentFullNames()
    {
        foreach (var arg in _method.GetGenericArguments())
        {
            yield return arg.FullName;
        }
    }

    public IEnumerable<IAtsAttributeInfo> GetCustomAttributes()
    {
        foreach (var attr in _method.GetCustomAttributes())
        {
            yield return new RoAttributeInfoWrapper(attr);
        }
    }

    public IEnumerable<IEnumerable<string>> GetGenericParameterConstraints()
    {
        var results = new List<IEnumerable<string>>();
        foreach (var typeParam in _method.GetGenericArguments())
        {
            if (typeParam.IsGenericParameter)
            {
                try
                {
                    results.Add(typeParam.GetGenericParameterConstraints().Select(c => c.FullName));
                }
                catch (InvalidOperationException)
                {
                    results.Add([]);
                }
            }
            else
            {
                results.Add([]);
            }
        }
        return results;
    }
}

/// <summary>
/// Wraps <see cref="RoParameterInfo"/> to implement <see cref="IAtsParameterInfo"/>.
/// </summary>
internal sealed class RoParameterInfoWrapper : IAtsParameterInfo
{
    private readonly RoParameterInfo _param;

    public RoParameterInfoWrapper(RoParameterInfo param)
    {
        _param = param ?? throw new ArgumentNullException(nameof(param));
    }

    /// <summary>Gets the underlying RoParameterInfo.</summary>
    public RoParameterInfo UnderlyingParameter => _param;

    public string Name => string.IsNullOrEmpty(_param.Name) ? "arg" : _param.Name;
    public string TypeFullName => _param.ParameterType.FullName;
    public IAtsTypeInfo ParameterType => new RoTypeInfoWrapper(_param.ParameterType);
    public bool IsOptional => _param.IsOptional;
    public object? DefaultValue => _param.RawDefaultValue;

    public IEnumerable<IAtsAttributeInfo> GetCustomAttributes()
    {
        foreach (var attr in _param.GetCustomAttributes())
        {
            yield return new RoAttributeInfoWrapper(attr);
        }
    }
}

/// <summary>
/// Wraps <see cref="RoPropertyInfo"/> to implement <see cref="IAtsPropertyInfo"/>.
/// </summary>
internal sealed class RoPropertyInfoWrapper : IAtsPropertyInfo
{
    private readonly RoPropertyInfo _prop;

    public RoPropertyInfoWrapper(RoPropertyInfo prop)
    {
        _prop = prop ?? throw new ArgumentNullException(nameof(prop));
    }

    /// <summary>Gets the underlying RoPropertyInfo.</summary>
    public RoPropertyInfo UnderlyingProperty => _prop;

    public string Name => _prop.Name;
    public string PropertyTypeFullName => _prop.PropertyType.FullName;
    public IAtsTypeInfo PropertyType => new RoTypeInfoWrapper(_prop.PropertyType);
    public bool CanRead => _prop.CanRead;
    public bool CanWrite => _prop.CanWrite;
    public bool IsStatic => _prop.IsStatic;
}

/// <summary>
/// Wraps <see cref="RoCustomAttributeData"/> to implement <see cref="IAtsAttributeInfo"/>.
/// </summary>
internal sealed class RoAttributeInfoWrapper : IAtsAttributeInfo
{
    private readonly RoCustomAttributeData _attr;
    private IReadOnlyDictionary<string, object?>? _namedArgs;

    public RoAttributeInfoWrapper(RoCustomAttributeData attr)
    {
        _attr = attr ?? throw new ArgumentNullException(nameof(attr));
    }

    public string AttributeTypeFullName => _attr.AttributeType.FullName;

    public IReadOnlyList<object?> FixedArguments => _attr.FixedArguments;

    public IReadOnlyDictionary<string, object?> NamedArguments =>
        _namedArgs ??= _attr.NamedArguments.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
}

/// <summary>
/// Wraps <see cref="RoAssembly"/> to implement <see cref="IAtsAssemblyInfo"/>.
/// </summary>
internal sealed class RoAssemblyInfoWrapper : IAtsAssemblyInfo
{
    private readonly RoAssembly _assembly;

    public RoAssemblyInfoWrapper(RoAssembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    /// <summary>Gets the underlying RoAssembly.</summary>
    public RoAssembly UnderlyingAssembly => _assembly;

    /// <summary>Gets the simple name of the assembly.</summary>
    public string Name => _assembly.Name;

    public IEnumerable<IAtsTypeInfo> GetTypes()
    {
        foreach (var type in _assembly.GetTypeDefinitions())
        {
            yield return new RoTypeInfoWrapper(type);
        }
    }

    public IEnumerable<IAtsAttributeInfo> GetCustomAttributes()
    {
        foreach (var attr in _assembly.GetCustomAttributes())
        {
            yield return new RoAttributeInfoWrapper(attr);
        }
    }
}
