// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public class PostgresDatabaseResource(string name, PostgresResource postgresContainer) : DistributedApplicationResource(name), IPostgresResource, IDistributedApplicationResourceWithParent<PostgresResource>
{
    public PostgresResource Parent { get; } = postgresContainer;

    public string? GetConnectionString()
    {
        if (Parent.GetConnectionString() is { } connectionString)
        {
            return $"{connectionString}Database={Name}";
        }
        else
        {
            throw new DistributedApplicationException("Parent resource connection string was null.");
        }
    }
}
