// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var clickhouse = builder.AddClickHouse("clickhouse")
                .AddDatabase("default");

builder.AddProject<Projects.ClickHouse_ApiService>("api")
       .WithReference(clickhouse);

builder.Build().Run();
