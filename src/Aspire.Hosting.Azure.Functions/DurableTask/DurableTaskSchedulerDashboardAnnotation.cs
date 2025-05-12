// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

sealed class DurableTaskSchedulerDashboardAnnotation(ParameterOrValue? subscriptionId, ParameterOrValue? dashboardEndpoint)
    : IResourceAnnotation
{
    public ParameterOrValue? DashboardEndpoint => dashboardEndpoint;

    public ParameterOrValue? SubscriptionId => subscriptionId;
}
