// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A general-purpose value provider that resolves its value and manifest expression lazily via callbacks.
/// This enables dynamic values to be embedded in <see cref="ReferenceExpression"/> instances, where the
/// actual value is determined at resolution time rather than at expression build time.
/// </summary>
/// <remarks>
/// <para>
/// Use this type when a portion of a connection string or expression depends on state that isn't known
/// until later in the application lifecycle (e.g., whether TLS has been enabled on an endpoint).
/// </para>
/// <para>
/// Callbacks receive a <see cref="ValueProviderContext"/> that provides access to the execution context,
/// the calling resource, and network information. When no separate manifest expression callback is provided,
/// the value callback is used for both <see cref="GetValueAsync(ValueProviderContext, CancellationToken)"/> and <see cref="ValueExpression"/>. A
/// <see langword="null"/> return from the value callback is treated as an empty string for the manifest expression.
/// </para>
/// </remarks>
public class DeferredValueProvider : IValueProvider, IManifestExpressionProvider
{
    private readonly Func<ValueProviderContext, string?> _valueCallback;
    private readonly Func<string>? _manifestExpressionCallback;

    /// <summary>
    /// Initializes a new instance of <see cref="DeferredValueProvider"/> with a context-free callback.
    /// </summary>
    /// <param name="valueCallback">A callback that returns the value. A <see langword="null"/> return is treated
    /// as an empty string for the manifest expression. Called each time the value is resolved.</param>
    /// <param name="manifestExpressionCallback">An optional callback that returns the manifest expression string.
    /// When <see langword="null"/>, the <paramref name="valueCallback"/> is used for both runtime and manifest values.</param>
    public DeferredValueProvider(Func<string?> valueCallback, Func<string>? manifestExpressionCallback = null)
    {
        ArgumentNullException.ThrowIfNull(valueCallback);
        _valueCallback = _ => valueCallback();
        _manifestExpressionCallback = manifestExpressionCallback;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DeferredValueProvider"/> with a context-aware callback.
    /// </summary>
    /// <param name="valueCallback">A callback that receives a <see cref="ValueProviderContext"/> and returns
    /// the value. A <see langword="null"/> return is treated as an empty string for the manifest expression.
    /// Called each time the value is resolved.</param>
    /// <param name="manifestExpressionCallback">An optional callback that returns the manifest expression string.
    /// When <see langword="null"/>, the <paramref name="valueCallback"/> is invoked with an empty
    /// <see cref="ValueProviderContext"/> for the manifest expression.</param>
    public DeferredValueProvider(Func<ValueProviderContext, string?> valueCallback, Func<string>? manifestExpressionCallback = null)
    {
        ArgumentNullException.ThrowIfNull(valueCallback);
        _valueCallback = valueCallback;
        _manifestExpressionCallback = manifestExpressionCallback;
    }

    /// <inheritdoc />
    public string ValueExpression => _manifestExpressionCallback is not null
        ? _manifestExpressionCallback()
        : _valueCallback(new ValueProviderContext()) ?? "";

    /// <inheritdoc />
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        return GetValueAsync(new ValueProviderContext(), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken = default)
    {
        return new(_valueCallback(context));
    }
}
