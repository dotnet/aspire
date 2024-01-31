// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Aspire;

internal sealed class MockConfiguration : IConfiguration, IEnumerable
{
    private readonly Dictionary<string, string> _valueByName = new(StringComparers.EnvironmentVariableName);

    public string? this[string key]
    {
        get
        {
            if (_valueByName.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }
        set => throw new NotImplementedException();
    }

    public void Add(string name, string value)
    {
        _valueByName[name] = value;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => throw new NotImplementedException();

    public IChangeToken GetReloadToken() => throw new NotImplementedException();

    public IConfigurationSection GetSection(string key) => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
