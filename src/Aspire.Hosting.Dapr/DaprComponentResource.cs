// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Initializes a new instance of <see cref="DaprComponentResource"/>.
/// </summary>
/// <param name="name">The resource name.</param>
/// <param name="type">The Dapr component type. This may be a generic "state" or "pubsub" if Aspire should choose an appropriate type when running or deploying.</param>
public sealed class DaprComponentResource(string name, string type) : Resource(ThrowIfNullOrEmpty(name)), IDaprComponentResource
{
    /// <inheritdoc/>
    public string Type { get; } = ThrowIfNullOrEmpty(type);

    /// <inheritdoc/>
    public DaprComponentOptions? Options { get; init; }

    private static string ThrowIfNullOrEmpty([NotNull] string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
