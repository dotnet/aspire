// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a TCP/UDP port that a container can expose.
/// </summary>
[DebuggerDisplay("{ValueExpression}")]
public readonly record struct ContainerPort(IResource resource) : IManifestExpressionProvider
{
    /// <summary>
    /// Gets the resource that this container port is associated with.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <inheritdoc/>
    public string ValueExpression => $"{{{resource.Name}.containerPort}}";
}
