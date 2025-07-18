// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var nginx = builder.AddContainer("nginx", "nginx", "1.25");
_ = builder.AddProject<Projects.Containers_ApiService>("apiservice").WaitFor(nginx);

builder.Build().Run();
