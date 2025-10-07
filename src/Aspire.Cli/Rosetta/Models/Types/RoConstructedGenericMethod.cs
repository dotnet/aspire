// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Rosetta.Models.Types;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class RoConstructedGenericMethod : RoMethod
{
    private readonly IReadOnlyList<RoType> _genericArguments;
    private readonly RoType _constructedReturnType;

    public RoConstructedGenericMethod(RoDefinitionMethod genericMethodDefinition, IReadOnlyList<RoType> genericArguments)
    {
        GenericMethodDefinition = genericMethodDefinition;
        _genericArguments = genericArguments;

        IsStatic = genericMethodDefinition.IsStatic;
        IsPublic = genericMethodDefinition.IsPublic;

        _constructedReturnType = genericMethodDefinition.ReturnType;

        // Make the return type a constructed generic type if needed.
        // T, not a generic type but a generic parameter
        if (_constructedReturnType.IsGenericType)
        {
            // List<T>, a generic type
            var typeDef = _constructedReturnType.GenericTypeDefinition ?? throw new InvalidOperationException("Generic type definition is null for a type marked as a generic type definition.");
            var typeArgs = _constructedReturnType.GetGenericArguments().Select(t =>
            {
                if (t.DeclaringMethod == genericMethodDefinition)
                {
                    // This is a generic method parameter, substitute it.
                    for (var i = 0; i < genericMethodDefinition.GetGenericArguments().Count; i++)
                    {
                        if (genericMethodDefinition.GetGenericArguments()[i] == t)
                        {
                            if (i >= _genericArguments.Count)
                            {
                                throw new InvalidOperationException("Not enough generic arguments provided for the method.");
                            }
                            return _genericArguments[i];
                        }
                    }
                }
                return t;
            }).ToArray();
            _constructedReturnType = new RoConstructedGenericType(typeDef, typeArgs);
        }
        else if (_constructedReturnType.IsGenericParameter)
        {
            for (var i = 0; i < genericMethodDefinition.GetGenericArguments().Count; i++)
            {
                if (genericMethodDefinition.GetGenericArguments()[i] == _constructedReturnType)
                {
                    if (i >= _genericArguments.Count)
                    {
                        throw new InvalidOperationException("Not enough generic arguments provided for the method.");
                    }

                    _constructedReturnType = _genericArguments[i];
                }
            }
        }
    }

    public RoDefinitionMethod GenericMethodDefinition { get; }
    public override RoType DeclaringType => GenericMethodDefinition.DeclaringType;
    public override string Name => GenericMethodDefinition.Name;
    public override IReadOnlyList<RoParameterInfo> Parameters => GenericMethodDefinition.Parameters;
    public override RoType ReturnType => _constructedReturnType;
    public override IReadOnlyList<RoType> GetGenericArguments() => _genericArguments;
    public override bool IsGenericMethodDefinition { get; protected set; }
    public override bool IsGenericMethod { get; protected set; } = true;
    public override int MetadataToken => GenericMethodDefinition.MetadataToken;

    public override bool IsStatic { get; protected set; }
    public override bool IsPublic { get; protected set; }

    public override RoMethod MakeGenericMethod(params RoType[] typeArguments) => throw new InvalidOperationException("Already a constructed generic method.");
    public override IEnumerable<RoCustomAttributeData> GetCustomAttributes() => GenericMethodDefinition.GetCustomAttributes();
}
