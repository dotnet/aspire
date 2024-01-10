// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
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
        var client = _nodeJsFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response0 = await testProgram.NodeAppBuilder!.HttpGetStringWithRetryAsync(client, "http", "/", cts.Token);
        var response1 = await testProgram.NpmAppBuilder!.HttpGetStringWithRetryAsync(client, "http", "/", cts.Token);

        Assert.Equal("Hello from node!", response0);
        Assert.Equal("Hello from node!", response1);
    }
}
