// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var pulsar = builder
    .AddPulsar(
        name: "pulsar",
        servicePort: 8080,
        brokerPort: 6650
    )
    .WithPulsarManager(
        name: "pulsar-manager",
        frontendPort: 9527,
        backendPort: 7750,
        configureContainer: c => c
            .WithApplicationProperties()
            .WithDefaultEnvironment("pulsar-playground")
    );

builder.AddProject<Projects.ApachePulsar_Api>("api")
    .WithExternalHttpEndpoints()
    .WithReference(pulsar);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
