// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta.Models;

/// <summary>
/// Represents a mapping from a method name and its parameter types to a generated method name.
/// This is used to handle method overloads and provide unique names for methods in generated code.
/// </summary>
internal class Mapping
{
    public Mapping(string methodName, RoType[] parameterTypes, string generatedName)
    {
        MethodName = methodName;
        ParameterTypes = parameterTypes;
        GeneratedName = generatedName;
    }

    public string MethodName { get; set; } = "";
    public RoType[] ParameterTypes { get; set; } = [];
    public string GeneratedName { get; set; } = "";
}
