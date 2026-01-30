// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An annotation that indicates a resource is the target of a private endpoint.
/// When this annotation is present, the resource should be configured to deny public network access.
/// </summary>
[Experimental("ASPIREAZURE003", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class PrivateEndpointTargetAnnotation : IResourceAnnotation
{
}
