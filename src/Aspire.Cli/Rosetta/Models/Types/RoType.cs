// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Rosetta.Models.Types;

internal abstract class RoType : IEquatable<RoType>
{
    protected RoType(RoAssembly assembly)
    {
        DeclaringAssembly = assembly;
    }

    public RoAssembly DeclaringAssembly { get; }

    /// <summary>
    /// For generic parameter type scenarios, gets the declaring type if the current type is a generic parameter of a type, e.g. T in List&lt;T&gt;.
    /// </summary>
    public virtual RoType? DeclaringType => null;

    /// <summary>
    /// For generic parameter type scenarios, gets the declaring method if the current type is a generic parameter of a method, e.g. T in Array.Empty&lt;T&gt;().
    /// </summary>
    public virtual RoMethod? DeclaringMethod => null;

    public abstract string Name { get; }
    public abstract string FullName { get; }
    public virtual bool IsAbstract { get; protected set; }
    public virtual bool IsPublic { get; protected set; }
    public virtual bool IsByRef { get; protected set; }
    public virtual bool IsEnum { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the current type represents an array.
    /// </summary>
    public virtual bool IsArray { get; }

    /// <summary>
    /// Retrieves the type of the elements contained within an array, pointer, or reference type.
    /// </summary>
    public virtual RoType? GetElementType() => null;

    public virtual int GetArrayRank() => throw new ArgumentException("Must be an array type.");

    /// <summary>
    /// Gets a value indicating whether the current type is a generic type, e.g. IResourceBuilder&lt;&gt; or IResourceBuilder&lt;Container%gt;
    /// </summary>
    public virtual bool IsGenericType { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the current type represents a type definition rather than a type reference or
    /// constructed type, e.g., IResourceBuilder&lt;&gt;
    /// </summary>
    public virtual bool IsTypeDefinition { get; protected set; }

    public virtual bool IsSealed { get; protected set; }
    public virtual bool IsNested { get; protected set; }

    /// <summary>
    /// Gets the generic type definition for this type, if it represents a constructed generic type; otherwise, returns
    /// null.
    /// </summary>
    /// <remarks>Use this property to obtain the generic type definition from a constructed generic type, such
    /// as List&lt;int&gt; yielding List&lt;&gt;. If the type is not a constructed generic type, the property returns
    /// null.
    /// </remarks>
    public virtual RoType? GenericTypeDefinition { get; protected set; }

    public virtual IReadOnlyList<RoType> GetGenericArguments() => [];

    public virtual bool IsGenericParameter { get; protected set; }
    public virtual bool IsInterface { get; protected set; }

    /// <summary>
    /// Gets the names of the constants in the current enumeration type.
    /// </summary>
    /// <returns>
    /// An enumerable collection containing the names of the enum values if this type is an enum; otherwise, an empty collection.
    /// </returns>
    /// <remarks>
    /// For enum types, this method returns the names of all fields that represent enum values (fields with the Literal attribute).
    /// For non-enum types, this method returns an empty collection.
    /// The order of names in the returned collection is the order in which they appear in the metadata.
    /// </remarks>
    public virtual IEnumerable<string> GetEnumNames()
    {
        if (!IsEnum)
        {
            return Enumerable.Empty<string>();
        }

        // For RoDefinitionType, this will be overridden to read from metadata
        // For other types, return empty collection
        return Enumerable.Empty<string>();
    }

    public virtual bool ContainsGenericParameters { get; protected set; }
    public virtual IReadOnlyList<RoType> Interfaces => [];

    /// <summary>
    /// Gets the base type of the current type, if one exists.
    /// </summary>
    /// <remarks>
    /// This is null for interfaces and System.Object.
    /// </remarks>
    public virtual RoType? BaseType => null;
    /// <summary>
    /// Gets the generic type arguments for the current type.
    /// </summary>
    public virtual IReadOnlyList<RoType> GenericTypeArguments => [];
    public virtual RoType MakeGenericType(params RoType[] typeArguments) => throw new NotImplementedException();

    /// <summary>
    /// Gets the collection of method metadata associated with the current type.
    /// </summary>
    /// <remarks>Only public methods. Doesn't include property accessors.
    /// The returned list provides read-only access to method information. The order of methods in
    /// the collection is not guaranteed and may vary depending on the underlying type system.
    /// </remarks>
    public virtual IReadOnlyList<RoMethod> Methods => [];
    public virtual RoMethod? GetMethod(string name) => null;
    public virtual IEnumerable<RoCustomAttributeData> GetCustomAttributes() => throw new NotImplementedException();
    /// <summary>
    /// Gets the zero-based position of the generic parameter if this type represents a generic parameter; otherwise -1.
    /// </summary>
    public virtual int GenericParameterPosition => -1;

    /// <summary>
    /// Gets a value indicating whether this type represents a generic type parameter.
    /// </summary>
    public virtual bool IsGenericTypeParameter => false;

    /// <summary>
    /// Gets a value indicating whether this type represents a generic method parameter.
    /// </summary>
    public virtual bool IsGenericMethodParameter => false;

    /// <summary>
    /// Returns the generic parameter constraints for this type if it is a generic parameter; otherwise an empty list.
    /// </summary>
    public virtual IReadOnlyList<RoType> GetGenericParameterConstraints() => throw new InvalidOperationException("Method may only be called on a Type for which Type.IsGenericParameter is true.");

    public bool IsAssignableTo(RoType targetType)
    {
        return targetType.IsAssignableFrom(this);
    }

    public bool IsAssignableFrom(RoType? c)
    {
        if (c == null)
        {
            return false;
        }

        // Check if the types are the same
        if (this == c)
        {
            return true;
        }

        // Check inheritance hierarchy
        var current = c;
        while (current != null)
        {
            if (this == current)
            {
                return true;
            }
            current = current.BaseType;
       }

        // Check if this type is an interface that c implements
        if (IsInterface)
        {
            if (c.Interfaces.Contains(this) || c.Interfaces.Any(this.IsAssignableFrom))
            {
                return true;
            }

            if (c.BaseType != null && IsAssignableFrom(c.BaseType))
            {
                return true;
            }
        }

        // Check if c implements any interfaces that are assignable to this type
        foreach (var interfaceType in c.Interfaces)
        {
            if (this.IsAssignableFrom(interfaceType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines equality based on the fully qualified type name.
    /// </summary>
    public bool Equals(RoType? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        // Equality is defined by identical full type name.
        return string.Equals(FullName, other.FullName, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RoType);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(FullName);
    }

    public static bool operator ==(RoType? left, RoType? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RoType? left, RoType? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return FullName;
    }
}
