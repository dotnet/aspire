using Aspire.Hosting.Azure;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

//var parameter = builder.AddParameter("val");

//var templ = builder.AddBicepTemplate("test", "test.bicep")
//                   .AddParameter("test", parameter)
//                   .AddParameter("values", ["one", "two"]);

var kv = builder.AddBicepKeyVault("kv");
var appConfig = builder.AddBicepAppConfiguration("appConfig");
var blobs = builder.AddAzureBicepStorage("storage").AddBlob("blob");

var sqlServer = builder.AddBicepAzureSql("sql").AddDatabase("db");

var user = builder.AddParameter("username");
var pwd = builder.AddParameter("password", secret: true);

var pg = builder.AddAzurePostgres("postgres2", user, pwd).AddDatabase("db2");

var cosmosDb = builder.AddBicepCosmosDb("cosmos").AddDatabase("db3");

builder.AddProject<Projects.BicepSample_ApiService>("api")
       .WithReference(sqlServer)
       .WithReference(pg)
       .WithReference(cosmosDb)
       .WithReference(blobs)
       .WithReference(kv)
       .WithReference(appConfig);
//.WithEnvironment("bicepValue_test", templ.GetOutput("test"))
//.WithEnvironment("bicepValue0", templ.GetOutput("val0"))
//.WithEnvironment("bicepValue1", templ.GetOutput("val1"));
//.WithReference(pg);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
