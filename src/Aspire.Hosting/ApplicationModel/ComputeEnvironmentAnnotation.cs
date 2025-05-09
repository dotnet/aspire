// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class ComputeEnvironmentAnnotation(IComputeEnvironmentResource computeEnvironment) : IResourceAnnotation
{
    public IComputeEnvironmentResource ComputeEnvironment { get; } = computeEnvironment;
}
