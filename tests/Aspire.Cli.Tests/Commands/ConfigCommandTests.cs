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

    [Fact]
    public async Task ConfigListWhenEmpty()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<ConfigCommand>();
        var result = command.Parse("list");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigListWithValues()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        await configService.SetConfigurationAsync("key1", "value1");
        await configService.SetConfigurationAsync("key2", "value2");

        var command = provider.GetRequiredService<ConfigCommand>();
        var result = command.Parse("list");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigDeleteExistingKey()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        await configService.SetConfigurationAsync("testKey", "testValue");

        var command = provider.GetRequiredService<ConfigCommand>();
        var result = command.Parse("delete testKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the key was deleted
        var value = configService.GetConfiguration("testKey");
        Assert.Null(value);
    }

    [Fact]
    public async Task ConfigDeleteNonExistentKey()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<ConfigCommand>();
        var result = command.Parse("delete nonExistentKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigServiceGetAllConfiguration()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        
        // Initially empty
        var emptyConfig = configService.GetAllConfiguration();
        Assert.Empty(emptyConfig);

        // Add some values
        await configService.SetConfigurationAsync("key1", "value1");
        await configService.SetConfigurationAsync("key2", "value2");

        var allConfig = configService.GetAllConfiguration();
        Assert.Equal(2, allConfig.Count);
        Assert.Equal("value1", allConfig["key1"]);
        Assert.Equal("value2", allConfig["key2"]);
    }

    [Fact]
    public async Task ConfigServiceDeleteConfiguration()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        
        // Delete non-existent key returns false
        var deleted = await configService.DeleteConfigurationAsync("nonExistent");
        Assert.False(deleted);

        // Set a value and delete it
        await configService.SetConfigurationAsync("testKey", "testValue");
        deleted = await configService.DeleteConfigurationAsync("testKey");
        Assert.True(deleted);

        // Verify it's gone
        var value = configService.GetConfiguration("testKey");
        Assert.Null(value);
    }
}