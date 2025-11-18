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

    private ReferenceExpression CreateConnectionString()
    {
        if (this.IsContainer())
        {
            var grpcEndpoint = new EndpointReference(this, "grpc");

            return ReferenceExpression.Create($"Endpoint=http://{grpcEndpoint.Property(EndpointProperty.Host)}:{grpcEndpoint.Property(EndpointProperty.Port)};Authentication=None");
        }

        throw new NotImplementedException();
    }
}
