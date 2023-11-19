// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var containerResource = builder.AddKafkaContainer("kafka");

builder.AddProject<Projects.Producer>("producer")
    .WithReference(containerResource);

builder.AddProject<Projects.Consumer>("consumer")
    .WithReference(containerResource);

builder.Build().Run();
