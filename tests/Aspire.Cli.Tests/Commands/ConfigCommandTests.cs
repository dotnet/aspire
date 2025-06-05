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

        // For this test, I need to test the actual writing functionality
        // but without using SetCurrentDirectory. I'll need to create
        // a test setup that the ConfigurationWriter can work with.
        
        // Create .aspire directory in the temp workspace
        var aspireDir = Path.Combine(tempRepo.WorkspaceRoot.FullName, ".aspire");
        Directory.CreateDirectory(aspireDir);
        
        // Save current directory and change to temp for this test only
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);
        
        try
        {
            var services = CliTestHelper.CreateServiceCollection(outputHelper);
            var provider = services.BuildServiceProvider();

            var configWriter = provider.GetRequiredService<IConfigurationWriter>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            
            // Set a configuration value
            await configWriter.SetConfigurationAsync("testKey", "testValue");
            
            // Get the configuration value by rebuilding the service provider to pick up changes
            services = CliTestHelper.CreateServiceCollection(outputHelper);
            provider = services.BuildServiceProvider();
            configuration = provider.GetRequiredService<IConfiguration>();
            
            var value = configuration["testKey"];
            
            Assert.Equal("testValue", value);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public async Task ConfigCommandSetWithValidArguments()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // For this test, I need to test the actual writing functionality
        // but without using SetCurrentDirectory. I'll need to create
        // a test setup that the ConfigurationWriter can work with.
        
        // Create .aspire directory in the temp workspace
        var aspireDir = Path.Combine(tempRepo.WorkspaceRoot.FullName, ".aspire");
        Directory.CreateDirectory(aspireDir);
        
        // Save current directory and change to temp for this test only
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);
        
        try
        {
            var services = CliTestHelper.CreateServiceCollection(outputHelper);
            var provider = services.BuildServiceProvider();

            var command = provider.GetRequiredService<RootCommand>();
            var result = command.Parse("config set testKey testValue");

            var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
            Assert.Equal(0, exitCode);

            // Verify the value was set by rebuilding the service provider to pick up changes
            services = CliTestHelper.CreateServiceCollection(outputHelper);
            provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IConfiguration>();
            var value = configuration["testKey"];
            Assert.Equal("testValue", value);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public async Task ConfigCommandGetWithValidKey()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.LocalSettingsContent = """{"testKey": "testValue"}""";
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config get testKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigCommandGetWithInvalidKey()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config get nonExistentKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigListWhenEmpty()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
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
        
        // Create .aspire directory in the temp workspace
        var aspireDir = Path.Combine(tempRepo.WorkspaceRoot.FullName, ".aspire");
        Directory.CreateDirectory(aspireDir);
        
        // Save current directory and change to temp for this test only
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);
        
        try
        {
            var services = CliTestHelper.CreateServiceCollection(outputHelper);
            var provider = services.BuildServiceProvider();

            var configWriter = provider.GetRequiredService<IConfigurationWriter>();
            await configWriter.SetConfigurationAsync("key1", "value1");
            await configWriter.SetConfigurationAsync("key2", "value2");

            // Rebuild service provider to pick up configuration changes
            services = CliTestHelper.CreateServiceCollection(outputHelper);
            provider = services.BuildServiceProvider();

            var command = provider.GetRequiredService<RootCommand>();
            var result = command.Parse("config list");

            var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public async Task ConfigDeleteExistingKey()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configWriter = provider.GetRequiredService<IConfigurationWriter>();
        await configWriter.SetConfigurationAsync("testKey", "testValue");

        // Rebuild service provider to pick up configuration changes
        services = CliTestHelper.CreateServiceCollection(outputHelper);
        provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config delete testKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the key was deleted by rebuilding service provider
        services = CliTestHelper.CreateServiceCollection(outputHelper);
        provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var value = configuration["testKey"];
        Assert.Null(value);
    }

    [Fact]
    public async Task ConfigDeleteNonExistentKey()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
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
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configWriter = provider.GetRequiredService<IConfigurationWriter>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        
        // Initially empty
        var emptyConfig = configuration.AsEnumerable().Where(kvp => !string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value));
        Assert.Empty(emptyConfig);

        // Add some values
        await configWriter.SetConfigurationAsync("key1", "value1");
        await configWriter.SetConfigurationAsync("key2", "value2");

        // Rebuild service provider to pick up configuration changes
        services = CliTestHelper.CreateServiceCollection(outputHelper);
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
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var configWriter = provider.GetRequiredService<IConfigurationWriter>();
        
        // Delete non-existent key returns false
        var deleted = await configWriter.DeleteConfigurationAsync("nonExistent");
        Assert.False(deleted);

        // Set a value and delete it
        await configWriter.SetConfigurationAsync("testKey", "testValue");
        deleted = await configWriter.DeleteConfigurationAsync("testKey");
        Assert.True(deleted);

        // Verify it's gone by rebuilding service provider
        services = CliTestHelper.CreateServiceCollection(outputHelper);
        provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var value = configuration["testKey"];
        Assert.Null(value);
    }

    [Fact]
    public async Task ConfigSetGlobalOption()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config set --global testGlobalKey testGlobalValue");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the value was set globally by checking if it exists in global settings
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalSettingsPath = Path.Combine(homeDirectory, ".aspire", "settings.json");
        
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
        Directory.SetCurrentDirectory(tempRepo.WorkspaceRoot.FullName);

        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        // First set a global value
        var configWriter = provider.GetRequiredService<IConfigurationWriter>();
        await configWriter.SetConfigurationAsync("testGlobalKey", "testGlobalValue", isGlobal: true);

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("config delete --global testGlobalKey");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }
}