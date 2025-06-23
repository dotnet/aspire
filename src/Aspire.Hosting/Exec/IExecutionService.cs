// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Exec;

internal interface IExecutionService
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

internal class NoopExecutionService : IExecutionService
{
    public Task ExecuteAsync(CancellationToken _)
    {
        return Task.CompletedTask;
    }
}
