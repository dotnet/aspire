// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Containers_ApiService>("apiservice");
var nginx = builder.AddContainer("nginx", "nginx", "1.25");

Debug.Assert(apiService is not null, "apiService should not be null");
Debug.Assert(nginx is not null, "nginx should not be null");

builder.Build().Run();
