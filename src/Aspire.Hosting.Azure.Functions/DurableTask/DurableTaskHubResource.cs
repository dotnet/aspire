// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an indivdual task hub of a Durable Task Scheduler.
/// </summary>
/// <param name="name">The name of the task hub resource.</param>
/// <param name="parent">The scheduler to which the task hub belongs.</param>
public class DurableTaskHubResource(string name, DurableTaskSchedulerResource parent)
    : Resource(name), IResourceWithConnectionString, IResourceWithEndpoints, IResourceWithParent<DurableTaskSchedulerResource>, IDurableTaskResourceWithDashboard
{
    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{this.Parent.ConnectionStringExpression};TaskHub={this.ResolveTaskHubName()}");

    /// <inheritdoc />
    public DurableTaskSchedulerResource Parent => parent;

    /// <summary>
    /// Gets or sets the name of the task hub (if different from the resource name).
    /// </summary>
    public string? TaskHubName { get; set; }

    ReferenceExpression IDurableTaskResourceWithDashboard.DashboardEndpointExpression =>
        this.GetDashboardEndpoint();

    internal ReferenceExpression TaskHubNameExpression =>
        ReferenceExpression.Create($"{this.ResolveTaskHubName()}");

    ReferenceExpression GetDashboardEndpoint()
    {
        var defaultValue = ReferenceExpression.Create($"default");

        ReferenceExpressionBuilder builder = new();

        builder.Append($"{this.ResolveDashboardEndpoint()}subscriptions/{this.ResolveSubscriptionId() ?? defaultValue}/schedulers/{this.Parent.SchedulerNameExpression}/taskhubs/{this.ResolveTaskHubName()}");

        if (!this.Parent.IsEmulator)
        {
            // NOTE: The endpoint is expected to have the trailing slash.
            builder.Append($"?endpoint={QueryParameterReference.Create(this.Parent.DashboardSchedulerEndpointExpression)}");
        }

        return builder.Build();
    }

    string ResolveTaskHubName() => this.TaskHubName ?? this.Name;

    ReferenceExpression ResolveDashboardEndpoint()
    {
        if (this.TryGetLastAnnotation<DurableTaskSchedulerDashboardAnnotation>(out var annotation)
            && annotation.DashboardEndpoint is not null)
        {
            return annotation.DashboardEndpoint;
        }

        return (this.Parent as IDurableTaskResourceWithDashboard).DashboardEndpointExpression;
    }

    ReferenceExpression? ResolveSubscriptionId()
    {
        if (this.TryGetLastAnnotation<DurableTaskSchedulerDashboardAnnotation>(out var annotation)
            && annotation.SubscriptionId is not null)
        {
            return annotation.SubscriptionId;
        }

        return this.Parent.SubscriptionIdExpression;
    }
}
