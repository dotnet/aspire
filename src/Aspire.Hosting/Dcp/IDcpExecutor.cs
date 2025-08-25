// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dcp;

internal interface IDcpExecutor
{
    Task RunApplicationAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    IResourceReference GetResource(string resourceName);
    Task StartResourceAsync(IResourceReference resourceReference, CancellationToken cancellationToken);
    Task StopResourceAsync(IResourceReference resourceReference, CancellationToken cancellationToken);

    /// <summary>
    /// Runs a resource which did not exist at the application start time.
    /// Adds the resource to the infra to allow monitoring via <see cref="ResourceNotificationService"/> and <see cref="ResourceLoggerService"/>
    /// </summary>
    /// <param name="ephemeralResource">The aspire model resource definition.</param>
    /// <param name="cancellationToken">The token to cancel run.</param>
    /// <returns>The appResource containing the appHost resource and dcp resource.</returns>
    Task<AppResource> RunEphemeralResourceAsync(IResource ephemeralResource, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the ephemeral resource created via <see cref="RunEphemeralResourceAsync"/>.
    /// It's up to the caller to ensure that the resource has finished and is will not be used anymore.
    /// </summary>
    /// <param name="ephemeralResource">The resource to delete.</param>
    Task DeleteEphemeralResourceAsync(AppResource ephemeralResource);
}
