// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer;

public class SqlServerContainerComponent : ContainerComponent, ISqlServerComponent
{
    public SqlServerContainerComponent(string name) : base(name)
    {
        GeneratedPassword = Guid.NewGuid().ToString();
    }

    public string GeneratedPassword { get; }

    public string GetConnectionString(string? databaseName = null)
    {
        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Sql component does not have endpoint annotation.");
        }

        var endpoint = allocatedEndpoints.Single();

        // HACK: Use the 127.0.0.1 address because localhost is resolving to [::1] following
        //       up with DCP on this issue.
        return $"Server=127.0.0.1,{endpoint.Port};Database={databaseName ?? "master"};User ID=sa;Password={GeneratedPassword};TrustServerCertificate=true;";
    }
}
