// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Azure.Provisioning.Sql;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var dbServer = builder.AddAzureSqlServer("mysqlserver")
    .ConfigureInfrastructure(c =>
    {
        const string FREE_DB_SKU = "GP_S_Gen5_2";

        foreach (var database in c.GetProvisionableResources().OfType<SqlDatabase>())
        {
            database.Sku = new SqlSku() { Name = FREE_DB_SKU };
        }
    });

var todosDb = dbServer.AddDatabase("todosdb");

builder.AddProject<Projects.WebApplication1>("api1")
    .WithExternalHttpEndpoints()
    .WithReference(todosDb).WaitFor(todosDb);

builder.AddProject<Projects.WebApplication2>("api2")
    .WithExternalHttpEndpoints()
    .WithReference(todosDb).WaitFor(todosDb);

builder.Build().Run();
