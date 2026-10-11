// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.Native.Dcp;

namespace Aspire.Hosting.Native.Core.Tests;

public class DcpJsonTests
{
    [Fact]
    public async Task GenericContainerSupportsMultipleEndpointsEntrypointAndMounts()
    {
        var spec = new DcpWorkloadSpec
        {
            Image = "example/service:1", Command = "/bin/worker", Env = [],
            Args = ["--serve"],
            Ports = [new DcpContainerPort(8080, "127.0.0.1", "TCP"),
                new DcpContainerPort(8443, "127.0.0.1", "TCP") { HostPort = 5001 }],
            VolumeMounts = [new DcpVolumeMount("volume", "retained-data", "/data", false),
                new DcpVolumeMount("bind", "/host/config", "/config", true)]
        };
        await Verify(JsonSerializer.Serialize(spec, DcpJsonContext.Default.DcpWorkloadSpec), "json")
            .UseDirectory("Snapshots");
    }
    [Fact]
    public async Task TypedWorkloadEnvelopePreservesTheDcpWireContract()
    {
        var spec = new DcpWorkloadSpec
        {
            Image = "example/worker:1", Env = [new DcpEnvironment("CUSTOM", "configured")],
            Args = ["--work"], Ports = [new DcpContainerPort(4567, "127.0.0.1", "TCP")]
        };
        var envelope = new DcpResourceEnvelope("usvc-dev.developer.microsoft.com/v1", "Container",
            new DcpMetadata("test-worker", null),
            JsonSerializer.SerializeToElement(spec, DcpJsonContext.Default.DcpWorkloadSpec));
        await Verify(JsonSerializer.Serialize(envelope, DcpJsonContext.Default.DcpResourceEnvelope), "json")
            .UseDirectory("Snapshots");
    }

    [Fact]
    public void TypedStatusAcceptsAdditionalKubernetesFieldsWithoutInventingExitCodesOrPorts()
    {
        var resource = JsonSerializer.Deserialize("""
            {"apiVersion":"usvc-dev.developer.microsoft.com/v1","kind":"Executable",
             "metadata":{"name":"arbitrary-worker"},"status":{"state":"Exited","futureField":true}}
            """, DcpJsonContext.Default.DcpResource)!;
        Assert.Equal("Exited", resource.Status!.State);
        Assert.Null(resource.Status.ExitCode);
        Assert.Null(resource.Status.EffectivePort);
        Assert.Null(resource.Status.Pid);
    }
}
