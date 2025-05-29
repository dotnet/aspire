// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.AppConfiguration;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.EventHubs;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Search;
using Azure.Provisioning.ServiceBus;
using Azure.Provisioning.SignalR;
using Azure.Provisioning.WebPubSub;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class RoleAssignmentTests()
{
    [Fact]
    public Task ServiceBusSupport()
    {
        return RoleAssignmentTest("sb",
            builder =>
            {
                var sb = builder.AddAzureServiceBus("sb");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(sb, ServiceBusBuiltInRole.AzureServiceBusDataReceiver, ServiceBusBuiltInRole.AzureServiceBusDataSender);
            });
    }

    [Fact]
    public Task AppConfigurationSupport()
    {
        return RoleAssignmentTest("config",
            builder =>
            {
                var config = builder.AddAzureAppConfiguration("config");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(config, AppConfigurationBuiltInRole.AppConfigurationDataReader);
            });
    }

    [Fact]
    public Task OpenAISupport()
    {
        return RoleAssignmentTest("openai",
            builder =>
            {
                var openai = builder.AddAzureOpenAI("openai");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(openai, CognitiveServicesBuiltInRole.CognitiveServicesOpenAIUser);
            });
    }

    [Fact]
    public Task EventHubsSupport()
    {
        return RoleAssignmentTest("eventhubs",
            builder =>
            {
                var eventhubs = builder.AddAzureEventHubs("eventhubs");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(eventhubs, EventHubsBuiltInRole.AzureEventHubsDataReceiver);
            });
    }

    [Fact]
    public Task KeyVaultSupport()
    {
        return RoleAssignmentTest("keyvault",
            builder =>
            {
                var keyvault = builder.AddAzureKeyVault("keyvault");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(keyvault, KeyVaultBuiltInRole.KeyVaultSecretsUser);
            });
    }

    [Fact]
    public Task SearchSupport()
    {
        return RoleAssignmentTest("search",
            builder =>
            {
                var search = builder.AddAzureSearch("search");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(search, SearchBuiltInRole.SearchIndexDataReader);
            });
    }

    [Fact]
    public Task SignalRSupport()
    {
        return RoleAssignmentTest("signalr",
            builder =>
            {
                var signalr = builder.AddAzureSignalR("signalr");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(signalr, SignalRBuiltInRole.SignalRContributor);
            });
    }

    [Fact]
    public Task WebPubSubSupport()
    {
        return RoleAssignmentTest("webpubsub",
            builder =>
            {
                var webpubsub = builder.AddAzureWebPubSub("webpubsub");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(webpubsub, WebPubSubBuiltInRole.WebPubSubServiceReader);
            });
    }

    [Fact]
    public Task CosmosDBSupport()
    {
        return RoleAssignmentTest("cosmos",
            builder =>
            {
                var redis = builder.AddAzureCosmosDB("cosmos");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithReference(redis);
            });
    }

    [Fact]
    public Task RedisSupport()
    {
        return RoleAssignmentTest("redis",
            builder =>
            {
                var redis = builder.AddAzureRedis("redis");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithReference(redis);
            });
    }

    [Fact]
    public Task PostgresSupport()
    {
        return RoleAssignmentTest("postgres",
            builder =>
            {
                var redis = builder.AddAzurePostgresFlexibleServer("postgres");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithReference(redis);
            });
    }

    [Fact]
    public Task SqlSupport()
    {
        return RoleAssignmentTest("sql",
            builder =>
            {
                var redis = builder.AddAzureSqlServer("sql");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithReference(redis);
            });
    }

    private static async Task RoleAssignmentTest(
        string azureResourceName,
        Action<IDistributedApplicationBuilder> configureBuilder
        )
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        configureBuilder(builder);

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var projRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == $"api-roles-{azureResourceName}");

        var (rolesManifest, rolesBicep) = await GetManifestWithBicep(projRoles);

        await Verify(rolesManifest.ToString(), "json")
            .AppendContentAsFile(rolesBicep, "bicep");
            
    }

    private static Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(IResource resource) =>
        AzureManifestUtils.GetManifestWithBicep(resource, skipPreparer: true);

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
