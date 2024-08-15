// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// TODO
/// </summary>
/// <param name="dependency">TODO</param>
/// <param name="waitTask">TODO</param>
public class WaitAnnotation(IResource dependency, Func<CancellationToken, Task> waitTask) : IResourceAnnotation
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken">TODO</param>
    /// <returns>TODO</returns>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await waitTask(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// TODO
    /// </summary>
    public IResource Dependency { get; } = dependency;
}
