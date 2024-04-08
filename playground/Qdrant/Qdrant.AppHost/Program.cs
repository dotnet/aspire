// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var qdrant = builder.AddQdrant("qdrant")
    .WithDataVolume("qdrant_data");

builder.AddProject<Projects.Qdrant_ApiService>("apiservice")
    .WithReference(qdrant);

builder.Build().Run();
