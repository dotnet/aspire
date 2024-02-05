// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp;

internal interface IDashboardAvailability
{
    Task WaitForDashboardAvailabilityAsync(Uri url, TimeSpan timeout, CancellationToken cancellationToken = default);
}
