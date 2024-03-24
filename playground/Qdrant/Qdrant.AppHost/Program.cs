// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

//var key = builder.AddParameter("QdrantApiKey", true);

var qdrant = builder.AddQdrant("qdrant");

builder.AddProject<Projects.Qdrant_ApiService>("apiservice")
    .WithReference(qdrant);

builder.Build().Run();
