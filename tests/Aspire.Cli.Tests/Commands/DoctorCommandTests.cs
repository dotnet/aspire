// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class DoctorCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task DoctorCommand_Help_Works()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("doctor --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        
        // Help should return success
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task DoctorCommand_Runs_Successfully()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("doctor");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        
        // Doctor command should complete (exit code 0 or 1 depending on environment)
        // We don't assert specific exit code as it depends on the environment
        Assert.True(exitCode == ExitCodeConstants.Success || exitCode == ExitCodeConstants.InvalidCommand);
    }
}
