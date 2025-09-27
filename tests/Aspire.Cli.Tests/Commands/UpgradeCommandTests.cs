// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using RootCommand = Aspire.Cli.Commands.RootCommand;

namespace Aspire.Cli.Tests.Commands;

public class UpgradeCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void UpgradeCommandCanBeResolved()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var upgradeCommand = provider.GetRequiredService<UpgradeCommand>();
        Assert.NotNull(upgradeCommand);
        Assert.Equal("upgrade", upgradeCommand.Name);
    }

    [Fact]
    public async Task UpgradeCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var result = rootCommand.Parse("upgrade --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void UpgradeCommandIsAddedToRootCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var upgradeSubcommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "upgrade");
        
        Assert.NotNull(upgradeSubcommand);
        Assert.IsType<UpgradeCommand>(upgradeSubcommand);
    }

    [Fact]
    public void UpgradeCommandHasExpectedOptions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var upgradeCommand = provider.GetRequiredService<UpgradeCommand>();
        
        var prereleaseOption = upgradeCommand.Options.FirstOrDefault(o => o.Name == "--prerelease");
        var versionOption = upgradeCommand.Options.FirstOrDefault(o => o.Name == "--version");
        
        Assert.NotNull(prereleaseOption);
        Assert.NotNull(versionOption);
    }
}