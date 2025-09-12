// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a deployment-specific tag that can be applied to resources during deployment.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Tag = {Tag}")]
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class DeploymentImageTagAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the tag for the deployment.
    /// </summary>
    public required string Tag { get; set; }
}
