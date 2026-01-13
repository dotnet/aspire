// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Types;

/// <summary>
/// Interface for parameter information that supports both definition and substituted parameters.
/// </summary>
public interface IRoParameterInfo
{
    RoMethod DeclaringMethod { get; }
    RoType ParameterType { get; }
    string Name { get; }
    bool IsOptional { get; }
    object? RawDefaultValue { get; }
    IEnumerable<RoCustomAttributeData> GetCustomAttributes();
}

/// <summary>
/// Wraps an <see cref="RoMethod"/> and substitutes generic type parameters with actual type arguments.
/// Used when accessing methods on constructed generic types like Action&lt;T&gt;.
/// </summary>
internal sealed class RoSubstitutedMethod : RoMethod
{
    private readonly RoMethod _underlyingMethod;
    private readonly IReadOnlyList<RoType> _typeArguments;
    private readonly RoType _declaringType;
    private readonly Lazy<IReadOnlyList<IRoParameterInfo>> _substitutedParameters;
    private readonly Lazy<RoType> _substitutedReturnType;

    public RoSubstitutedMethod(RoMethod underlyingMethod, RoType declaringType, IReadOnlyList<RoType> typeArguments)
    {
        _underlyingMethod = underlyingMethod;
        _declaringType = declaringType;
        _typeArguments = typeArguments;

        _substitutedParameters = new Lazy<IReadOnlyList<IRoParameterInfo>>(() =>
            _underlyingMethod.Parameters
                .Select(p => (IRoParameterInfo)new RoSubstitutedParameterInfo(p, this, SubstituteType(p.ParameterType)))
                .ToList());

        _substitutedReturnType = new Lazy<RoType>(() => SubstituteType(_underlyingMethod.ReturnType));

        // Copy non-type-dependent properties
        Name = underlyingMethod.Name;
        IsStatic = underlyingMethod.IsStatic;
        IsPublic = underlyingMethod.IsPublic;
        IsSpecialName = underlyingMethod.IsSpecialName;
        IsGenericMethodDefinition = underlyingMethod.IsGenericMethodDefinition;
        IsGenericMethod = underlyingMethod.IsGenericMethod;
    }

    public override RoType DeclaringType => _declaringType;
    public override string Name { get; }
    public override IReadOnlyList<RoParameterInfo> Parameters => throw new NotSupportedException("Use ParametersSubstituted instead");
    public IReadOnlyList<IRoParameterInfo> ParametersSubstituted => _substitutedParameters.Value;
    public override RoType ReturnType => _substitutedReturnType.Value;
    public override bool IsStatic { get; protected set; }
    public override bool IsPublic { get; protected set; }
    public override bool IsSpecialName { get; protected set; }
    public override bool IsGenericMethodDefinition { get; protected set; }
    public override bool IsGenericMethod { get; protected set; }
    public override int MetadataToken => _underlyingMethod.MetadataToken;

    public override IReadOnlyList<RoType> GetGenericArguments() => _underlyingMethod.GetGenericArguments();

    public override RoMethod MakeGenericMethod(params RoType[] typeArguments)
    {
        return _underlyingMethod.MakeGenericMethod(typeArguments);
    }

    public override IEnumerable<RoCustomAttributeData> GetCustomAttributes()
    {
        return _underlyingMethod.GetCustomAttributes();
    }

    /// <summary>
    /// Substitutes a type if it's a generic parameter, using the type arguments from the declaring type.
    /// </summary>
    private RoType SubstituteType(RoType type)
    {
        // If the type is a generic parameter, substitute it with the actual type argument
        if (type.IsGenericParameter)
        {
            // Try to find the matching type argument by name/index
            var genericParams = _declaringType.GenericTypeDefinition?.GetGenericArguments() ?? [];
            for (var i = 0; i < genericParams.Count && i < _typeArguments.Count; i++)
            {
                if (genericParams[i].Name == type.Name)
                {
                    return _typeArguments[i];
                }
            }

            // Fallback: if we have exactly one type argument and one generic parameter, use it
            if (_typeArguments.Count == 1)
            {
                return _typeArguments[0];
            }
        }

        return type;
    }
}

/// <summary>
/// A parameter info with a substituted parameter type.
/// </summary>
internal sealed class RoSubstitutedParameterInfo : IRoParameterInfo
{
    private readonly RoParameterInfo _underlyingParameter;

    public RoSubstitutedParameterInfo(RoParameterInfo underlyingParameter, RoMethod declaringMethod, RoType substitutedType)
    {
        _underlyingParameter = underlyingParameter;
        DeclaringMethod = declaringMethod;
        ParameterType = substitutedType;
    }

    public RoMethod DeclaringMethod { get; }
    public RoType ParameterType { get; }
    public string Name => _underlyingParameter.Name;
    public bool IsOptional => _underlyingParameter.IsOptional;
    public object? RawDefaultValue => _underlyingParameter.RawDefaultValue;
    public IEnumerable<RoCustomAttributeData> GetCustomAttributes() => _underlyingParameter.GetCustomAttributes();
}
