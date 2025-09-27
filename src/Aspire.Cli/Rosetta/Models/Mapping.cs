// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Rosetta.Models;

public class Mapping
{
    public Mapping(string methodName, Type[] parameterTypes, string generatedName)
    {
        MethodName = methodName;
        ParameterTypes = parameterTypes;
        GeneratedName = generatedName;
    }

    public string MethodName { get; set; } = "";
    public Type[] ParameterTypes { get; set; } = [];
    public string GeneratedName {get; set;} = "";
}
