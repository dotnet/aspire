// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public class PostgresContainerResource(string name, string password) : ContainerResource(name), IPostgresResource
{
    public string Password { get; } = password;

    public string? GetConnectionString(IDistributedApplicationResource? targetResource = null)
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for Postgres.

        var host = targetResource is null || !targetResource.IsContainer() ? allocatedEndpoint.Address : "host.docker.internal";

        var connectionString = $"Host={host};Port={allocatedEndpoint.Port};Username=postgres;Password={Password};";
        return connectionString;
    }
}
