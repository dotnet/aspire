// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Passed the callback on a <see cref="WaitAnnotation"/> to allow access to the dependency injection container.
/// </summary>
/// <param name="logger">TODO</param>
/// <param name="serviceProvider">TODO</param>
public class WaitContext(ILogger logger, IServiceProvider serviceProvider)
{
    /// <summary>
    /// TODO:
    /// </summary>
    public ILogger Logger { get; } = logger;

    /// <summary>
    /// The <see cref="IServiceProvider"/> for the application host.
    /// </summary>
    public IServiceProvider Services { get; } = serviceProvider;
}
