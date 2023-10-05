// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface ILogSource
{
    ValueTask<bool> StartAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<string[]> WatchOutputLogAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<string[]> WatchErrorLogAsync(CancellationToken cancellationToken);
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
