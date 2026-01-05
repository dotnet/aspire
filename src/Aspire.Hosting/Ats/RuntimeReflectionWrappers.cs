// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Wraps <see cref="System.Type"/> to implement <see cref="IAtsTypeInfo"/>.
/// </summary>
internal sealed class RuntimeTypeInfo : IAtsTypeInfo
{
    private readonly Type _type;

    public RuntimeTypeInfo(Type type)
    {
        _type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>Gets the underlying CLR type.</summary>
    public Type UnderlyingType => _type;

    public string FullName => _type.FullName ?? _type.Name;
    public string Name => _type.Name;
    public bool IsInterface => _type.IsInterface;
    public bool IsGenericType => _type.IsGenericType;
    public bool IsEnum => _type.IsEnum;
    public bool IsGenericParameter => _type.IsGenericParameter;
    public bool IsSealed => _type.IsSealed;
    public bool IsNested => _type.IsNested;
    public bool IsArray => _type.IsArray;
    public string? BaseTypeFullName => _type.BaseType?.FullName;
    public string? GenericTypeDefinitionFullName => _type.IsGenericType ? _type.GetGenericTypeDefinition().FullName : null;

    public IEnumerable<string> GetInterfaceFullNames()
    {
        foreach (var iface in _type.GetInterfaces())
        {
            if (iface.FullName != null)
            {
                yield return iface.FullName;
            }
        }
    }

    public IEnumerable<IAtsTypeInfo> GetInterfaces()
    {
        foreach (var iface in _type.GetInterfaces())
        {
            yield return new RuntimeTypeInfo(iface);
        }
    }

    public IEnumerable<string> GetGenericArgumentFullNames()
    {
        if (!_type.IsGenericType)
        {
            yield break;
        }

        foreach (var arg in _type.GetGenericArguments())
        {
            if (arg.FullName != null)
            {
                yield return arg.FullName;
            }
        }
    }

    public IEnumerable<IAtsTypeInfo> GetGenericArguments()
    {
        if (!_type.IsGenericType)
        {
            yield break;
        }

        foreach (var arg in _type.GetGenericArguments())
        {
            yield return new RuntimeTypeInfo(arg);
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
            yield break;
        }

        foreach (var constraint in _type.GetGenericParameterConstraints())
        {
            if (constraint.FullName != null)
            {
                yield return constraint.FullName;
            }
        }
    }

    public IEnumerable<IAtsAttributeInfo> GetCustomAttributes()
    {
        foreach (var attr in _type.GetCustomAttributesData())
        {
            yield return new RuntimeAttributeInfo(attr);
        }
    }

    public IEnumerable<IAtsMethodInfo> GetMethods()
    {
        foreach (var method in _type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
        {
            yield return new RuntimeMethodInfo(method);
        }
    }

    public IEnumerable<IAtsPropertyInfo> GetProperties()
    {
        foreach (var prop in _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            yield return new RuntimePropertyInfo(prop);
        }
    }

    public IEnumerable<string> GetEnumNames()
    {
        return _type.IsEnum ? Enum.GetNames(_type) : [];
    }
}

/// <summary>
/// Wraps <see cref="System.Reflection.MethodInfo"/> to implement <see cref="IAtsMethodInfo"/>.
/// </summary>
internal sealed class RuntimeMethodInfo : IAtsMethodInfo
{
    private readonly MethodInfo _method;

    public RuntimeMethodInfo(MethodInfo method)
    {
        _method = method ?? throw new ArgumentNullException(nameof(method));
    }

    /// <summary>Gets the underlying CLR method.</summary>
    public MethodInfo UnderlyingMethod => _method;

    public string Name => _method.Name;
    public bool IsStatic => _method.IsStatic;
    public bool IsPublic => _method.IsPublic;
    public string ReturnTypeFullName => _method.ReturnType.FullName ?? _method.ReturnType.Name;
    public IAtsTypeInfo ReturnType => new RuntimeTypeInfo(_method.ReturnType);

    public IEnumerable<IAtsParameterInfo> GetParameters()
    {
        foreach (var param in _method.GetParameters())
        {
            yield return new RuntimeParameterInfo(param);
        }
    }

    public IEnumerable<string> GetGenericArgumentFullNames()
    {
        if (!_method.IsGenericMethod)
        {
            yield break;
        }

        foreach (var arg in _method.GetGenericArguments())
        {
            if (arg.FullName != null)
            {
                yield return arg.FullName;
            }
            else
            {
                // For type parameters like T, use the name
                yield return arg.Name;
            }
        }
    }

    public IEnumerable<IAtsAttributeInfo> GetCustomAttributes()
    {
        foreach (var attr in _method.GetCustomAttributesData())
        {
            yield return new RuntimeAttributeInfo(attr);
        }
    }

    public IEnumerable<IEnumerable<string>> GetGenericParameterConstraints()
    {
        if (!_method.IsGenericMethod)
        {
            yield break;
        }

        foreach (var typeParam in _method.GetGenericArguments())
        {
            if (typeParam.IsGenericParameter)
            {
                yield return typeParam.GetGenericParameterConstraints()
                    .Where(c => c.FullName != null)
                    .Select(c => c.FullName!);
            }
            else
            {
                yield return [];
            }
        }
    }
}

/// <summary>
/// Wraps <see cref="System.Reflection.ParameterInfo"/> to implement <see cref="IAtsParameterInfo"/>.
/// </summary>
internal sealed class RuntimeParameterInfo : IAtsParameterInfo
{
    private readonly ParameterInfo _param;

    public RuntimeParameterInfo(ParameterInfo param)
    {
        _param = param ?? throw new ArgumentNullException(nameof(param));
    }

    /// <summary>Gets the underlying CLR parameter.</summary>
    public ParameterInfo UnderlyingParameter => _param;

    public string Name => _param.Name ?? $"arg{_param.Position}";
    public string TypeFullName => _param.ParameterType.FullName ?? _param.ParameterType.Name;
    public IAtsTypeInfo ParameterType => new RuntimeTypeInfo(_param.ParameterType);
    public bool IsOptional => _param.IsOptional;
    public object? DefaultValue => _param.RawDefaultValue;

    public IEnumerable<IAtsAttributeInfo> GetCustomAttributes()
    {
        foreach (var attr in _param.GetCustomAttributesData())
        {
            yield return new RuntimeAttributeInfo(attr);
        }
    }
}

/// <summary>
/// Wraps <see cref="System.Reflection.PropertyInfo"/> to implement <see cref="IAtsPropertyInfo"/>.
/// </summary>
internal sealed class RuntimePropertyInfo : IAtsPropertyInfo
{
    private readonly PropertyInfo _prop;

    public RuntimePropertyInfo(PropertyInfo prop)
    {
        _prop = prop ?? throw new ArgumentNullException(nameof(prop));
    }

    /// <summary>Gets the underlying CLR property.</summary>
    public PropertyInfo UnderlyingProperty => _prop;

    public string Name => _prop.Name;
    public string PropertyTypeFullName => _prop.PropertyType.FullName ?? _prop.PropertyType.Name;
    public IAtsTypeInfo PropertyType => new RuntimeTypeInfo(_prop.PropertyType);
    public bool CanRead => _prop.CanRead;
    public bool IsStatic => _prop.GetMethod?.IsStatic ?? _prop.SetMethod?.IsStatic ?? false;
}

/// <summary>
/// Wraps <see cref="System.Reflection.CustomAttributeData"/> to implement <see cref="IAtsAttributeInfo"/>.
/// </summary>
internal sealed class RuntimeAttributeInfo : IAtsAttributeInfo
{
    private readonly CustomAttributeData _attr;
    private IReadOnlyDictionary<string, object?>? _namedArgs;

    public RuntimeAttributeInfo(CustomAttributeData attr)
    {
        _attr = attr ?? throw new ArgumentNullException(nameof(attr));
    }

    public string AttributeTypeFullName => _attr.AttributeType.FullName ?? _attr.AttributeType.Name;

    public IReadOnlyList<object?> FixedArguments =>
        _attr.ConstructorArguments.Select(a => a.Value).ToList();

    public IReadOnlyDictionary<string, object?> NamedArguments =>
        _namedArgs ??= _attr.NamedArguments.ToDictionary(a => a.MemberName, a => a.TypedValue.Value);
}

/// <summary>
/// Wraps <see cref="System.Reflection.Assembly"/> to implement <see cref="IAtsAssemblyInfo"/>.
/// </summary>
internal sealed class RuntimeAssemblyInfo : IAtsAssemblyInfo
{
    private readonly Assembly _assembly;

    public RuntimeAssemblyInfo(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    /// <summary>Gets the underlying CLR assembly.</summary>
    public Assembly UnderlyingAssembly => _assembly;

    /// <summary>Gets the simple name of the assembly.</summary>
    public string Name => _assembly.GetName().Name ?? "";

    public IEnumerable<IAtsTypeInfo> GetTypes()
    {
        Type[] types;
        try
        {
            types = _assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return what we can load
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        foreach (var type in types)
        {
            yield return new RuntimeTypeInfo(type);
        }
    }

    public IEnumerable<IAtsAttributeInfo> GetCustomAttributes()
    {
        foreach (var attr in _assembly.GetCustomAttributesData())
        {
            yield return new RuntimeAttributeInfo(attr);
        }
    }
}
