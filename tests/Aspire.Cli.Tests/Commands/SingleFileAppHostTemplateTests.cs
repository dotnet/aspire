// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class SingleFileAppHostTemplateTests
{
    [Fact]
    public void GetTemplates_WhenSingleFileAppHostFeatureDisabled_DoesNotIncludeSingleFileTemplate()
    {
        // Arrange
        var factory = new Aspire.Cli.Templating.DotNetTemplateFactory(
            interactionService: null!,
            runner: null!,
            certificateService: null!,
            packagingService: null!,
            prompter: null!,
            executionContext: null!,
            features: new TestFeatures(singleFileAppHostEnabled: false));

        // Act
        var templates = factory.GetTemplates().ToList();

        // Assert
        Assert.DoesNotContain(templates, t => t.Name == "aspire-apphost-singlefile");
    }

    [Fact]
    public void GetTemplates_WhenSingleFileAppHostFeatureEnabled_IncludesSingleFileTemplate()
    {
        // Arrange
        var factory = new Aspire.Cli.Templating.DotNetTemplateFactory(
            interactionService: null!,
            runner: null!,
            certificateService: null!,
            packagingService: null!,
            prompter: null!,
            executionContext: null!,
            features: new TestFeatures(singleFileAppHostEnabled: true));

        // Act
        var templates = factory.GetTemplates().ToList();

        // Assert
        Assert.Contains(templates, t => t.Name == "aspire-apphost-singlefile");
    }

    private sealed class TestFeatures : Aspire.Cli.Configuration.IFeatures
    {
        private readonly bool _singleFileAppHostEnabled;

        public TestFeatures(bool singleFileAppHostEnabled)
        {
            _singleFileAppHostEnabled = singleFileAppHostEnabled;
        }

        public bool IsFeatureEnabled(string featureFlag, bool defaultValue)
        {
            if (featureFlag == Aspire.Cli.KnownFeatures.SingleFileAppHostEnabled)
            {
                return _singleFileAppHostEnabled;
            }
            
            return defaultValue;
        }
    }
}