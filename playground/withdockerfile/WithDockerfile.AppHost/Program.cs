// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration["Parameters:goversion"] = "1.22"; // Just for validating parameter handling in Dockerfile builds.

var goVersion = builder.AddParameter("goversion");
var secret = builder.AddParameter("secret", secret: true);

builder.AddDockerfile("mycontainer", "qots")
       .WithBuildArg("GO_VERSION", goVersion)
       .WithBuildSecret("SECRET_ASFILE", new FileInfo("Program.cs"))
       .WithBuildSecret("SECRET_ASENV", secret);

builder.AddRedis("vanillaredis").WithRedisCommander();

builder.AddRedis("spicyredis")
       .WithDockerfile("spicyredis") // This overrides the port that we listen on.
       .WithEndpoint("tcp", (endpoint) =>
       {
           endpoint.TargetPort = 6380; // This fixes the app model so it still works.
       }).WithRedisCommander();

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
