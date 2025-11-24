// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Context for image push options callbacks.
/// </summary>
[Experimental("ASPIRECOMPUTE002", UrlFormat = "https://aka.ms/aspire/diagnostics#{0}")]
public sealed class ContainerImagePushOptionsCallbackContext
{
    /// <summary>
    /// Gets the resource being configured.
    /// </summary>
    public required IResource Resource { get; init; }

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Gets the image push options.
    /// </summary>
    public required ContainerImagePushOptions Options { get; init; }
}
