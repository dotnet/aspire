// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

sealed class ParameterOrValue(object? parameter) : IValueProvider, IManifestExpressionProvider
{
    public static ParameterOrValue Create(IValueProvider? parameter)
    {
        return new(parameter);
    }

    public static ParameterOrValue Create(object? parameter)
    {
        return new(parameter);
    }

    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        if (parameter is IValueProvider valueProvider)
        {
            return valueProvider.GetValueAsync(cancellationToken);
        }
        else
        {
            return new ValueTask<string?>(parameter?.ToString());
        }
    }

    public string ValueExpression =>
        parameter is IManifestExpressionProvider manifestExpressionProvider
            ? manifestExpressionProvider.ValueExpression
            : parameter?.ToString() ?? String.Empty;
}
