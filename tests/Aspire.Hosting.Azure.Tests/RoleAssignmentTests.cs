// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class RoleAssignmentTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ServiceBusSupport()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppsInfrastructure();

        var sb = builder.AddAzureServiceBus("sb");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(sb, ServiceBusBuiltInRole.AzureServiceBusDataReceiver, ServiceBusBuiltInRole.AzureServiceBusDataSender);

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var projRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>().Where(r => r.Name == $"api-roles"));

        var (rolesManifest, rolesBicep) = await GetManifestWithBicep(projRoles);

        var expectedRolesManifest =
            """
            {
              "type": "azure.bicep.v0",
              "path": "api-roles.module.bicep",
              "params": {
                "sb_outputs_name": "{sb.outputs.name}"
              }
            }
            """;
        Assert.Equal(expectedRolesManifest, rolesManifest.ToString());

        var expectedRolesBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sb_outputs_name string

            resource api_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
              name: take('api_identity-${uniqueString(resourceGroup().id)}', 128)
              location: location
            }

            resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: sb_outputs_name
            }

            resource sb_AzureServiceBusDataReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(sb.id, api_identity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'))
              properties: {
                principalId: api_identity.properties.principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')
                principalType: 'ServicePrincipal'
              }
              scope: sb
            }

            resource sb_AzureServiceBusDataSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(sb.id, api_identity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'))
              properties: {
                principalId: api_identity.properties.principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
                principalType: 'ServicePrincipal'
              }
              scope: sb
            }

            output id string = api_identity.id

            output clientId string = api_identity.properties.clientId

            output principalId string = api_identity.properties.principalId
            """;
        output.WriteLine(rolesBicep);
        Assert.Equal(expectedRolesBicep, rolesBicep);
    }

    private static Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(IResource resource) =>
        AzureManifestUtils.GetManifestWithBicep(resource, skipPreparer: true);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
