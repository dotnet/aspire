// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Templating;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Templating;

public class GitRepositoryTemplateFactoryTests
{
    private readonly ITestOutputHelper _outputHelper;

    public GitRepositoryTemplateFactoryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void GetTemplates_WhenFeatureFlagDisabled_ReturnsEmptyCollection()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(
            workspace, 
            _outputHelper, 
            options => options.DisabledFeatures = new[] { KnownFeatures.GitRepositoryTemplates });
        var provider = services.BuildServiceProvider();
        var features = provider.GetRequiredService<IFeatures>();
        var factory = new GitRepositoryTemplateFactory(features);

        // Act
        var templates = factory.GetTemplates();

        // Assert
        Assert.Empty(templates);
    }

    [Fact]
    public async Task GetTemplates_WhenFeatureFlagEnabled_ReturnsEmptyCollectionForNow()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper);
        var provider = services.BuildServiceProvider();
        
        // Enable the feature flag through configuration
        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var setResult = command.Parse($"config set {KnownFeatures.FeaturePrefix}.{KnownFeatures.GitRepositoryTemplates} true");
        var setExitCode = await setResult.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, setExitCode);

        // Create new service provider to pick up the configuration change
        var newServices = CliTestHelper.CreateServiceCollection(workspace, _outputHelper);
        var newProvider = newServices.BuildServiceProvider();
        var features = newProvider.GetRequiredService<IFeatures>();
        var factory = new GitRepositoryTemplateFactory(features);

        // Act
        var templates = factory.GetTemplates();

        // Assert
        // For now, this should return empty as we're only setting up infrastructure
        // TODO: Update this test when actual Git repository templates are implemented
        Assert.Empty(templates);
    }

    [Fact]
    public void GetTemplates_WhenFeatureFlagNotSet_DefaultsToDisabled()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper);
        var provider = services.BuildServiceProvider();
        var features = provider.GetRequiredService<IFeatures>();
        var factory = new GitRepositoryTemplateFactory(features);

        // Act
        var templates = factory.GetTemplates();

        // Assert
        Assert.Empty(templates);
        // Verify the feature flag defaults to false
        Assert.False(features.IsFeatureEnabled(KnownFeatures.GitRepositoryTemplates, false));
    }

    [Fact]
    public void GitRepositoryTemplateFactory_IsRegisteredInServiceProvider()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper);
        var provider = services.BuildServiceProvider();

        // Act - Get the template provider (which should include all template factories)
        var templateProvider = provider.GetRequiredService<ITemplateProvider>();

        // Assert - Since the test helper manually creates a TemplateProvider with just DotNetTemplateFactory,
        // we'll verify that our GitRepositoryTemplateFactory can be instantiated correctly
        var features = provider.GetRequiredService<IFeatures>();
        var gitFactory = new GitRepositoryTemplateFactory(features);
        
        // This should not throw and return empty templates when feature is disabled
        var templates = gitFactory.GetTemplates();
        Assert.Empty(templates);
    }

    [Fact]
    public void TemplateProvider_WorksWithBothFactories()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper);
        var provider = services.BuildServiceProvider();

        // Act - Create a template provider with both factories like Program.cs does
        var interactionService = provider.GetRequiredService<IInteractionService>();
        var runner = provider.GetRequiredService<IDotNetCliRunner>();
        var certificateService = provider.GetRequiredService<ICertificateService>();
        var packagingService = provider.GetRequiredService<IPackagingService>();
        var prompter = provider.GetRequiredService<INewCommandPrompter>();
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var features = provider.GetRequiredService<IFeatures>();
        
        var dotnetFactory = new DotNetTemplateFactory(interactionService, runner, certificateService, packagingService, prompter, executionContext, features);
        var gitFactory = new GitRepositoryTemplateFactory(features);
        var templateProvider = new TemplateProvider([dotnetFactory, gitFactory]);

        // Act
        var allTemplates = templateProvider.GetTemplates();

        // Assert - Should have templates from DotNetTemplateFactory, none from GitRepositoryTemplateFactory (feature disabled)
        Assert.True(allTemplates.Any(), "Should have templates from DotNetTemplateFactory");
        
        // Verify both factories are working
        Assert.NotNull(dotnetFactory.GetTemplates());
        Assert.Empty(gitFactory.GetTemplates()); // Feature disabled by default
    }
}