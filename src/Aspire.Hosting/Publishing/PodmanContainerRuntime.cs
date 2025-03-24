// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Publishing;

internal sealed class PodmanContainerRuntime : IContainerRuntime
{
    public Task<string> BuildImageAsync(string contextPath, string dockerfilePath, string imageName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}