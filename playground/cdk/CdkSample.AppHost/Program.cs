// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRE0001 // Because we use the CDK callbacks.

using Aspire.Hosting.Azure;
using Azure.Provisioning.KeyVaults;
using Azure.ResourceManager.ApplicationInsights.Models;
using Azure.ResourceManager.OperationalInsights.Models;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureProvisioning();

var cosmosdb = builder.AddAzureCosmosDB("cosmos").AddDatabase("cosmosdb");

var sku = builder.AddParameter("storagesku");
var locationOverride = builder.AddParameter("locationOverride");
var storage = builder.AddAzureStorage("storage", (_, _, account) =>
{
    account.AssignProperty(sa => sa.Sku.Name, sku);
    account.AssignProperty(sa => sa.Location, locationOverride);
});

var blobs = storage.AddBlobs("blobs");

var sqldb = builder.AddSqlServer("sql").AsAzureSqlDatabase().AddDatabase("sqldb");

var signaturesecret = builder.AddParameter("signaturesecret");
var keyvault = builder.AddAzureKeyVault("mykv", (_, construct, keyVault) =>
{
    var secret = new KeyVaultSecret(construct, name: "mysecret");
    secret.AssignProperty(x => x.Properties.Value, signaturesecret);
});

var cache = builder.AddRedis("cache").AsAzureRedis();

var pgsqlAdministratorLogin = builder.AddParameter("pgsqlAdministratorLogin");
var pgsqlAdministratorLoginPassword = builder.AddParameter("pgsqlAdministratorLoginPassword", secret: true);
var pgsqldb = builder.AddPostgres("pgsql")
                   .AsAzurePostgresFlexibleServer(pgsqlAdministratorLogin, pgsqlAdministratorLoginPassword)
                   .AddDatabase("pgsqldb");

var pgsql2 = builder.AddPostgres("pgsql2").AsAzurePostgresFlexibleServer();

var sb = builder.AddAzureServiceBus("servicebus")
    .AddQueue("queue1",
        (_, construct, queue) =>
        {
            queue.Properties.MaxDeliveryCount = 5;
            queue.Properties.LockDuration = TimeSpan.FromMinutes(5);
        })
    .AddTopic("topic1",
        (_, construct, topic) =>
        {
            topic.Properties.EnablePartitioning = true;
        })
    .AddTopic("topic2")
    .AddSubscription("topic1", "subscription1",
        (_, construct, subscription) =>
        {
            subscription.Properties.LockDuration = TimeSpan.FromMinutes(5);
            subscription.Properties.RequiresSession = true;
        })
    .AddSubscription("topic1", "subscription2")
    .AddTopic("topic3", new[] { "sub1", "sub2" });

var appConfig = builder.AddAzureAppConfiguration("appConfig");

var search = builder.AddAzureSearch("search");

var signalr = builder.AddAzureSignalR("signalr");

var logAnalyticsWorkspace = builder.AddAzureLogAnalyticsWorkspace(
    "logAnalyticsWorkspace",
    (_, _, logAnalyticsWorkspace) =>
    {
        logAnalyticsWorkspace.Properties.Sku = new OperationalInsightsWorkspaceSku(OperationalInsightsWorkspaceSkuName.PerNode);
    });

var appInsights = builder.AddAzureApplicationInsights(
    "appInsights",
    (_, _, appInsights) =>
{
    appInsights.AssignProperty(
        p => p.WorkspaceResourceId,
        logAnalyticsWorkspace.Resource.WorkspaceId,
        AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId);

    appInsights.Properties.IngestionMode = IngestionMode.LogAnalytics;
});

builder.AddProject<Projects.CdkSample_ApiService>("api")
    .WithReference(signalr)
    .WithReference(blobs)
    .WithReference(sqldb)
    .WithReference(keyvault)
    .WithReference(cache)
    .WithReference(cosmosdb)
    .WithReference(pgsqldb)
    .WithReference(sb)
    .WithReference(appConfig)
    .WithReference(search)
    .WithReference(appInsights);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
