// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.EndToEnd.Tests;
using Aspire.Playground.Tests;
using Xunit;
using Xunit.Abstractions;

public class MongoDbTests : PlaygroundTestsBase, IClassFixture<MongoPlaygroundAppFixture>
{
    public MongoDbTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [Fact]
    public async Task Simple()
    {
        await Task.CompletedTask;
    }

}
