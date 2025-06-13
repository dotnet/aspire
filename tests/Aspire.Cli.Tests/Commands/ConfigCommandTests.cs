// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class ConfigCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ConfigSetAndGetWork()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        
        // Set a configuration value
        await configService.SetConfigurationAsync("testKey", "testValue");
        
        // Get the configuration value by rebuilding the service provider to pick up changes
        services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        provider = services.BuildServiceProvider();
        configuration = provider.GetRequiredService<IConfiguration>();
        
        var value = configuration["testKey"];
        
        Assert.Equal("testValue", value);
    }

    [Fact]
    public async Task ConfigCommandSetWithValidArguments()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config set testKey testValue");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the value was set by rebuilding the service provider to pick up changes
        services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var value = configuration["testKey"];
        Assert.Equal("testValue", value);
    }

    [Fact]
    public async Task ConfigCommandGetWithValidKey()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var aspireSettingsDirectory = workspace.CreateDirectory(".aspire");
        var settingsFilePath = Path.Combine(aspireSettingsDirectory.FullName, "settings.json");
        await File.WriteAllTextAsync(settingsFilePath, """{"testKey": "testValue"}""");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config get testKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigCommandGetWithInvalidKey()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config get nonExistentKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigListWhenEmpty()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config list");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigListWithValues()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        await configService.SetConfigurationAsync("key1", "value1");
        await configService.SetConfigurationAsync("key2", "value2");

        // Rebuild service provider to pick up configuration changes
        services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config list");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigDeleteExistingKey()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        await configService.SetConfigurationAsync("testKey", "testValue");

        // Rebuild service provider to pick up configuration changes
        services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config delete testKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the key was deleted by rebuilding service provider
        services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var value = configuration["testKey"];
        Assert.Null(value);
    }

    [Fact]
    public async Task ConfigDeleteNonExistentKey()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config delete nonExistentKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigServiceGetAllConfiguration()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        
        // Initially empty
        var emptyConfig = configuration.AsEnumerable().Where(kvp => !string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value));
        Assert.Empty(emptyConfig);

        // Add some values
        await configService.SetConfigurationAsync("key1", "value1");
        await configService.SetConfigurationAsync("key2", "value2");

        // Rebuild service provider to pick up configuration changes
        services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        provider = services.BuildServiceProvider();
        configuration = provider.GetRequiredService<IConfiguration>();

        var allConfig = configuration.AsEnumerable().Where(kvp => !string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.Equal(2, allConfig.Count);
        Assert.Equal("value1", allConfig["key1"]);
        Assert.Equal("value2", allConfig["key2"]);
    }

    [Fact]
    public async Task ConfigServiceDeleteConfiguration()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        
        // Delete non-existent key returns false
        var deleted = await configService.DeleteConfigurationAsync("nonExistent");
        Assert.False(deleted);

        // Set a value and delete it
        await configService.SetConfigurationAsync("testKey", "testValue");
        deleted = await configService.DeleteConfigurationAsync("testKey");
        Assert.True(deleted);

        // Verify it's gone by rebuilding service provider
        services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var value = configuration["testKey"];
        Assert.Null(value);
    }

    [Fact]
    public async Task ConfigSetGlobalOption()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config set --global testGlobalKey testGlobalValue");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the value was set globally by checking if it exists in global settings
        var globalSettingsPath = Path.Combine(tempRepo.WorkspaceRoot.FullName, ".aspire", "settings.global.json");
        
        if (File.Exists(globalSettingsPath))
        {
            var content = await File.ReadAllTextAsync(globalSettingsPath);
            Assert.Contains("testGlobalKey", content);
            Assert.Contains("testGlobalValue", content);
        }
    }

    [Fact]
    public async Task ConfigDeleteGlobalOption()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        // First set a global value
        var configService = provider.GetRequiredService<IConfigurationService>();
        await configService.SetConfigurationAsync("testGlobalKey", "testGlobalValue", isGlobal: true);

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config delete --global testGlobalKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigSetThenAppHostPathPreservesExistingConfig()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        
        // First, set some configuration value (simulating user doing `aspire config set foo bar`)
        await configService.SetConfigurationAsync("foo", "bar");
        
        // Then simulate what happens when aspire run finds an apphost and sets the appHostPath
        await configService.SetConfigurationAsync("appHostPath", "./TestProject.AppHost.csproj");
        
        // Verify both values are preserved by rebuilding service provider
        services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfiguration>();
        
        // Both values should be present
        Assert.Equal("bar", configuration["foo"]);
        Assert.Equal("./TestProject.AppHost.csproj", configuration["appHostPath"]);
        
        // Also verify the raw file content contains both values
        var settingsPath = Path.Combine(tempRepo.WorkspaceRoot.FullName, ".aspire", "settings.json");
        Assert.True(File.Exists(settingsPath));
        
        var fileContent = await File.ReadAllTextAsync(settingsPath);
        Assert.Contains("\"foo\"", fileContent);
        Assert.Contains("\"bar\"", fileContent);
        Assert.Contains("\"appHostPath\"", fileContent);
        Assert.Contains("./TestProject.AppHost.csproj", fileContent);
    }
}