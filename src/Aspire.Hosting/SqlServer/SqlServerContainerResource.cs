// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer;

public class SqlServerContainerResource(string name, string password) : ContainerResource(name), ISqlServerResource
{
    public string GeneratedPassword { get; } = password;

    public string? GetConnectionString()
    {
        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var endpoint = allocatedEndpoints.Single();

        // HACK: Use the 127.0.0.1 address because localhost is resolving to [::1] following
        //       up with DCP on this issue.
        return $"Server=127.0.0.1,{endpoint.Port};User ID=sa;Password={GeneratedPassword};TrustServerCertificate=true;";
    }
}
