// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddRedis("redis").WithRedisCommander(c => c.WithDevTunnel("http"));

builder.AddContainer("grafana1", "grafana/grafana")
       .WithEndpoint(3000, 3000, "http")
       .WithDevTunnel("http", t => t.AllowAnonymous = true);

//builder.AddContainer("grafana2", "grafana/grafana")
//       .WithEndpoint(3001, 3000, "http")

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
