// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents metadata about a service.
/// </summary>
public interface IServiceMetadata : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the assembly containing the service.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the fully-qualified path to the assembly containing the service.
    /// </summary>
    public string AssemblyPath { get; }

    /// <summary>
    /// Gets the fully-qualified path to the project containing the service.
    /// </summary>
    public string ProjectPath { get; }
}
