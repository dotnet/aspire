// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.ApplicationInsights;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.OperationalInsights;
using Azure.Provisioning.ServiceBus;
using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

var cosmosdb = builder.AddAzureCosmosDB("cosmos").AddDatabase("cosmosdb");

var sku = builder.AddParameter("storagesku");
var locationOverride = builder.AddParameter("locationOverride");
var storage = builder.AddAzureStorage("storage")
    .ConfigureConstruct(construct =>
    {
        var account = construct.GetResources().OfType<StorageAccount>().Single();
        account.Sku = new StorageSku() { Name = sku.AsProvisioningParameter(construct) };
        account.Location = locationOverride.AsProvisioningParameter(construct);
    });

var blobs = storage.AddBlobs("blobs");

var sqldb = builder.AddAzureSqlServer("sql").AddDatabase("sqldb");

var signaturesecret = builder.AddParameter("signaturesecret", secret: true);
var keyvault = builder.AddAzureKeyVault("mykv")
    .ConfigureConstruct(construct =>
{
    var keyVault = construct.GetResources().OfType<KeyVaultService>().Single();
    var secret = new KeyVaultSecret("mysecret")
    {
        Parent = keyVault,
        Name = "mysecret",
        Properties = new SecretProperties { Value = signaturesecret.AsProvisioningParameter(construct) }
    };
    construct.Add(secret);
});

var cache = builder.AddAzureRedis("cache");

var pgsqlAdministratorLogin = builder.AddParameter("pgsqlAdministratorLogin");
var pgsqlAdministratorLoginPassword = builder.AddParameter("pgsqlAdministratorLoginPassword", secret: true);
var pgsqldb = builder.AddAzurePostgresFlexibleServer("pgsql")
                   .WithPasswordAuthentication(pgsqlAdministratorLogin, pgsqlAdministratorLoginPassword)
                   .AddDatabase("pgsqldb");

var pgsql2 = builder.AddAzurePostgresFlexibleServer("pgsql2")
    .AddDatabase("pgsql2db");

var sb = builder.AddAzureServiceBus("servicebus")
    .AddQueue("queue1")
    .ConfigureConstruct(construct =>
    {
        var queue = construct.GetResources().OfType<ServiceBusQueue>().Single(q => q.IdentifierName == "queue1");
        queue.MaxDeliveryCount = 5;
        queue.LockDuration = new StringLiteral("PT5M");
        // TODO: this should be
        // queue.LockDuration = TimeSpan.FromMinutes(5);
    })
    .AddTopic("topic1")
    .ConfigureConstruct(construct =>
    {
        var topic = construct.GetResources().OfType<ServiceBusTopic>().Single(q => q.IdentifierName == "topic1");
        topic.EnablePartitioning = true;
    })
    .AddTopic("topic2")
    .AddSubscription("topic1", "subscription1")
    .ConfigureConstruct(construct =>
    {
        var subscription = construct.GetResources().OfType<ServiceBusSubscription>().Single(q => q.IdentifierName == "subscription1");
        subscription.LockDuration = new StringLiteral("PT5M");
        // TODO: this should be
        //subscription.LockDuration = TimeSpan.FromMinutes(5);
        subscription.RequiresSession = true;
    })
    .AddSubscription("topic1", "subscription2")
    .AddTopic("topic3", new[] { "sub1", "sub2" });

var appConfig = builder.AddAzureAppConfiguration("appConfig");

var search = builder.AddAzureSearch("search");

var signalr = builder.AddAzureSignalR("signalr");

var logAnalyticsWorkspace = builder.AddAzureLogAnalyticsWorkspace("logAnalyticsWorkspace")
    .ConfigureConstruct(construct =>
    {
        var logAnalyticsWorkspace = construct.GetResources().OfType<OperationalInsightsWorkspace>().Single();
        logAnalyticsWorkspace.Sku = new OperationalInsightsWorkspaceSku()
        {
            Name = OperationalInsightsWorkspaceSkuName.PerNode
        };
    });

var appInsights = builder.AddAzureApplicationInsights("appInsights", logAnalyticsWorkspace)
    .ConfigureConstruct(construct =>
    {
        var appInsights = construct.GetResources().OfType<ApplicationInsightsComponent>().Single();
        appInsights.IngestionMode = ComponentIngestionMode.LogAnalytics;
    });

builder.AddProject<Projects.CdkSample_ApiService>("api")
    .WithExternalHttpEndpoints()
    .WithReference(signalr)
    .WithReference(blobs)
    .WithReference(sqldb)
    .WithReference(keyvault)
    .WithReference(cache)
    .WithReference(cosmosdb)
    .WithReference(pgsqldb)
    .WithReference(pgsql2)
    .WithReference(sb)
    .WithReference(appConfig)
    .WithReference(search)
    .WithReference(appInsights);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
