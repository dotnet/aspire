// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;

namespace Aspire.Hosting.Tests.Dcp;

internal sealed class MockDashboardAvailability : IDashboardAvailability
{
    public Task WaitForDashboardAvailabilityAsync(Uri url, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
