// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;

namespace Aspire.Hosting.Orchestrator;

internal interface IDcpOrchestrator
{
    Task OnEndpointsAllocated(OnEndpointsAllocatedContext context);
    Task OnResourceChanged(OnResourceChangedContext context);
    Task OnResourceFailedToStart(OnResourceFailedToStartContext context);
    Task OnResourceStarting(OnResourceStartingContext context);
}
