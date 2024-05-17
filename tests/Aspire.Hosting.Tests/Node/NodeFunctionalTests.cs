// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
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

    [LocalOnlyFact("node")]
    public async Task VerifyNodeAppWorks()
    {
        var testProgram = _nodeJsFixture.TestProgram;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        using var nodeClient = testProgram.App!.CreateHttpClient(testProgram.NodeAppBuilder!.Resource.Name, "http");
        var response0 = await nodeClient.GetStringAsync("/", cts.Token);

        using var npmClient = testProgram.App!.CreateHttpClient(testProgram.NodeAppBuilder!.Resource.Name, "http");
        var response1 = await npmClient.GetStringAsync("/", cts.Token);

        Assert.Equal("Hello from node!", response0);
        Assert.Equal("Hello from node!", response1);
    }
}
