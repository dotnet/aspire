// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal interface IEncoderProvider : IValueProvider, IManifestExpressionProvider
{
}

internal class UrlEncoderProvider<T> : IEncoderProvider where T : IValueProvider, IManifestExpressionProvider
{
    private readonly T _valueProvider;
    
    public UrlEncoderProvider(T valueProvider)
    {
        _valueProvider = valueProvider;
    }

    public string ValueExpression
    {
        get
        {
            var expression = _valueProvider.ValueExpression;

            if (expression.StartsWith("{", StringComparison.Ordinal) &&
                expression.EndsWith("}", StringComparison.Ordinal))
            {
                return $"{{{expression[1..^1]}:uri}}";
            }

            return expression;
        }
    }

    /// <inheritdoc/>
    public async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        var value = await _valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false);

        // Don't double-encode
        if (value is null)
        {
            return null;
        }

        return Uri.EscapeDataString(value);
    }
}

internal class NullEncoderProvider<T> : IEncoderProvider where T : IValueProvider, IManifestExpressionProvider
{
    private readonly T _valueProvider;
    
    public NullEncoderProvider(T valueProvider)
    {
        _valueProvider = valueProvider;
    }
    public string ValueExpression => _valueProvider.ValueExpression;
    /// <inheritdoc/>
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default) =>
        _valueProvider.GetValueAsync(cancellationToken);
}
