// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies the format for container images.
/// </summary>
[Experimental("ASPIREPIPELINES003", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum ContainerImageFormat
{
    /// <summary>
    /// Docker format (default).
    /// </summary>
    Docker,

    /// <summary>
    /// OCI format.
    /// </summary>
    Oci
}
