// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Represents a Dapr component resource.
/// </summary>
public interface IDaprSidecarResource : IResource
{
    /// <summary>
    /// Gets options used to configure the sidecar, if any.
    /// </summary>
    DaprSidecarOptions? Options { get; }
}
