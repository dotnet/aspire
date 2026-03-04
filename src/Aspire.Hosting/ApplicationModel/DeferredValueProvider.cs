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
/// Value callbacks are asynchronous and receive a <see cref="ValueProviderContext"/> that provides access
/// to the execution context, the calling resource, and network information. A separate synchronous manifest
/// expression callback is required because <see cref="IManifestExpressionProvider.ValueExpression"/> is a
/// synchronous property.
/// </para>
/// </remarks>
public class DeferredValueProvider : IValueProvider, IManifestExpressionProvider
{
    private readonly Func<ValueProviderContext, ValueTask<string?>> _valueCallback;
    private readonly Func<string> _manifestExpressionCallback;

    /// <summary>
    /// Initializes a new instance of <see cref="DeferredValueProvider"/> with a context-free async callback.
    /// </summary>
    /// <param name="valueCallback">An async callback that returns the value. Called each time the value is resolved.</param>
    /// <param name="manifestExpressionCallback">A callback that returns the manifest expression string.
    /// This is required because <see cref="IManifestExpressionProvider.ValueExpression"/> is synchronous
    /// and cannot call the async <paramref name="valueCallback"/>.</param>
    public DeferredValueProvider(Func<ValueTask<string?>> valueCallback, Func<string> manifestExpressionCallback)
    {
        ArgumentNullException.ThrowIfNull(valueCallback);
        ArgumentNullException.ThrowIfNull(manifestExpressionCallback);
        _valueCallback = _ => valueCallback();
        _manifestExpressionCallback = manifestExpressionCallback;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DeferredValueProvider"/> with a context-aware async callback.
    /// </summary>
    /// <param name="valueCallback">An async callback that receives a <see cref="ValueProviderContext"/> and returns
    /// the value. Called each time the value is resolved.</param>
    /// <param name="manifestExpressionCallback">A callback that returns the manifest expression string.
    /// This is required because <see cref="IManifestExpressionProvider.ValueExpression"/> is synchronous
    /// and cannot call the async <paramref name="valueCallback"/>.</param>
    public DeferredValueProvider(Func<ValueProviderContext, ValueTask<string?>> valueCallback, Func<string> manifestExpressionCallback)
    {
        ArgumentNullException.ThrowIfNull(valueCallback);
        ArgumentNullException.ThrowIfNull(manifestExpressionCallback);
        _valueCallback = valueCallback;
        _manifestExpressionCallback = manifestExpressionCallback;
    }

    /// <inheritdoc />
    public string ValueExpression => _manifestExpressionCallback();

    /// <inheritdoc />
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        return GetValueAsync(new ValueProviderContext(), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken = default)
    {
        return _valueCallback(context);
    }
}
