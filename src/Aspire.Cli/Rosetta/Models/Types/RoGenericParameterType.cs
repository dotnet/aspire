// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace Aspire.Cli.Rosetta.Models.Types;

/// <summary>
/// Represents a generic method or type parameter in the reflection-only model.
/// </summary>
internal abstract class RoGenericParameterType : RoType
{
    private readonly string _name;
    private readonly int _position;
    private readonly GenericParameter _genericParameter;
    private IReadOnlyList<RoType>? _constraints;

    protected RoGenericParameterType(RoAssembly declaringAssembly, GenericParameterHandle handle, int position)
        : base(declaringAssembly)
    {
        var reader = declaringAssembly.Reader;
        _genericParameter = reader.GetGenericParameter(handle);
        _name = reader.GetString(_genericParameter.Name);
        _position = position;
        IsGenericParameter = true;
        ContainsGenericParameters = true;
    }

    public override string Name => _name;
    public override string FullName => _name;
    public override bool IsGenericParameter { get; protected set; }
    public override bool ContainsGenericParameters { get; protected set; }
    public override IReadOnlyList<RoType> GetGenericParameterConstraints()
    {
        if (_constraints != null)
        {
            return _constraints;
        }

        var result = new List<RoType>();

        var reader = DeclaringAssembly.Reader;

        foreach (var cHandle in _genericParameter.GetConstraints())
        {
            var c = reader.GetGenericParameterConstraint(cHandle);
            if (AssemblyLoaderContext.TryGetFullName(c.Type, reader, out var fullName) && DeclaringAssembly.AssemblyLoaderContext.GetType(fullName) is var type && type is not null)
            {
                result.Add(type);
            }
        }

        return _constraints = result;
    }

    public override int GenericParameterPosition => _position;
}

/// <summary>
/// Represents a generic type parameter (e.g. T in List{T}).
/// </summary>
internal sealed class RoGenericTypeParameterType : RoGenericParameterType
{
    private readonly RoType _declaringType;

    public RoGenericTypeParameterType(RoType declaringType, GenericParameterHandle handle, int position)
        : base(declaringType.DeclaringAssembly, handle, position)
    {
        _declaringType = declaringType;
    }

    public override RoType? DeclaringType => _declaringType;
    public override bool IsGenericTypeParameter => true;
}

/// <summary>
/// Represents a generic method parameter (e.g. T in IEnumerable{T} M{T}()).
/// </summary>
internal sealed class RoGenericMethodParameterType : RoGenericParameterType
{
    private readonly RoMethod _declaringMethod;

    public RoGenericMethodParameterType(RoMethod declaringMethod, RoAssembly declaringAssembly, GenericParameterHandle handle, int position)
        : base(declaringAssembly, handle, position)
    {
        _declaringMethod = declaringMethod;
    }

    public override RoMethod? DeclaringMethod => _declaringMethod;
    public override bool IsGenericMethodParameter => true;
}
