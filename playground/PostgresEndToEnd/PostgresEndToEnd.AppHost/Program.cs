// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

// Abstract resources.
var db1 = builder.AddPostgres("pg1").WithPgAdmin().AddDatabase("db1");
var db2 = builder.AddPostgres("pg2").WithPgAdmin().AddDatabase("db2");
var pg3 = builder.AddPostgres("pg3").WithPgAdmin();
var db3 = pg3.AddDatabase("db3");
var db4 = pg3.AddDatabase("db4");

// Containerized resources.
var db5 = builder.AddPostgres("pg4").WithPgAdmin().PublishAsContainer().AddDatabase("db5");
var db6 = builder.AddPostgres("pg5").WithPgAdmin().PublishAsContainer().AddDatabase("db6");
var pg6 = builder.AddPostgres("pg6").WithPgAdmin().PublishAsContainer();
var db7 = pg6.AddDatabase("db7");
var db8 = pg6.AddDatabase("db8");

builder.AddProject<Projects.PostgresEndToEnd_ApiService>("api")
       .WithReference(db1)
       .WithReference(db2)
       .WithReference(db3)
       .WithReference(db4)
       .WithReference(db5)
       .WithReference(db6)
       .WithReference(db7)
       .WithReference(db8);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
