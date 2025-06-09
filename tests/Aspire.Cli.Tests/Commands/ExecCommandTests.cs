// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class ExecCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ExecCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("exec --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ExecCommandInteractiveFlowSmokeTest()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options => {
            options.ProjectLocatorFactory = _ => new TestProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<ExecCommand>();
        var result = command.Parse("exec --resource MyApp1 --command \"dotnet ef migrations add Initial\"");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }
}
