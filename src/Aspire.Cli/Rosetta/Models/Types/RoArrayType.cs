// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Rosetta.Models.Types;

internal class RoArrayType : RoType
{
    private readonly RoType _elementType;
    private readonly int _rank;

    public RoArrayType(RoType elementType, int rank)
        :base(elementType.DeclaringAssembly)
    {
        _elementType = elementType;
        _rank = rank;
        Name = elementType.Name + "[]";
        FullName = elementType.FullName + $"[{new string(',', _rank - 1)}]";
    }
    public override string Name { get; }
    public override string FullName { get; }
    public override bool IsArray => true;
    public override RoType? GetElementType() => _elementType;
    public override int GetArrayRank() => _rank;
}
