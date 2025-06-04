// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class ConfigCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ConfigSetAndGetWork()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        
        // Set a configuration value
        await configService.SetConfigurationAsync("testKey", "testValue");
        
        // Get the configuration value
        var value = configService.GetConfiguration("testKey");
        
        Assert.Equal("testValue", value);
    }

    [Fact]
    public async Task ConfigCommandSetWithValidArguments()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<ConfigCommand>();
        var result = command.Parse("set testKey testValue");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the value was set
        var configService = provider.GetRequiredService<IConfigurationService>();
        var value = configService.GetConfiguration("testKey");
        Assert.Equal("testValue", value);
    }

    [Fact]
    public async Task ConfigCommandGetWithValidKey()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        await configService.SetConfigurationAsync("testKey", "testValue");

        var command = provider.GetRequiredService<ConfigCommand>();
        var result = command.Parse("get testKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigCommandGetWithInvalidKey()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<ConfigCommand>();
        var result = command.Parse("get nonExistentKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<ConfigCommand>();
        var result = command.Parse("--help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }
}