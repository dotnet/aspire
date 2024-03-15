// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using Aspire.Workload.Tests;

namespace Aspire.EndToEnd.Tests;

public class HostingTestingTests
{
    // private readonly IntegrationServicesFixture _integrationServicesFixture;
    private readonly TestOutputWrapper _testOutput;

    public HostingTestingTests(ITestOutputHelper testOutput)
    {
        _testOutput = new TestOutputWrapper(testOutput);
    }

    [Fact]
    public async Task AspireHostingTestingTestsAsync()
    {
        HostingTestsRunner runner = new(_testOutput, "Aspire.Hosting.Testing.Tests");
        await runner.InitializeAsync();
        _testOutput.WriteLine($".. back from InitializeAsync");
        await runner.DisposeAsync();
    }
}
