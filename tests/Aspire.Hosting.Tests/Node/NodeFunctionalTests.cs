// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Testing;
using Xunit;

namespace Aspire.Hosting.Tests.Node;

[Collection("NodeApp")]
public class NodeFunctionalTests
{
    private readonly NodeAppFixture _nodeJsFixture;

    public NodeFunctionalTests(NodeAppFixture nodeJsFixture)
    {
        _nodeJsFixture = nodeJsFixture;
    }

    [Fact]
    [RequiresTools(["node"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyNodeAppWorks()
    {
        var testProgram = _nodeJsFixture.TestProgram;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        using var nodeClient = testProgram.App!.CreateHttpClient(testProgram.NodeAppBuilder!.Resource.Name, "http");
        var response0 = await nodeClient.GetStringAsync("/", cts.Token);

        Assert.Equal("Hello from node!", response0);
    }

    [Fact]
    [RequiresTools(["npm"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyNpmAppWorks()
    {
        var testProgram = _nodeJsFixture.TestProgram;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        using var npmClient = testProgram.App!.CreateHttpClient(testProgram.NpmAppBuilder!.Resource.Name, "http");
        var response0 = await npmClient.GetStringAsync("/", cts.Token);

        Assert.Equal("Hello from npm!", response0);
    }
}
