// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;

namespace Aspire.Hosting.Tests.Dcp;

public class ApplicationExecutorProxy
{
    internal ApplicationExecutorProxy(ApplicationExecutor executor)
    {
        _executor = executor;
    }

    private readonly ApplicationExecutor _executor;

    public Task StartResourceAsync(string resourceName, CancellationToken cancellationToken) => _executor.StartResourceAsync(resourceName, cancellationToken);

    public Task StopResourceAsync(string resourceName, CancellationToken cancellationToken) => _executor.StopResourceAsync(resourceName, cancellationToken);
}
