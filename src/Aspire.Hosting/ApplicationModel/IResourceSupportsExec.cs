// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Exec;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that supports an Aspire Exec against it.
/// Each type of resource will support Exec differently.
/// </summary>
public interface IResourceSupportsExec : IResource
{
    /// <summary>
    /// Executes the resource.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ExecuteAsync(ExecOptions options, ILogger logger, CancellationToken cancellationToken);
}
