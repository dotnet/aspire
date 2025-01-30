// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Indicates that a Dapr component should be used with the sidecar for the associated resource.
/// </summary>
/// <param name="Component">The Dapr component to use.</param>
[Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
public sealed record DaprComponentReferenceAnnotation(IDaprComponentResource Component) : IResourceAnnotation
{
}
