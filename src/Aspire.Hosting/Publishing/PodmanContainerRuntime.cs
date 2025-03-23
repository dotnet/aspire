// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Publishing;

internal sealed class PodmanContainerRuntime : IContainerRuntime
{
    public Task<string> BuildAsync(IResource resource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}