// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Proxy support annotation for a resource, used to disable all endpoint proxies for a resource if desired
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Enabled = {Enabled}")]
public sealed class ProxySupportAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Create a new instance of the <see cref="ProxySupportAnnotation"/> class.
    /// </summary>
    [Experimental("ASPIREPROXYENDPOINTS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public ProxySupportAnnotation()
    {}

    /// <summary>
    /// Gets or sets the value indicating whether the proxy support is enabled for the resource.
    /// </summary>
    public required bool ProxyEnabled { get; set; }
}
