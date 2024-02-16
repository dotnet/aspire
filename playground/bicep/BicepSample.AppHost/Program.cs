using Aspire.Hosting.Azure;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var parameter = builder.AddParameter("val");

var templ = builder.AddBicepTemplate("test", "test.bicep")
                   .WithParameter("test", parameter)
                   .WithParameter("values", ["one", "two"]);

var kv = builder.AddBicepKeyVault("kv");
var appConfig = builder.AddBicepAppConfiguration("appConfig").WithParameter("sku", "standard");
var storage = builder.AddAzureBicepAzureStorage("storage");
                    // .UseEmulator();

var blobs = storage.AddBlob("blob");
var tables = storage.AddTable("table");
var queues = storage.AddQueue("queue");

var sqlServer = builder.AddBicepAzureSqlServer("sql").AddDatabase("db");

var pwd = builder.AddParameter("password", secret: true);

var pg = builder.AddBicepAzurePostgres("postgres2", "someuser", pwd).AddDatabase("db2");

var cosmosDb = builder.AddBicepCosmosDb("cosmos")
                      // .UseEmulator()
                      .AddDatabase("db3");

var appInsights = builder.AddBicepApplicationInsights("ai");

// Redis takes forever to spin up...
var redis = builder.AddRedis("redis").PublishAsAzureRedis();

var serviceBus = builder.AddBicepAzureServiceBus("sb", ["queue1"], ["topic1"]);

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
