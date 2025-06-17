// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddNpmApp("site", "../", "dev")
    .WithHttpEndpoint(targetPort: 4321)
    .WithUrlForEndpoint("http", url =>
    {
        url.Url = "/aspire";
        url.DisplayText = "Aspire Site";
    });

builder.Build().Run();
