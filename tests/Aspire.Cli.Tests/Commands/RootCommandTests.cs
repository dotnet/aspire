// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class RootCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task RootCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("--help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }
}
