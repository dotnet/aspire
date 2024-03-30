// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var db1 = builder.AddSqlServer("sql1").PublishAsAzureSqlDatabase().AddDatabase("db1");
var db2 = builder.AddSqlServer("sql2").PublishAsContainer().AddDatabase("db2");

builder.AddProject<Projects.SqlServerEndToEnd_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(db1)
       .WithReference(db2);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
