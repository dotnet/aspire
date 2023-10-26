// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Aspire.Hosting.ApplicationModel;

[InterpolatedStringHandler]
public struct EnvironmentVariableStringInterpolationHandler
{
    private readonly StringBuilder _builder;
    private readonly object?[]? _parameters;
    private int _paramterCount;

    public EnvironmentVariableStringInterpolationHandler(int literalLength, int formattedCount)
    {
        _builder = new StringBuilder(literalLength);
        _parameters = formattedCount > 0 ? new object?[formattedCount] : null;
    }

    public void AppendLiteral(string s)
    {
        _builder.Append(s);
    }

    public void AppendFormatted<T>(T item)
    {
        var parameterName = $"{{{_paramterCount}}}";
        _builder.Append(parameterName);
        _parameters![_paramterCount++] = item;
    }

    public void AppendFormatted(Func<string?> item)
    {
        var parameterName = $"{{{_paramterCount}}}";
        _builder.Append(parameterName);
        _parameters![_paramterCount++] = item;
    }

    internal string GetValue()
    {
        if (_parameters is null)
        {
            return _builder.ToString();
        }

        var transformed = new string?[_paramterCount];
        var at = 0;
        foreach (var p in _parameters)
        {
            transformed[at++] = p switch
            {
                Func<string?> d => d(),
                IResourceWithConnectionString resource => resource.GetConnectionString(),
                EndpointReference reference => reference.UriString,
                _ => p?.ToString()
            };
        }
        return string.Format(CultureInfo.InvariantCulture, _builder.ToString(), transformed);
    }
}
