// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation which tracks the name of the health check used to detect to health of a resource.
/// </summary>
/// <param name="callback">TODO     </param>
public class HealthCheckAnnotation(Func<IServiceProvider, CancellationToken, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="serviceProvider">TODO</param>
    /// <param name="cancellationToken">TODO</param>
    /// <returns>TODO</returns>
    public async Task WaitUntilResourceHealthyAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await callback(serviceProvider, cancellationToken).ConfigureAwait(false);
    }
}
