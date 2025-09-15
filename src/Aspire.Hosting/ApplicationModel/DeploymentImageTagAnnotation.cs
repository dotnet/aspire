// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a deployment-specific tag that can be applied to resources during deployment.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeploymentImageTagCallbackAnnotation"/> class.
/// </remarks>
/// <param name="callback">The callback that returns the deployment tag name.</param>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class DeploymentImageTagCallbackAnnotation(Func<string> callback) : IResourceAnnotation
{
    /// <summary>
    /// The callback that returns the deployment tag name.
    /// </summary>
    public Func<string> Callback { get; } = callback ?? throw new ArgumentNullException(nameof(callback));
}
