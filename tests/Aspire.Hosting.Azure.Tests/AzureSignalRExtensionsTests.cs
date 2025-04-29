// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSignalRExtensionsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AddAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var signalr = builder.AddAzureSignalR("signalr");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        var manifest = await GetManifestWithBicep(signalr.Resource, skipPreparer: true);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Endpoint=https://{signalr.outputs.hostName};AuthType=azure",
              "path": "signalr.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        await Verifier.Verify(manifest.BicepText, extension: "bicep");

        var signalrRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>().Where(r => r.Name == $"signalr-roles"));
        var signalrRolesManifest = await GetManifestWithBicep(signalrRoles, skipPreparer: true);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param signalr_outputs_name string

            param principalType string

            param principalId string

            resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
              name: signalr_outputs_name
            }

            resource signalr_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
                principalType: principalType
              }
              scope: signalr
            }
            """;
        output.WriteLine(signalrRolesManifest.BicepText);
        Assert.Equal(expectedBicep, signalrRolesManifest.BicepText);
    }

    [Fact]
    public async Task AddServerlessAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var signalr = builder.AddAzureSignalR("signalr", AzureSignalRServiceMode.Serverless);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        var manifest = await GetManifestWithBicep(signalr.Resource, skipPreparer: true);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Endpoint=https://{signalr.outputs.hostName};AuthType=azure",
              "path": "signalr.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        await Verifier.Verify(manifest.BicepText, extension: "bicep");

        var signalrRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>().Where(r => r.Name == $"signalr-roles"));
        var signalrRolesManifest = await GetManifestWithBicep(signalrRoles, skipPreparer: true);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param signalr_outputs_name string

            param principalType string

            param principalId string

            resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
              name: signalr_outputs_name
            }

            resource signalr_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
                principalType: principalType
              }
              scope: signalr
            }

            resource signalr_SignalRRestApiOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'fd53cd77-2268-407a-8f46-7e7863d0f521'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'fd53cd77-2268-407a-8f46-7e7863d0f521')
                principalType: principalType
              }
              scope: signalr
            }
            """;
        output.WriteLine(signalrRolesManifest.BicepText);
        Assert.Equal(expectedBicep, signalrRolesManifest.BicepText);
    }
}
