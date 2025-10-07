// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// Annotation that holds Node.js static build options for YarpNpmResource.
/// </summary>
/// <param name="options">The Node.js static build options.</param>
[Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
internal sealed class NodeStaticBuildOptionsAnnotation(NodeStaticBuildOptions options) : IResourceAnnotation
{
    /// <summary>
    /// Gets the Node.js static build options.
    /// </summary>
    public NodeStaticBuildOptions Options { get; } = options;
}
