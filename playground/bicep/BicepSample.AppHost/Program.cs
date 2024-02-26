// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var parameter = builder.AddParameter("val");

var templ = builder.AddBicepTemplate("test", "test.bicep")
                   .WithParameter("test", parameter)
                   .WithParameter("values", ["one", "two"]);

var kv = builder.AddAzureKeyVault("kv");
var appConfig = builder.AddAzureAppConfiguration("appConfig").WithParameter("sku", "standard");
var storage = builder.AddAzureStorage("storage");
                    // .RunAsEmulator();

var blobs = storage.AddBlobs("blob");
var tables = storage.AddTables("table");
var queues = storage.AddQueues("queue");

var sqlServer = builder.AddSqlServer("sql").AsAzureSqlDatabase().AddDatabase("db");

var administratorLogin = builder.AddParameter("administratorLogin");
var administratorLoginPassword = builder.AddParameter("administratorLoginPassword", secret: true);
var pg = builder.AddPostgres("postgres2")
                .AsAzurePostgresFlexibleServer(administratorLogin, administratorLoginPassword)
                .AddDatabase("db2");

var cosmosDb = builder.AddAzureCosmosDB("cosmos")
                      .AddDatabase("db3");

var appInsights = builder.AddAzureApplicationInsights("ai");

// Redis takes forever to spin up...
var redis = builder.AddRedis("redis")
                   .AsAzureRedis();

var serviceBus = builder.AddAzureServiceBus("sb")
                        .AddQueue("queue1")
                        .AddTopic("topic1", ["subscription1", "subscription2"])
                        .AddTopic("topic2", ["subscription1"]);
var signalr = builder.AddAzureSignalR("signalr");

builder.AddProject<Projects.BicepSample_ApiService>("api")
       .WithReference(sqlServer)
       .WithReference(pg)
       .WithReference(cosmosDb)
       .WithReference(blobs)
       .WithReference(tables)
       .WithReference(queues)
       .WithReference(kv)
       .WithReference(appConfig)
       .WithReference(appInsights)
       .WithReference(redis)
       .WithReference(serviceBus)
       .WithReference(signalr)
       .WithEnvironment("bicepValue_test", templ.GetOutput("test"))
       .WithEnvironment("bicepValue0", templ.GetOutput("val0"))
       .WithEnvironment("bicepValue1", templ.GetOutput("val1"));

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
