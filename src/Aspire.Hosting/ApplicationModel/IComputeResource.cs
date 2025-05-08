// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a compute resource.
/// </summary>
/// <remarks>
/// A compute resource is a resource that can be hosted/executed on an <see cref="IComputeEnvironmentResource"/>. Examples
/// include projects, containers, and other resources that can be executed on a compute environment.
/// </remarks>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostic/{0}")]
public interface IComputeResource : IResource
{
}
