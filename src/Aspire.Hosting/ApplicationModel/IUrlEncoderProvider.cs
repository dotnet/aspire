// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a service that can url-encode a ReferenceExpression.
/// </summary>
public interface IUrlEncoderProvider : IValueWithReferences, IValueProvider, IManifestExpressionProvider
{
    /// <summary>
    /// The value provider.
    /// </summary>
    object ValueProvider { get; }
}

internal class UrlEncoderProvider<T> : IUrlEncoderProvider where T : IValueProvider, IManifestExpressionProvider
{
    private readonly T _valueProvider;

    public UrlEncoderProvider(T valueProvider)
    {
        _valueProvider = valueProvider;
    }

    public object ValueProvider => _valueProvider;
    
    public string ValueExpression
    {
        get
        {
            var expression = _valueProvider.ValueExpression;

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

    IEnumerable<object> IValueWithReferences.References
    {
        get
        {
            if (_valueProvider is IResource)
            {
                yield return _valueProvider;
            }

            if (_valueProvider is IValueWithReferences valueWithReferences)
            {
                foreach (var reference in valueWithReferences.References)
                {
                    yield return reference;
                }
            }
        }
    }
}
