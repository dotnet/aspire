// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var mountPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "aspire-garnet-data");

var garnet = builder.AddGarnet("garnet")
    .WithDataBindMount(mountPath);

builder.AddProject<Projects.Garnet_ApiService>("apiservice")
    .WithReference(garnet);

builder.Build().Run();
