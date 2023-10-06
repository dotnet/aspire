// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public class PostgresContainerComponent(string name) : ContainerComponent(name), IPostgresComponent
{
    public string GetConnectionString(string? databaseName = null)
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        if (!this.TryGetLastAnnotation<PostgresPasswordAnnotation>(out var passwordAnnotation))
        {
            throw new DistributedApplicationException($"Postgres does not have a password set!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for Postgres.

        var baseConnectionString = $"Host={allocatedEndpoint.Address};Port={allocatedEndpoint.Port};Username=postgres;Password={passwordAnnotation.Password};";
        var connectionString = databaseName is null ? baseConnectionString : $"{baseConnectionString}Database={databaseName};";
        return connectionString;
    }
}
