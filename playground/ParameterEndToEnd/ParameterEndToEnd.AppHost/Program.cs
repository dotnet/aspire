// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Use launch profiles to change DOTNET_ENVIRONMENT. If you use the "UseConnectionString"
// environment you will need to add a Cosmos connection string to your user
// secrets (example):
// {
//   "ConnectionStrings": {
//     "db": "[insert connection string]"
//   }
// }
//
// The expression below is a bit more complex than the average developer app would
// probably have, but in our repo we'll probably want to experiment with seperately
// deployed resources a little bit.
var simulateProduction = builder.Configuration.GetValue<bool>("SimulateProduction", false);
var db = builder.Environment.EnvironmentName switch
{
    "Production" => builder.AddConnectionString("db"),
    _ => simulateProduction ? builder.AddConnectionString("db") : builder.AddSqlServer("sql").AddDatabase("db")
};

var insertionrows = builder.AddParameter("insertionrows");

builder.AddProject<Projects.ParameterEndToEnd_ApiService>("api")
       .WithEnvironment("InsertionRows", insertionrows)
       .WithReference(db);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
