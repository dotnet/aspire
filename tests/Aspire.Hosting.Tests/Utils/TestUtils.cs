// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Tests;
internal static class TestUtils
{
    public sealed class ThrowLifecycleHook : IDistributedApplicationLifecycleHook
    {
        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException();
        }
    }
}
