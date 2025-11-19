// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.DurableTask;

/// <summary>
/// Represents a Durable Task scheduler resource used in Aspire hosting that provides endpoints
/// and a connection string for Durable Task orchestration scheduling.
/// </summary>
/// <param name="name">The unique resource name.</param>
public sealed class DurableTaskSchedulerResource(string name) : Resource(name), IResourceWithEndpoints, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the expression that resolves to the connection string for the Durable Task scheduler.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => CreateConnectionString();

    internal ReferenceExpression EmulatorDashboardEndpoint => CreateDashboardEndpoint();

    /// <summary>
    /// Gets a value indicating whether the Durable Task scheduler is running using the local
    /// emulator (container) instead of a cloud-hosted service.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    private ReferenceExpression CreateConnectionString()
    {
        if (IsEmulator)
        {
            var grpcEndpoint = new EndpointReference(this, "grpc");

            return ReferenceExpression.Create($"Endpoint=http://{grpcEndpoint.Property(EndpointProperty.Host)}:{grpcEndpoint.Property(EndpointProperty.Port)};Authentication=None");
        }

        static ReferenceExpression CreateReferenceExpression(object? value) => value is IResourceBuilder<ParameterResource> parameterResource
            ? ReferenceExpression.Create($"{parameterResource}")
            : ReferenceExpression.Create($"{value?.ToString() ?? String.Empty}");

        if (this.TryGetLastAnnotation<DurableTaskSchedulerConnectionStringAnnotation>(out var connectionStringAnnotation))
        {
            return CreateReferenceExpression(connectionStringAnnotation.ConnectionString);
        }

        throw new NotImplementedException();
    }

    private ReferenceExpression CreateDashboardEndpoint()
    {
        if (IsEmulator)
        {
            var dashboardEndpoint = new EndpointReference(this, "dashboard");

            return ReferenceExpression.Create($"http://{dashboardEndpoint.Property(EndpointProperty.Host)}:{dashboardEndpoint.Property(EndpointProperty.Port)}");
        }

        throw new NotImplementedException();
    }
}
