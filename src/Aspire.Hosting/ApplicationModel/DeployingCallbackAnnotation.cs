// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a default deploying callback annotation for a distributed application model.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeployingCallbackAnnotation"/> class.
/// </remarks>
/// <param name="callback">The deploying callback.</param>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class DeployingCallbackAnnotation(Func<DeployingContext, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// The deploying callback.
    /// </summary>
    public Func<DeployingContext, Task> Callback { get; } = callback ?? throw new ArgumentNullException(nameof(callback));
}
