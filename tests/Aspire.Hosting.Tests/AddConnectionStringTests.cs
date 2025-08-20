// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class AddConnectionStringTests
{

    [Fact]
    public async Task AddConnectionStringExpressionIsAValueInTheManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var endpoint = appBuilder.AddParameter("endpoint", "http://localhost:3452");
        var key = appBuilder.AddParameter("key", "secretKey", secret: true);

        // Get the service provider.
        appBuilder.AddConnectionString("mycs", ReferenceExpression.Create($"Endpoint={endpoint};Key={key}"));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var connectionStringResource = Assert.Single(appModel.Resources.OfType<ConnectionStringResource>());

        Assert.Equal("mycs", connectionStringResource.Name);
        var connectionStringManifest = await ManifestUtils.GetManifest(connectionStringResource).DefaultTimeout();

        var expectedManifest = $$"""
            {
              "type": "value.v0",
              "connectionString": "Endpoint={endpoint.value};Key={key.value}"
            }
            """;

        var s = connectionStringManifest.ToString();

        Assert.Equal(expectedManifest, s);
    }

    [Fact]
    public void ConnectionStringsAreVisibleByDefault()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var endpoint = appBuilder.AddParameter("endpoint", "http://localhost:3452");
        var key = appBuilder.AddParameter("key", "secretKey", secret: true);

        appBuilder.AddConnectionString("testcs", ReferenceExpression.Create($"Endpoint={endpoint};Key={key}"));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<ConnectionStringResource>());
        var annotation = connectionStringResource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.False(state.IsHidden);
        Assert.Equal(KnownResourceTypes.ConnectionString, state.ResourceType);
        Assert.Equal(KnownResourceStates.Waiting, state.State?.Text);
    }
}