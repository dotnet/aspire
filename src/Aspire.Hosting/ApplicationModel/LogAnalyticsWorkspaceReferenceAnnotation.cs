// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that indicates a resource is using a specific Log Analytics Workspace.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LogAnalyticsWorkspaceReferenceAnnotation"/> class.
/// </remarks>
/// <param name="workspace">The Log Analytics Workspace resource.</param>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class LogAnalyticsWorkspaceReferenceAnnotation(Resource workspace) : IResourceAnnotation
{
    /// <summary>
    /// Gets the Log Analytics Workspace resource.
    /// </summary>
    public Resource Workspace { get; } = workspace;
}
