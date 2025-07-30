// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

namespace Aspire.Hosting.Publishing;

internal interface IContainerRuntime
{
    string Name { get; }
    Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken);
    public Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, CancellationToken cancellationToken);
}