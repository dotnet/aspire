// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSignalRExtensionsTests
{
    [Fact]
    public async Task AddAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var signalr = builder.AddAzureSignalR("signalr");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        var (manifest, bicep) = await GetManifestWithBicep(signalr.Resource, skipPreparer: true);
        var signalrRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "signalr-roles");
        var (signalrRolesManifest, signalrRolesBicep) = await GetManifestWithBicep(signalrRoles, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(signalrRolesManifest.ToString(), "json")
              .AppendContentAsFile(signalrRolesBicep, "bicep")
              .UseHelixAwareDirectory();
    }

    [Fact]
    public async Task AddServerlessAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var signalr = builder.AddAzureSignalR("signalr", AzureSignalRServiceMode.Serverless);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        var (manifest, bicep) = await GetManifestWithBicep(signalr.Resource, skipPreparer: true);
        var signalrRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "signalr-roles");
        var (signalrRolesManifest, signalrRolesBicep) = await GetManifestWithBicep(signalrRoles, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(signalrRolesManifest.ToString(), "json")
              .AppendContentAsFile(signalrRolesBicep, "bicep")
              .UseHelixAwareDirectory();
    }
}
