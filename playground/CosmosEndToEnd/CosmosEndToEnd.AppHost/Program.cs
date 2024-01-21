// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddAzureCosmosDB("cosmos")
                .UseEmulator()
                .AddDatabase("db");

builder.AddProject<Projects.CosmosEndToEnd_ApiService>("api")
       .WithReference(db);

builder.Build().Run();
