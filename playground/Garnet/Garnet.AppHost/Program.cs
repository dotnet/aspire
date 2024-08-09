// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var garnet = builder.AddGarnet("garnet")
    .WithDataVolume("garnet-data");
builder.AddProject<Projects.Garnet_ApiService>("apiservice")
    .WithReference(garnet);

builder.Build().Run();
