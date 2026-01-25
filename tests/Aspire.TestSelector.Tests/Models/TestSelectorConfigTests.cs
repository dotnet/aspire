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
            "projectMappings": [
                {
                    "sourcePattern": "src/Components/{name}/**",
                    "testPattern": "tests/{name}.Tests/",
                    "exclude": ["src/Components/Internal/**"]
                }
            ],
            "categories": {
                "core": {
                    "description": "Critical paths",
                    "triggerAll": true,
                    "triggerPaths": ["global.json", "Directory.Build.props"]
                },
                "integrations": {
                    "description": "Integration tests",
                    "triggerPaths": ["src/**"],
                    "excludePaths": ["src/Aspire.Cli/**"]
                }
            }
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Equal("https://example.com/schema.json", config.Schema);
        Assert.Equal(2, config.IgnorePaths.Count);
        Assert.Contains("**/*.md", config.IgnorePaths);
        Assert.Single(config.ProjectMappings);
        Assert.Equal("src/Components/{name}/**", config.ProjectMappings[0].SourcePattern);
        Assert.Equal("tests/{name}.Tests/", config.ProjectMappings[0].TestPattern);
        Assert.Single(config.ProjectMappings[0].Exclude);
        Assert.Equal(2, config.Categories.Count);
        Assert.True(config.Categories["core"].TriggerAll);
        Assert.False(config.Categories["integrations"].TriggerAll);
    }

    [Fact]
    public void LoadFromJson_CaseInsensitivePropertyNames()
    {
        var json = """
        {
            "IGNOREPATHS": ["**/*.md"],
            "Categories": {
                "test": {
                    "TRIGGERPATHS": ["src/**"]
                }
            }
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Single(config.IgnorePaths);
        Assert.Single(config.Categories);
        Assert.Single(config.Categories["test"].TriggerPaths);
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
            "categories": {},
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
        Assert.Empty(config.ProjectMappings);
        Assert.Empty(config.Categories);
        Assert.Null(config.Schema);
    }

    [Fact]
    public void LoadFromJson_NullJson_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => TestSelectorConfig.LoadFromJson(null!));
    }

    [Fact]
    public void LoadFromJson_CategoryWithTriggerAll_ParsesCorrectly()
    {
        var json = """
        {
            "categories": {
                "core": {
                    "description": "Critical paths",
                    "triggerAll": true,
                    "triggerPaths": [
                        "global.json",
                        "Directory.Build.props"
                    ]
                }
            }
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Single(config.Categories);
        var coreCategory = config.Categories["core"];
        Assert.Equal("Critical paths", coreCategory.Description);
        Assert.True(coreCategory.TriggerAll);
        Assert.Equal(2, coreCategory.TriggerPaths.Count);
    }

    [Fact]
    public void LoadFromJson_CategoryWithExcludePaths_ParsesCorrectly()
    {
        var json = """
        {
            "categories": {
                "integrations": {
                    "triggerPaths": ["src/**", "tests/**"],
                    "excludePaths": ["src/Aspire.Cli/**", "tests/E2E/**"]
                }
            }
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        var category = config.Categories["integrations"];
        Assert.Equal(2, category.TriggerPaths.Count);
        Assert.Equal(2, category.ExcludePaths.Count);
        Assert.Contains("src/Aspire.Cli/**", category.ExcludePaths);
    }

    [Fact]
    public void LoadFromJson_ProjectMappingsMultiple_ParsesAll()
    {
        var json = """
        {
            "projectMappings": [
                {"sourcePattern": "src/Components/{name}/**", "testPattern": "tests/{name}.Tests/"},
                {"sourcePattern": "src/Aspire.Hosting.{name}/**", "testPattern": "tests/Aspire.Hosting.{name}.Tests/"},
                {"sourcePattern": "tests/{name}.Tests/**", "testPattern": "tests/{name}.Tests/"}
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        Assert.Equal(3, config.ProjectMappings.Count);
        Assert.Equal("tests/{name}.Tests/", config.ProjectMappings[0].TestPattern);
        Assert.Equal("tests/Aspire.Hosting.{name}.Tests/", config.ProjectMappings[1].TestPattern);
    }

    [Fact]
    public void LoadFromJson_ProjectMappingWithExclude_ParsesCorrectly()
    {
        var json = """
        {
            "projectMappings": [
                {
                    "sourcePattern": "src/Aspire.Hosting.{name}/**",
                    "testPattern": "tests/Aspire.Hosting.{name}.Tests/",
                    "exclude": ["src/Aspire.Hosting.Testing/**", "src/Aspire.Hosting.Internal/**"]
                }
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        var mapping = config.ProjectMappings[0];
        Assert.Equal(2, mapping.Exclude.Count);
        Assert.Contains("src/Aspire.Hosting.Testing/**", mapping.Exclude);
    }

    [Fact]
    public void LoadFromJson_CategoryDefaults_AreCorrect()
    {
        var json = """
        {
            "categories": {
                "minimal": {}
            }
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(json);

        var category = config.Categories["minimal"];
        Assert.Null(category.Description);
        Assert.False(category.TriggerAll);
        Assert.Empty(category.TriggerPaths);
        Assert.Empty(category.ExcludePaths);
    }
}
