// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Rosetta.Models.Types;

internal sealed class RoConstructedGenericType : RoType
{
    private readonly RoType _genericTypeDefinition;
    private readonly IReadOnlyList<RoType> _genericTypeArguments;

    public RoConstructedGenericType(RoType genericTypeDefinition, IReadOnlyList<RoType> genericTypeArguments)
        : base(genericTypeDefinition.DeclaringAssembly)
    {
        _genericTypeDefinition = genericTypeDefinition;
        _genericTypeArguments = genericTypeArguments;
        IsGenericType = true;
        IsInterface = genericTypeDefinition.IsInterface;
        IsPublic = genericTypeDefinition.IsPublic;
        IsAbstract = genericTypeDefinition.IsAbstract;
        GenericTypeDefinition = genericTypeDefinition;

        // Build display names similar to System.Type
        Name = BuildName(includeNamespace: false);
        FullName = BuildName(includeNamespace: true);
    }

    public override string Name { get; }
    public override string FullName { get; }

    public override bool IsGenericType { get; protected set; }
    public override RoType? GenericTypeDefinition { get; protected set; }
    public override IReadOnlyList<RoType> GetGenericArguments() => _genericTypeArguments;
    public override IReadOnlyList<RoType> GenericTypeArguments => _genericTypeArguments;
    public override bool ContainsGenericParameters => _genericTypeArguments.Any(a => a.ContainsGenericParameters);

    public override RoType MakeGenericType(params RoType[] typeArguments)
    {
        return new RoConstructedGenericType(_genericTypeDefinition, typeArguments);
    }

    private string BuildName(bool includeNamespace)
    {
        var defFullName = includeNamespace ? _genericTypeDefinition.FullName : _genericTypeDefinition.Name;
        // Strip the `arity suffix from the definition name for constructed form
        var backtickIndex = defFullName.IndexOf('`');
        var baseName = backtickIndex >= 0 ? defFullName.Substring(0, backtickIndex) : defFullName;
        var args = string.Join(", ", _genericTypeArguments.Select(a => includeNamespace ? a.FullName : a.Name));
        return $"{baseName}<{args}>";
    }
}
