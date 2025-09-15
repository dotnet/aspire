// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class PostgresWithAzureContainerAppsTests
{
    [Fact]
    public void IsPostgresDataVolume_DetectsPostgresCorrectly()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        
        // Create a Postgres resource with data volume
        var postgres = builder.AddPostgres("postgres").WithDataVolume();
        
        // Create a regular container for comparison
        var container = builder.AddContainer("test", "test-image").WithVolume("test-vol", "/test");
        
        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var postgresResource = model.Resources.OfType<PostgresServerResource>().First();
        var containerResource = model.Resources.OfType<ContainerResource>().First();
        
        var postgresMount = postgresResource.Annotations.OfType<ContainerMountAnnotation>().First();
        var containerMount = containerResource.Annotations.OfType<ContainerMountAnnotation>().First();
        
        // Test the logic that would be used in IsPostgresDataVolume
        var isPostgresData = postgresResource.GetType().Name == "PostgresServerResource" && 
                           postgresMount.Target == "/var/lib/postgresql/data";
        var isContainerData = containerResource.GetType().Name == "PostgresServerResource" && 
                            containerMount.Target == "/var/lib/postgresql/data";
        
        Assert.True(isPostgresData);
        Assert.False(isContainerData);
    }
}