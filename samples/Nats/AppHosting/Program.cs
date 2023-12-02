﻿using Aspire.Hosting.Nats;

var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNatsContainer("nats");

builder.AddProject<Projects.MyService>("myservice")
    .WithReference(nats);

builder.Build().Run();
