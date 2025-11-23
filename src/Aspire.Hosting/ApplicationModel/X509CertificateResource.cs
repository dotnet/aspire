// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a X509 Certificate resource. This may be backed by a local certificate in run mode or a remote certificate in deploy mode.
/// </summary>
[Experimental("ASPIRECERTIFICATES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class X509CertificateResource : Resource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="X509CertificateResource"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    public X509CertificateResource(string name) : base(name)
    {
        ArgumentNullException.ThrowIfNull(name);
    }
}