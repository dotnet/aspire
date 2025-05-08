// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a compute environment resource.
/// </summary>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostic/{0}")]
public interface IComputeEnvironmentResource : IResource
{
}
