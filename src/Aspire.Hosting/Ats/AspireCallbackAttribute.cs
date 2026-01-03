// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Marks a delegate as an ATS (Aspire Type System) callback type.
/// Callbacks enable bidirectional communication between the host and polyglot clients.
/// </summary>
/// <remarks>
/// <para>
/// Callbacks are used for scenarios where the host needs to invoke client-provided logic,
/// such as environment variable configuration callbacks or event handlers.
/// </para>
/// <para>
/// Example:
/// <code>
/// [AspireCallback("aspire.core/EnvironmentCallback")]
/// public delegate Task EnvironmentCallbackDelegate(EnvironmentCallbackContextHandle context);
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class AspireCallbackAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspireCallbackAttribute"/> class.
    /// </summary>
    /// <param name="callbackId">
    /// The globally unique callback identifier.
    /// Should follow the format: <c>aspire.{package}/{CallbackName}</c>
    /// For example: <c>aspire.core/EnvironmentCallback</c>
    /// </param>
    public AspireCallbackAttribute(string callbackId)
    {
        CallbackId = callbackId ?? throw new ArgumentNullException(nameof(callbackId));
    }

    /// <summary>
    /// Gets the globally unique callback identifier.
    /// </summary>
    public string CallbackId { get; }
}
