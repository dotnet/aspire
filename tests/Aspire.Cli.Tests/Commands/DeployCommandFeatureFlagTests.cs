// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class DeployCommandFeatureFlagTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void DeployCommand_IsNotAvailableByDefault()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // Check that deploy command is not available by default (feature flag disabled)
        var hasDeployCommand = rootCommand.Subcommands.Any(cmd => cmd.Name == "deploy");
        Assert.False(hasDeployCommand);
    }

    [Fact]
    public void DeployCommand_IsAvailableWhenFeatureFlagEnabled()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.EnabledFeatures = [Aspire.Cli.KnownFeatures.DeployCommandEnabled];
        });
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // Check that deploy command is available when feature flag is enabled
        var hasDeployCommand = rootCommand.Subcommands.Any(cmd => cmd.Name == "deploy");
        Assert.True(hasDeployCommand);
    }
}