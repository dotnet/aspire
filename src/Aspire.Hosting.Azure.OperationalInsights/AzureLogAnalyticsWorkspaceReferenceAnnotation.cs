// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Annotation that indicates a resource is using a specific Log Analytics Workspace.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AzureLogAnalyticsWorkspaceReferenceAnnotation"/> class.
/// </remarks>
/// <param name="workspace">The Log Analytics Workspace resource.</param>
public class AzureLogAnalyticsWorkspaceReferenceAnnotation(AzureLogAnalyticsWorkspaceResource workspace) : IResourceAnnotation
{
    /// <summary>
    /// Gets the Log Analytics Workspace resource.
    /// </summary>
    public AzureLogAnalyticsWorkspaceResource Workspace { get; } = workspace;
}
