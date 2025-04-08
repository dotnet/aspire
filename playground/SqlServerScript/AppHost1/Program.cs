// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var dbServer = builder.AddAzureSqlServer("sqlserver")
    // .RunAsContainer(c => c.WithLifetime(ContainerLifetime.Persistent))
    ;

var todosDb = dbServer.AddDatabase("todosdb");

builder.AddProject<Projects.WebApplication1>("api1").WithReference(todosDb).WaitFor(todosDb);
builder.AddProject<Projects.WebApplication2>("api2").WithReference(todosDb).WaitFor(todosDb);

builder.Build().Run();
