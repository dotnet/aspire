// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Ats;

/// <summary>
/// Minimal type info abstraction for ATS scanning.
/// All type references are by FullName (flattened design) to enable code sharing
/// between runtime (System.Reflection) and code-gen (RoType) paths.
/// </summary>
internal interface IAtsTypeInfo
{
    /// <summary>Gets the full name of the type (e.g., "System.String").</summary>
    string FullName { get; }

    /// <summary>Gets the simple name of the type (e.g., "String").</summary>
    string Name { get; }

    /// <summary>Gets whether this type is an interface.</summary>
    bool IsInterface { get; }

    /// <summary>Gets whether this type is a generic type.</summary>
    bool IsGenericType { get; }

    /// <summary>Gets whether this type is an enum.</summary>
    bool IsEnum { get; }

    /// <summary>Gets whether this type is a generic parameter (e.g., T in List&lt;T&gt;).</summary>
    bool IsGenericParameter { get; }

    /// <summary>Gets whether this type is sealed.</summary>
    bool IsSealed { get; }

    /// <summary>Gets whether this type is nested.</summary>
    bool IsNested { get; }

    /// <summary>Gets whether this type is an array.</summary>
    bool IsArray { get; }

    /// <summary>Gets the full name of the base type, or null if none.</summary>
    string? BaseTypeFullName { get; }

    /// <summary>Gets the full name of the generic type definition, or null if not generic.</summary>
    string? GenericTypeDefinitionFullName { get; }

    /// <summary>Gets the full names of all interfaces this type implements.</summary>
    IEnumerable<string> GetInterfaceFullNames();

    /// <summary>Gets the full names of generic type arguments.</summary>
    IEnumerable<string> GetGenericArgumentFullNames();

    /// <summary>Gets the generic type arguments as IAtsTypeInfo.</summary>
    IEnumerable<IAtsTypeInfo> GetGenericArguments();

    /// <summary>Gets the full name of the array element type, or null if not an array.</summary>
    string? GetElementTypeFullName();

    /// <summary>Gets the full names of generic parameter constraints (for type parameters).</summary>
    IEnumerable<string> GetGenericParameterConstraintFullNames();

    /// <summary>Gets all custom attributes on this type.</summary>
    IEnumerable<IAtsAttributeInfo> GetCustomAttributes();

    /// <summary>Gets all public methods on this type.</summary>
    IEnumerable<IAtsMethodInfo> GetMethods();

    /// <summary>Gets all public properties on this type.</summary>
    IEnumerable<IAtsPropertyInfo> GetProperties();

    /// <summary>Gets the enum value names if this is an enum type.</summary>
    IEnumerable<string> GetEnumNames();
}

/// <summary>
/// Minimal method info abstraction for ATS scanning.
/// </summary>
internal interface IAtsMethodInfo
{
    /// <summary>Gets the method name.</summary>
    string Name { get; }

    /// <summary>Gets whether this is a static method.</summary>
    bool IsStatic { get; }

    /// <summary>Gets whether this is a public method.</summary>
    bool IsPublic { get; }

    /// <summary>Gets the full name of the return type.</summary>
    string ReturnTypeFullName { get; }

    /// <summary>Gets the return type as IAtsTypeInfo.</summary>
    IAtsTypeInfo ReturnType { get; }

    /// <summary>Gets all parameters of this method.</summary>
    IEnumerable<IAtsParameterInfo> GetParameters();

    /// <summary>Gets the full names of generic type arguments.</summary>
    IEnumerable<string> GetGenericArgumentFullNames();

    /// <summary>Gets all custom attributes on this method.</summary>
    IEnumerable<IAtsAttributeInfo> GetCustomAttributes();

    /// <summary>
    /// Gets generic parameter constraints for all type parameters.
    /// Returns full names of constraint types for each generic parameter.
    /// </summary>
    IEnumerable<IEnumerable<string>> GetGenericParameterConstraints();
}

/// <summary>
/// Minimal parameter info abstraction for ATS scanning.
/// </summary>
internal interface IAtsParameterInfo
{
    /// <summary>Gets the parameter name.</summary>
    string Name { get; }

    /// <summary>Gets the full name of the parameter type.</summary>
    string TypeFullName { get; }

    /// <summary>Gets the parameter type as IAtsTypeInfo.</summary>
    IAtsTypeInfo ParameterType { get; }

    /// <summary>Gets whether this parameter is optional.</summary>
    bool IsOptional { get; }

    /// <summary>Gets the default value if this is an optional parameter.</summary>
    object? DefaultValue { get; }

    /// <summary>Gets all custom attributes on this parameter.</summary>
    IEnumerable<IAtsAttributeInfo> GetCustomAttributes();
}

/// <summary>
/// Minimal property info abstraction for ATS scanning.
/// </summary>
internal interface IAtsPropertyInfo
{
    /// <summary>Gets the property name.</summary>
    string Name { get; }

    /// <summary>Gets the full name of the property type.</summary>
    string PropertyTypeFullName { get; }

    /// <summary>Gets the property type as IAtsTypeInfo.</summary>
    IAtsTypeInfo PropertyType { get; }

    /// <summary>Gets whether this property has a getter.</summary>
    bool CanRead { get; }

    /// <summary>Gets whether this is a static property.</summary>
    bool IsStatic { get; }
}

/// <summary>
/// Minimal custom attribute info abstraction for ATS scanning.
/// </summary>
internal interface IAtsAttributeInfo
{
    /// <summary>Gets the full name of the attribute type.</summary>
    string AttributeTypeFullName { get; }

    /// <summary>Gets the constructor arguments by index.</summary>
    IReadOnlyList<object?> FixedArguments { get; }

    /// <summary>Gets the named arguments as key-value pairs.</summary>
    IReadOnlyDictionary<string, object?> NamedArguments { get; }
}

/// <summary>
/// Minimal assembly info abstraction for ATS scanning.
/// </summary>
internal interface IAtsAssemblyInfo
{
    /// <summary>Gets all types defined in this assembly.</summary>
    IEnumerable<IAtsTypeInfo> GetTypes();

    /// <summary>Gets all custom attributes on this assembly.</summary>
    IEnumerable<IAtsAttributeInfo> GetCustomAttributes();
}
