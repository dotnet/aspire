// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a Durable Task Scheduler resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public sealed class DurableTaskSchedulerResource(string name)
    : Resource(name), IResourceWithConnectionString, IResourceWithEndpoints, IResourceWithDashboard
{
    EndpointReference EmulatorDashboardEndpoint => new(this, DurableTaskConstants.Scheduler.Emulator.Endpoints.Dashboard);
    EndpointReference EmulatorSchedulerEndpoint => new(this, DurableTaskConstants.Scheduler.Emulator.Endpoints.Worker);

    /// <summary>
    /// Gets or sets the authentication type used to access the scheduler.
    /// </summary>
    /// <remarks>
    /// The value should be from <see cref="DurableTaskSchedulerAuthentication" />.
    /// The default value is <see cref="DurableTaskSchedulerAuthentication.None" />.
    /// </remarks>
    public string? Authentication { get; set; }

    /// <summary>
    /// Gets or sets the client ID used to access the scheduler, when using managed identity for authentication.
    /// </summary>
    public string? ClientId { get; set; }

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression =>
        this.CreateConnectionString();

    /// <summary>
    /// Gets a value indicating whether the scheduler is running as a local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets or sets the endpoint used by applications to access the scheduler.
    /// </summary>
    public Uri? SchedulerEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the name of the scheduler (if different from the resource name).
    /// </summary>
    public string? SchedulerName { get; set; }

    ReferenceExpression IResourceWithDashboard.DashboardEndpointExpression =>
        this.ResolveDashboardEndpoint();

    internal ReferenceExpression DashboardSchedulerEndpointExpression =>
        this.ResolveDashboardSchedulerEndpoint();

    internal ReferenceExpression SchedulerEndpointExpression =>
        this.ResolveSchedulerEndpoint();

    internal ReferenceExpression? SubscriptionIdExpression =>
        this.ResolveSubscriptionId();

    internal ReferenceExpression SchedulerNameExpression =>
        this.ResolveSchedulerName();

    ReferenceExpression? ResolveSubscriptionId()
    {
        if (this.TryGetLastAnnotation(out DurableTaskSchedulerDashboardAnnotation? annotation) && annotation.SubscriptionId is not null)
        {
            return ReferenceExpression.Create($"{annotation.SubscriptionId}");
        }

        return null;
    }

    ReferenceExpression ResolveSchedulerName()
    {
        if (this.SchedulerName is not null)
        {
            return ReferenceExpression.Create($"{this.SchedulerName}");
        }

        if (this.IsEmulator)
        {
            return ReferenceExpression.Create($"default");
        }

        return ReferenceExpression.Create($"{this.Name}");
    }

    ReferenceExpression CreateConnectionString()
    {
        if (this.TryGetLastAnnotation(out ExistingDurableTaskSchedulerAnnotation? annotation))
        {
            return ReferenceExpression.Create($"{annotation.ConnectionString}");
        }

        string connectionString = $"Authentication={this.Authentication ?? DurableTaskSchedulerAuthentication.None}";

        if (this.ClientId is not null)
        {
            connectionString += $";ClientID={this.ClientId}";
        }

        return ReferenceExpression.Create($"Endpoint={this.SchedulerEndpointExpression};{connectionString}");
    }

    ReferenceExpression ResolveDashboardEndpoint()
    {
        if (this.TryGetLastAnnotation(out DurableTaskSchedulerDashboardAnnotation? annotation) && annotation.DashboardEndpoint is not null)
        {
            // NOTE: Container endpoints do not include the trailing slash.
            return ReferenceExpression.Create($"{annotation.DashboardEndpoint}/");
        }

        if (this.IsEmulator)
        {
            // NOTE: Container endpoints do not include the trailing slash.
            return ReferenceExpression.Create($"{this.EmulatorDashboardEndpoint}/");
        }

        return ReferenceExpression.Create($"{DurableTaskConstants.Scheduler.Dashboard.Endpoint.ToString()}");
    }

    ReferenceExpression ResolveDashboardSchedulerEndpoint()
    {
        if (this.IsEmulator)
        {
            return ReferenceExpression.Create($"{this.EmulatorDashboardEndpoint}/api/");
        }

        return this.ResolveSchedulerEndpoint();
    }

    ReferenceExpression ResolveSchedulerEndpoint()
    {
        if (this.SchedulerEndpoint is not null)
        {
            return ReferenceExpression.Create($"{this.SchedulerEndpoint.ToString()}");
        }

        if (this.IsEmulator)
        {
            // NOTE: Container endpoints do not include the trailing slash.
            return ReferenceExpression.Create($"{this.EmulatorSchedulerEndpoint}/");
        }

        throw new InvalidOperationException("Scheduler endpoint is not set.");
    }
}
