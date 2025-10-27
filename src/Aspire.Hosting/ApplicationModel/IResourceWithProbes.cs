// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has probes associated with it.
/// </summary>
[Experimental("ASPIREPROBES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IResourceWithProbes : IResource
{
}
