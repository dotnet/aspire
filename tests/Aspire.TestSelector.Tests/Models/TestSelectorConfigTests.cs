// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Models;
using Xunit;

namespace Aspire.TestSelector.Tests.Models;

public class TestSelectorConfigTests
{
    [Fact]
    public void LoadFromJson_ValidConfig_ParsesAllProperties()
    {
        var json = """
        {
            "$schema": "https://example.com/schema.json",
            "ignorePaths": ["**/*.md", "docs/**"],
            "triggerAllPaths": ["eng/**", "Directory.Build.props"],
            "triggerAllExclude": ["eng/pipelines/**"],
            "nonDotNetRules": [
                {
                    "pattern": "src/Aspire.Dashboard/**",
                    "category": "dashboard"
                }
            ],
            "categories": {
                "integrations": {
                    "description": "Integration tests",
                    "testProjects": "auto"
                },
                "dashboard": {
                    "description": "Dashboard tests",
                    "testProjects": ["tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj"]
                }
            }
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Equal("https://example.com/schema.json", config.Schema);
        Assert.Equal(2, config.IgnorePaths.Count);
        Assert.Contains("**/*.md", config.IgnorePaths);
        Assert.Equal(2, config.TriggerAllPaths.Count);
        Assert.Single(config.TriggerAllExclude);
        Assert.Single(config.NonDotNetRules);
        Assert.Equal("src/Aspire.Dashboard/**", config.NonDotNetRules[0].Pattern);
        Assert.Equal("dashboard", config.NonDotNetRules[0].Category);
        Assert.Equal(2, config.Categories.Count);
        Assert.True(config.Categories["integrations"].TestProjects.IsAuto);
        Assert.False(config.Categories["dashboard"].TestProjects.IsAuto);
    }

    [Fact]
    public void LoadFromJson_CaseInsensitivePropertyNames()
    {
        var json = """
        {
            "IGNOREPATHS": ["**/*.md"],
            "TriggerAllPaths": ["eng/**"],
            "triggerallexclude": [],
            "Categories": {}
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Single(config.IgnorePaths);
        Assert.Single(config.TriggerAllPaths);
    }

    [Fact]
    public void LoadFromJson_ToleratesCommentsAndTrailingCommas()
    {
        var json = """
        {
            // This is a comment
            "ignorePaths": [
                "**/*.md",
                "docs/**", // trailing comma
            ],
            "triggerAllPaths": ["eng/**"],
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Equal(2, config.IgnorePaths.Count);
    }

    [Fact]
    public void LoadFromJson_InvalidJson_ThrowsException()
    {
        var json = "{ invalid json }";

        Assert.ThrowsAny<Exception>(() => TestSelectorConfig.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_EmptyObject_ReturnsDefaults()
    {
        var json = "{}";

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Empty(config.IgnorePaths);
        Assert.Empty(config.TriggerAllPaths);
        Assert.Empty(config.TriggerAllExclude);
        Assert.Empty(config.NonDotNetRules);
        Assert.Empty(config.Categories);
        Assert.Null(config.Schema);
    }

    [Fact]
    public void LoadFromJson_NullJson_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => TestSelectorConfig.LoadFromJson(null!));
    }

    [Fact]
    public void LoadFromJson_NestedCategories_ParsesCorrectly()
    {
        var json = """
        {
            "categories": {
                "cli": {
                    "description": "CLI tests",
                    "testProjects": [
                        "tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj",
                        "tests/Aspire.Cli.EndToEnd.Tests/Aspire.Cli.EndToEnd.Tests.csproj"
                    ]
                }
            }
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Single(config.Categories);
        var cliCategory = config.Categories["cli"];
        Assert.Equal("CLI tests", cliCategory.Description);
        Assert.False(cliCategory.TestProjects.IsAuto);
        Assert.Equal(2, cliCategory.TestProjects.Projects.Count);
    }

    [Fact]
    public void LoadFromJson_NonDotNetRulesMultiple_ParsesAll()
    {
        var json = """
        {
            "nonDotNetRules": [
                {"pattern": "src/Dashboard/**", "category": "dashboard"},
                {"pattern": "**/*.js", "category": "frontend"},
                {"pattern": "**/*.yml", "category": "ci"}
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Equal(3, config.NonDotNetRules.Count);
        Assert.Equal("dashboard", config.NonDotNetRules[0].Category);
        Assert.Equal("frontend", config.NonDotNetRules[1].Category);
        Assert.Equal("ci", config.NonDotNetRules[2].Category);
    }
}
