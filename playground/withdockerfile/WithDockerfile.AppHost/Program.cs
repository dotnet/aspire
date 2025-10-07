// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("docker-compose");

// Just for validating parameter handling in Dockerfile builds.
var goVersion = builder.AddParameter("goversion", "1.22");
var secret = builder.AddParameter("secret", secret: true);

builder.AddDockerfile("mycontainer", "qots")
       .WithBuildArg("GO_VERSION", goVersion)
       .WithBuildSecret("SECRET_ASENV", secret)
       .WithEnvironment("DOCKER_BUILDKIT", "1");

// Example: Dynamic Dockerfile generation with sync factory
builder.AddContainer("dynamic-sync", "dynamic-sync-image")
       .WithDockerfile("qots", context =>
       {
           var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
           return $"""
               FROM golang:1.22-alpine AS builder
               WORKDIR /app
               COPY . .
               RUN echo "Built at {timestamp}" > /build-info.txt
               RUN go build -o qots .
               
               FROM alpine:latest
               COPY --from=builder /app/qots /qots
               COPY --from=builder /build-info.txt /build-info.txt
               ENTRYPOINT ["/qots"]
               """;
       });

// Example: Dynamic Dockerfile generation with async factory
builder.AddContainer("dynamic-async", "dynamic-async-image")
       .WithDockerfile("qots", async context =>
       {
           // Simulate reading from a template or external source
           await Task.Delay(1, context.CancellationToken);
           var baseImage = Environment.GetEnvironmentVariable("BASE_IMAGE") ?? "golang:1.22-alpine";
           return $"""
               FROM {baseImage} AS builder
               WORKDIR /app
               COPY . .
               RUN go build -o qots .
               
               FROM alpine:latest
               COPY --from=builder /app/qots /qots
               ENTRYPOINT ["/qots"]
               """;
       });

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
