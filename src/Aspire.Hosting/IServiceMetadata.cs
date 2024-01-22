// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents metadata about a service.
/// </summary>
public interface IServiceMetadata : IResourceAnnotation
{
    /// <summary>
    /// Gets the fully-qualified path to the project containing the service.
    /// </summary>
    public string ProjectPath { get; }
}

[DebuggerDisplay("Type = {GetType().Name,nq}, ProjectPath = {ProjectPath}")]
internal class ServiceMetadata(string projectPath) : IServiceMetadata
{
    public string ProjectPath { get; } = projectPath;
}
