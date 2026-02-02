// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.InternalTesting;

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

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RootCommandWithNoLogoArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("--nologo --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("True", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public async Task NoLogoEnvironmentVariable_ParsedCorrectly(string? value, bool expectedNoLogo)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ConfigurationCallback = config =>
            {
                config[CliConfigNames.NoLogo] = value;
            };
        });
        var provider = services.BuildServiceProvider();

        var configuration = provider.GetRequiredService<IConfiguration>();
        var isNoLogo = configuration.GetBool(CliConfigNames.NoLogo, defaultValue: false);

        Assert.Equal(expectedNoLogo, isNoLogo);

        // Also verify command still works with the configuration
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("--help");
        var exitCode = await result.InvokeAsync().DefaultTimeout();
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void FirstTimeUseNotice_DisplayedWhenSentinelDoesNotExist()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
        });
        var provider = services.BuildServiceProvider();

        Program.DisplayFirstTimeUseNoticeIfNeeded(provider, noLogo: false);

        var errorOutput = errorWriter.ToString();
        var lines = errorOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(lines, line => line.EndsWith(RootCommandStrings.FirstTimeUseWelcome, StringComparison.Ordinal));
        Assert.True(sentinel.WasCreated);
    }

    [Fact]
    public void FirstTimeUseNotice_NotDisplayedWhenSentinelExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = true };

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
        });
        var provider = services.BuildServiceProvider();

        Program.DisplayFirstTimeUseNoticeIfNeeded(provider, noLogo: false);

        var errorOutput = errorWriter.ToString();
        var lines = errorOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.DoesNotContain(lines, line => line.EndsWith(RootCommandStrings.FirstTimeUseWelcome, StringComparison.Ordinal));
        Assert.False(sentinel.WasCreated);
    }

    [Fact]
    public void FirstTimeUseNotice_NotDisplayedWithNoLogoArgument()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
        });
        var provider = services.BuildServiceProvider();

        Program.DisplayFirstTimeUseNoticeIfNeeded(provider, noLogo: true);

        var errorOutput = errorWriter.ToString();
        var lines = errorOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.DoesNotContain(lines, line => line.EndsWith(RootCommandStrings.FirstTimeUseWelcome, StringComparison.Ordinal));
        Assert.True(sentinel.WasCreated);
    }

    [Fact]
    public void FirstTimeUseNotice_NotDisplayedWithNoLogoEnvironmentVariable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.ConfigurationCallback = config =>
            {
                config[CliConfigNames.NoLogo] = "1";
            };
        });
        var provider = services.BuildServiceProvider();

        var configuration = provider.GetRequiredService<IConfiguration>();
        var noLogo = configuration.GetBool(CliConfigNames.NoLogo, defaultValue: false);

        Program.DisplayFirstTimeUseNoticeIfNeeded(provider, noLogo);

        var errorOutput = errorWriter.ToString();
        var lines = errorOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.DoesNotContain(lines, line => line.EndsWith(RootCommandStrings.FirstTimeUseWelcome, StringComparison.Ordinal));
        Assert.True(sentinel.WasCreated);
    }
}
