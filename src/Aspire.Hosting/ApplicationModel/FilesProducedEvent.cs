// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Event that is raised when files are produced by a resource.
/// </summary>
/// <param name="resource">The resource that produced the files.</param>
/// <param name="services">The service provider for the app host.</param>
/// <param name="files">The collection of file paths that were produced.</param>
public class FilesProducedEvent(IResource resource, IServiceProvider services, IEnumerable<string> files) : IDistributedApplicationResourceEvent
{
    /// <summary>
    /// The resource that produced the files.
    /// </summary>
    public IResource Resource => resource;

    /// <summary>
    /// The service provider for the app host.
    /// </summary>
    public IServiceProvider Services => services;

    /// <summary>
    /// The collection of file paths that were produced.
    /// </summary>
    public IEnumerable<string> Files => files;
}