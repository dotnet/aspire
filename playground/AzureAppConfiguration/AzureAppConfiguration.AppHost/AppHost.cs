// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var appConfig =
    builder.AddAzureAppConfiguration("appconfig")
    .RunAsEmulator(emulator =>
    {
        emulator.WithDataBindMount();
    })
    .WithRefreshKey("Message", 10);

builder.AddProject<Projects.AzureAppConfiguration_Web>("web")
    .WithReference(appConfig);

builder.Build().Run();
