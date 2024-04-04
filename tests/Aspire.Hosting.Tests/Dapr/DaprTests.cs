// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dapr;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Dapr;

public class DaprTests
{
    [Fact]
    public async Task WithDaprSideCarAddsAnnotationAndSidecarResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddDapr(o =>
        {
            // Fake path to avoid throwing
            o.DaprPath = "dapr";
        });

        builder.AddContainer("name", "image")
            .WithEndpoint("http", e =>
            {
                e.Port = 8000;
                e.AllocatedEndpoint = new(e, "localhost", 80);
            })
            .WithDaprSidecar();

        using var app = builder.Build();
        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        Assert.Equal(3, model.Resources.Count);
        var container = Assert.Single(model.Resources.OfType<ContainerResource>());
        var sidecarResource = Assert.Single(model.Resources.OfType<IDaprSidecarResource>());
        var sideCarCli = Assert.Single(model.Resources.OfType<ExecutableResource>());

        Assert.True(sideCarCli.TryGetEndpoints(out var endpoints));

        var ports = new Dictionary<string, int>
        {
            ["http"] = 3500,
            ["grpc"] = 50001,
            ["metrics"] = 9090
        };

        foreach (var e in endpoints)
        {
            e.AllocatedEndpoint = new(e, "localhost", ports[e.Name], targetPortExpression: $$$"""{{- portForServing "{{{e.Name}}}" -}}""");
        }

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(container);
        var sidecarArgs = await ArgumentEvaluator.GetArgumentListAsync(sideCarCli);

        Assert.Equal("http://localhost:3500", config["DAPR_HTTP_ENDPOINT"]);
        Assert.Equal("http://localhost:50001", config["DAPR_GRPC_ENDPOINT"]);

        var expectedArgs = new[]
        {
            "run",
            "--app-id",
            "name",
            "--app-port",
            "80",
            "--dapr-grpc-port",
            "{{- portForServing \"grpc\" -}}",
            "--dapr-http-port",
            "{{- portForServing \"http\" -}}",
            "--metrics-port",
            "{{- portForServing \"metrics\" -}}",
            "--app-channel-address",
            "localhost"
        };

        Assert.Equal(expectedArgs, sidecarArgs);
        Assert.NotNull(container.Annotations.OfType<DaprSidecarAnnotation>());
    }
}
