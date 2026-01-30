// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task RootCommandWithNoLogoArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("--nologo --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
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
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task FirstTimeUseNotice_BannerDisplayedWhenSentinelDoesNotExist()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false, showBanner: false);

        Assert.True(bannerService.WasBannerDisplayed);
        Assert.True(sentinel.WasCreated);
    }

    [Fact]
    public async Task FirstTimeUseNotice_BannerNotDisplayedWhenSentinelExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = true };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false, showBanner: false);

        Assert.False(bannerService.WasBannerDisplayed);
        Assert.False(sentinel.WasCreated);
    }

    [Fact]
    public async Task FirstTimeUseNotice_BannerNotDisplayedWithNoLogoArgument()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: true, showBanner: false);

        Assert.False(bannerService.WasBannerDisplayed);
        Assert.True(sentinel.WasCreated);
    }

    [Fact]
    public async Task FirstTimeUseNotice_BannerNotDisplayedWithNoLogoEnvironmentVariable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
            options.ConfigurationCallback = config =>
            {
                config[CliConfigNames.NoLogo] = "1";
            };
        });
        var provider = services.BuildServiceProvider();

        var configuration = provider.GetRequiredService<IConfiguration>();
        var noLogo = configuration.GetBool(CliConfigNames.NoLogo, defaultValue: false);

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo, showBanner: false);

        Assert.False(bannerService.WasBannerDisplayed);
        Assert.True(sentinel.WasCreated);
    }

    [Fact]
    public async Task Banner_DisplayedWhenExplicitlyRequested()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = true }; // Already seen first run
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false, showBanner: true);

        Assert.True(bannerService.WasBannerDisplayed);
        Assert.False(sentinel.WasCreated); // Should not create sentinel when explicitly requested
    }

    [Fact]
    public async Task Banner_DisplayedWhenExplicitlyRequestedEvenWithNoLogo()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = true };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: true, showBanner: true);

        Assert.True(bannerService.WasBannerDisplayed);
    }

    [Fact]
    public async Task Banner_TelemetryNoticeNotDisplayedWhenExplicitlyRequested()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = true };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false, showBanner: true);

        // Telemetry notice should NOT be displayed when banner is explicitly requested
        // (only on first run)
        var errorOutput = errorWriter.ToString();
        Assert.DoesNotContain("Telemetry", errorOutput);
    }
}
