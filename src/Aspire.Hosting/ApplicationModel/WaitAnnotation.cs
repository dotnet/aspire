// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// TODO
/// </summary>
/// <param name="dependency">TODO</param>
/// <param name="callback">TODO</param>
public class WaitAnnotation(IResource dependency, Func<WaitContext, CancellationToken, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="context">TODO</param>
    /// <param name="cancellationToken">TODO</param>
    /// <returns>TODO</returns>
    public async Task WaitAsync(WaitContext context, CancellationToken cancellationToken)
    {
        await callback(context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// TODO
    /// </summary>
    public IResource Dependency { get; } = dependency;
}
