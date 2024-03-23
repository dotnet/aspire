// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Qdrant;

var builder = DistributedApplication.CreateBuilder(args);

var key = builder.AddParameter("ApiKey", true);

var qdrant = builder.AddQdrant("qdrant", key)
    .WithDashboard();

builder.AddProject<Projects.Qdrant_ApiService>("apiservice")
    .WithReference(qdrant);

builder.Build().Run();
