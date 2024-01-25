// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Aspire;

internal sealed class MockEnvironmentVariables : IEnvironmentVariables, IEnumerable
{
    private readonly Dictionary<string, string> _valueByName = new(StringComparers.EnvironmentVariableName);

    public void Add(string name, string value)
    {
        _valueByName[name] = value;
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public string? GetString(string variableName, string? defaultValue = null)
    {
        if (_valueByName.TryGetValue(variableName, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
