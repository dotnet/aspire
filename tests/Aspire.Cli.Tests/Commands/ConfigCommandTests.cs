// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class ConfigCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ConfigCommandReturnsInvalidCommandExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("config");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task ConfigSetCommand_WithFlatKey_CreatesSimpleProperty()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("config set foo bar");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the settings file was created correctly
        var settingsPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json");
        Assert.True(File.Exists(settingsPath));

        var json = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonNode.Parse(json)?.AsObject();
        Assert.NotNull(settings);
        Assert.Equal("bar", settings["foo"]?.ToString());
    }

    [Fact]
    public async Task ConfigSetCommand_WithDotNotation_CreatesNestedObject()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("config set foo.bar baz");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the settings file was created correctly
        var settingsPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json");
        Assert.True(File.Exists(settingsPath));

        var json = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonNode.Parse(json)?.AsObject();
        Assert.NotNull(settings);
        Assert.True(settings["foo"] is JsonObject);
        var fooObject = settings["foo"]!.AsObject();
        Assert.Equal("baz", fooObject["bar"]?.ToString());
    }

    [Fact]
    public async Task ConfigSetCommand_WithDeepDotNotation_CreatesDeeplyNestedObject()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("config set foo.bar.baz hello");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify the settings file was created correctly
        var settingsPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json");
        Assert.True(File.Exists(settingsPath));

        var json = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonNode.Parse(json)?.AsObject();
        Assert.NotNull(settings);
        
        Assert.True(settings["foo"] is JsonObject);
        var fooObject = settings["foo"]!.AsObject();
        Assert.True(fooObject["bar"] is JsonObject);
        var barObject = fooObject["bar"]!.AsObject();
        Assert.Equal("hello", barObject["baz"]?.ToString());
    }

    [Fact]
    public async Task ConfigSetCommand_ReplacesPrimitiveWithObject()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        
        // First set a primitive value
        var result1 = command.Parse("config set foo primitive");
        var exitCode1 = await result1.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode1);

        // Then set a nested value that should replace the primitive
        var result2 = command.Parse("config set foo.bar nested");
        var exitCode2 = await result2.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode2);

        // Verify the primitive was replaced with an object
        var settingsPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json");
        var json = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonNode.Parse(json)?.AsObject();
        Assert.NotNull(settings);
        
        Assert.True(settings["foo"] is JsonObject);
        var fooObject = settings["foo"]!.AsObject();
        Assert.Equal("nested", fooObject["bar"]?.ToString());
    }

    [Fact]
    public async Task ConfigGetCommand_WithFlatKey_ReturnsValue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services1 = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider1 = services1.BuildServiceProvider();

        var command1 = provider1.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // First set a value
        var setResult = command1.Parse("config set testkey testvalue");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        // Create a new service collection to reload config.
        var services2 = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider2 = services2.BuildServiceProvider();

        var command2 = provider2.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // Then get the value
        var getResult = command2.Parse("config get testkey");
        var getExitCode = await getResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, getExitCode);
    }

    [Fact]
    public async Task ConfigGetCommand_WithDotNotation_ReturnsNestedValue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services1 = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider1 = services1.BuildServiceProvider();

        var command1 = provider1.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // First set a nested value
        var setResult = command1.Parse("config set level1.level2.level3 nestedvalue");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        var services2 = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider2 = services2.BuildServiceProvider();

        var command2 = provider2.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // Then get the nested value
        var getResult = command2.Parse("config get level1.level2.level3");
        var getExitCode = await getResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, getExitCode);
    }

    [Fact]
    public async Task ConfigGetCommand_WithNonExistentKey_ReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("config get nonexistent.key");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(10, exitCode);
    }

    [Fact]
    public async Task ConfigDeleteCommand_WithFlatKey_RemovesValue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // First set a value
        var setResult = command.Parse("config set deletekey deletevalue");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        // Then delete the value
        var deleteResult = command.Parse("config delete deletekey");
        var deleteExitCode = await deleteResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, deleteExitCode);

        // Verify it's deleted
        var getResult = command.Parse("config get deletekey");
        var getExitCode = await getResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(10, getExitCode); // Should return error for missing key
    }

    [Fact]
    public async Task ConfigDeleteCommand_CleansUpEmptyParentObjects()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // Set a deeply nested value
        var setResult = command.Parse("config set deep.nested.value test");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        // Delete the nested value
        var deleteResult = command.Parse("config delete deep.nested.value");
        var deleteExitCode = await deleteResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, deleteExitCode);

        // Verify the entire deep.nested structure is cleaned up
        var settingsPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json");
        var json = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonNode.Parse(json)?.AsObject();
        Assert.NotNull(settings);
        
        // The deep object should be completely removed since it became empty
        Assert.False(settings.ContainsKey("deep"));
    }

    [Fact]
    public async Task ConfigShowCommand_ShowsMergedJsonConfiguration()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // Set various values
        var setResult1 = command.Parse("config set flatkey flatvalue");
        var setExitCode1 = await setResult1.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode1);

        var setResult2 = command.Parse("config set nested.key nestedvalue");
        var setExitCode2 = await setResult2.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode2);

        var setResult3 = command.Parse("config set deep.nested.key deepvalue");
        var setExitCode3 = await setResult3.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode3);

        // Show all configuration as JSON
        var showResult = command.Parse("config show");
        var showExitCode = await showResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, showExitCode);
    }

    [Fact]
    public async Task ConfigShowCommand_WithUglyOption_OutputsPlainJson()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // Set a test value
        var setResult = command.Parse("config set testkey testvalue");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        // Show configuration with --ugly option
        var showResult = command.Parse("config show --ugly");
        var showExitCode = await showResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, showExitCode);
    }

    [Fact]
    public async Task ConfigShowCommand_MergesGlobalAndLocalSettings()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();

        // Set a local value
        var setLocalResult = command.Parse("config set local.key localvalue");
        var setLocalExitCode = await setLocalResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setLocalExitCode);

        // Set a global value
        var setGlobalResult = command.Parse("config set global.key globalvalue --global");
        var setGlobalExitCode = await setGlobalResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setGlobalExitCode);

        // Set a value that should be overridden (global overrides local)
        var setOverrideLocalResult = command.Parse("config set override.key localvalue");
        var setOverrideLocalExitCode = await setOverrideLocalResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setOverrideLocalExitCode);

        var setOverrideGlobalResult = command.Parse("config set override.key globalvalue --global");
        var setOverrideGlobalExitCode = await setOverrideGlobalResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setOverrideGlobalExitCode);

        // Verify merged configuration via new API
        var configService = provider.GetRequiredService<IConfigurationService>();
        var mergedConfig = await configService.GetMergedConfigurationAsync(CancellationToken.None);
        
        Assert.NotNull(mergedConfig);
        Assert.Equal("localvalue", mergedConfig["local"]?["key"]?.ToString());
        Assert.Equal("globalvalue", mergedConfig["global"]?["key"]?.ToString());
        Assert.Equal("globalvalue", mergedConfig["override"]?["key"]?.ToString()); // Global should override local
    }

    [Fact]
    public async Task FeatureFlags_WhenSetToTrue_ReturnsTrue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(
            workspace,
            outputHelper,
            options => options.EnabledFeatures = new[] { "testFeature" }
            );
        var provider = services.BuildServiceProvider();

        // Set the feature flag to true
        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var setResult = command.Parse($"config set {KnownFeatures.FeaturePrefix}.testFeature true");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        // Check the feature flag
        var featureFlags = provider.GetRequiredService<IFeatures>();
        Assert.True(featureFlags.IsFeatureEnabled("testFeature", defaultValue: false));
    }

    [Fact]
    public async Task FeatureFlags_WhenSetToFalse_ReturnsFalse()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => options.DisabledFeatures = new[] { "testFeature" });
        var provider = services.BuildServiceProvider();

        // Set the feature flag to false
        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var setResult = command.Parse($"config set {KnownFeatures.FeaturePrefix}.testFeature false");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        // Check the feature flag
        var featureFlags = provider.GetRequiredService<IFeatures>();
        Assert.False(featureFlags.IsFeatureEnabled("testFeature", defaultValue: true));
    }

    [Fact]
    public async Task FeatureFlags_WhenSetToInvalidValue_ReturnsFalse()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(
            workspace,
            outputHelper,
            options => options.ConfigurationCallback += confing =>
            {
                confing[$"{KnownFeatures.FeaturePrefix}:testFeature"] = "invalid"; // Set an invalid value
            });
        var provider = services.BuildServiceProvider();

        // Set the feature flag to an invalid value
        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var setResult = command.Parse($"config set {KnownFeatures.FeaturePrefix}.testFeature invalid");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        // Check the feature flag
        var featureFlags = provider.GetRequiredService<IFeatures>();
        Assert.False(featureFlags.IsFeatureEnabled("testFeature", defaultValue: true));
    }

    [Fact]
    public void DeployCommand_IsAlwaysAvailable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        
        // Check that deploy command is always available
        var hasDeployCommand = rootCommand.Subcommands.Any(cmd => cmd.Name == "deploy");
        Assert.True(hasDeployCommand);
    }
}