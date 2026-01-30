// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Analyzers;
using Aspire.TestSelector.Models;
using Xunit;

namespace Aspire.TestSelector.Tests.Integration;

/// <summary>
/// End-to-end integration tests for the test selection feature.
/// Tests the full evaluation workflow using inline JSON configs.
/// </summary>
public class EndToEndEvaluationTests
{
    #region Full Workflow Tests

    [Fact]
    public void Evaluate_FullWorkflow_AllAnalyzers()
    {
        var configJson = """
        {
            "ignorePaths": ["**/*.md", "docs/**", ".github/**"],
            "triggerAllPaths": ["global.json", "Directory.Build.props", "*.slnx"],
            "categories": {
                "integrations": {
                    "description": "Integration tests",
                    "triggerPaths": ["src/**", "tests/**"],
                    "excludePaths": ["src/Aspire.Cli/**"]
                },
                "cli_e2e": {
                    "description": "CLI end-to-end tests",
                    "triggerPaths": ["src/Aspire.Cli/**", "tests/Aspire.Cli.EndToEnd.Tests/**"]
                }
            },
            "sourceToTestMappings": [
                {"source": "src/Components/{name}/**", "test": "tests/{name}.Tests/"},
                {"source": "tests/{name}.Tests/**", "test": "tests/{name}.Tests/"}
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var ignoreFilter = new IgnorePathFilter(config.IgnorePaths);
        var criticalDetector = new CriticalFileDetector(config.TriggerAllPaths);
        var categoryMapper = new CategoryMapper(config.Categories);
        var projectResolver = new ProjectMappingResolver(config.SourceToTestMappings);

        var changedFiles = new[]
        {
            "src/Components/Aspire.Redis/RedisExtensions.cs",
            "tests/Aspire.Redis.Tests/RedisTests.cs",
            "README.md"
        };

        // Step 1: Filter ignored files
        var (ignoredFiles, activeFiles) = ignoreFilter.SplitFiles(changedFiles);
        Assert.Single(ignoredFiles);
        Assert.Contains("README.md", ignoredFiles);
        Assert.Equal(2, activeFiles.Count);

        // Step 2: Check for critical files
        var criticalFile = criticalDetector.FindFirstCriticalFile(activeFiles);
        Assert.Null(criticalFile.File);

        // Step 3: Map to categories
        var (categories, matchedFiles) = categoryMapper.GetCategoriesTriggeredByFiles(activeFiles);
        Assert.True(categories["integrations"]);
        Assert.False(categories["cli_e2e"]);
        Assert.Equal(2, matchedFiles.Count);

        // Step 4: Resolve test projects
        var testProjects = projectResolver.ResolveAllTestProjects(activeFiles);
        Assert.Contains("tests/Aspire.Redis.Tests/", testProjects);
    }

    [Fact]
    public void Evaluate_CriticalFile_TriggersAllTests()
    {
        var configJson = """
        {
            "ignorePaths": ["**/*.md"],
            "triggerAllPaths": ["global.json", "Directory.Build.props"],
            "categories": {
                "integrations": {
                    "triggerPaths": ["src/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var criticalDetector = new CriticalFileDetector(config.TriggerAllPaths);

        var changedFiles = new[] { "global.json", "src/SomeFile.cs" };

        var file = criticalDetector.FindFirstCriticalFile(changedFiles);

        Assert.Equal("global.json", file.File);
    }

    #endregion

    #region Conservative Fallback Tests

    [Fact]
    public void Evaluate_UnmatchedFile_TriggersConservativeFallback()
    {
        var configJson = """
        {
            "ignorePaths": ["**/*.md"],
            "categories": {
                "known": {
                    "triggerPaths": ["src/**", "tests/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var ignoreFilter = new IgnorePathFilter(config.IgnorePaths);
        var categoryMapper = new CategoryMapper(config.Categories);

        var changedFiles = new[] { "some-random-file.txt" };
        var (_, activeFiles) = ignoreFilter.SplitFiles(changedFiles);
        var (categories, matchedFiles) = categoryMapper.GetCategoriesTriggeredByFiles(activeFiles);

        var unmatchedFiles = activeFiles.Except(matchedFiles).ToList();

        Assert.Single(unmatchedFiles);
        Assert.Contains("some-random-file.txt", unmatchedFiles);
        Assert.False(categories["known"]);
    }

    [Fact]
    public void Evaluate_AllFilesIgnored_NoTestsRun()
    {
        var configJson = """
        {
            "ignorePaths": ["**/*.md", "docs/**", ".github/**", "eng/**"],
            "categories": {
                "integrations": {
                    "triggerPaths": ["src/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var ignoreFilter = new IgnorePathFilter(config.IgnorePaths);

        var changedFiles = new[]
        {
            "README.md",
            "docs/getting-started.md",
            ".github/workflows/ci.yml",
            "eng/Version.Details.xml"
        };

        var (ignoredFiles, activeFiles) = ignoreFilter.SplitFiles(changedFiles);

        Assert.Equal(4, ignoredFiles.Count);
        Assert.Empty(activeFiles);
    }

    [Fact]
    public void Evaluate_NoChanges_NoTestsRun()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {
                "integrations": {
                    "triggerPaths": ["src/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var ignoreFilter = new IgnorePathFilter(config.IgnorePaths);
        var categoryMapper = new CategoryMapper(config.Categories);

        var changedFiles = Array.Empty<string>();

        var (_, activeFiles) = ignoreFilter.SplitFiles(changedFiles);
        var (categories, matchedFiles) = categoryMapper.GetCategoriesTriggeredByFiles(activeFiles);

        Assert.Empty(activeFiles);
        Assert.Empty(matchedFiles);
        Assert.False(categories["integrations"]);
    }

    #endregion

    #region Category Exclusion Tests

    [Fact]
    public void Evaluate_ExcludePaths_PreventsCategoryMatch()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {
                "integrations": {
                    "triggerPaths": ["src/**"],
                    "excludePaths": ["src/Aspire.Cli/**", "src/Aspire.ProjectTemplates/**"]
                },
                "cli": {
                    "triggerPaths": ["src/Aspire.Cli/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var categoryMapper = new CategoryMapper(config.Categories);

        var changedFiles = new[] { "src/Aspire.Cli/Program.cs" };

        var (categories, _) = categoryMapper.GetCategoriesTriggeredByFiles(changedFiles);

        Assert.False(categories["integrations"]); // Excluded
        Assert.True(categories["cli"]);
    }

    [Fact]
    public void Evaluate_MultipleCategories_FileMatchesMultiple()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {
                "hosting": {
                    "triggerPaths": ["src/Aspire.Hosting/**"]
                },
                "allsrc": {
                    "triggerPaths": ["src/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var categoryMapper = new CategoryMapper(config.Categories);

        var changedFiles = new[] { "src/Aspire.Hosting/Host.cs" };

        var (categories, matchedFiles) = categoryMapper.GetCategoriesTriggeredByFiles(changedFiles);

        Assert.True(categories["hosting"]);
        Assert.True(categories["allsrc"]);
        Assert.Single(matchedFiles);
    }

    #endregion

    #region Project Mapping Tests

    [Fact]
    public void Evaluate_ProjectMapping_CaptureAndSubstitution()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {},
            "sourceToTestMappings": [
                {"source": "src/Components/{name}/**", "test": "tests/{name}.Tests/"}
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var resolver = new ProjectMappingResolver(config.SourceToTestMappings);

        var changedFiles = new[]
        {
            "src/Components/Aspire.Npgsql/NpgsqlExtensions.cs",
            "src/Components/Aspire.Redis/RedisExtensions.cs"
        };

        var testProjects = resolver.ResolveAllTestProjects(changedFiles);

        Assert.Equal(2, testProjects.Count);
        Assert.Contains("tests/Aspire.Npgsql.Tests/", testProjects);
        Assert.Contains("tests/Aspire.Redis.Tests/", testProjects);
    }

    [Fact]
    public void Evaluate_ProjectMapping_ExcludePatterns()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {},
            "sourceToTestMappings": [
                {
                    "source": "src/Aspire.Hosting.{name}/**",
                    "test": "tests/Aspire.Hosting.{name}.Tests/",
                    "exclude": ["src/Aspire.Hosting.Testing/**"]
                }
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var resolver = new ProjectMappingResolver(config.SourceToTestMappings);

        var changedFiles = new[]
        {
            "src/Aspire.Hosting.Redis/RedisExtensions.cs",
            "src/Aspire.Hosting.Testing/TestBuilder.cs"
        };

        var testProjects = resolver.ResolveAllTestProjects(changedFiles);

        Assert.Single(testProjects);
        Assert.Contains("tests/Aspire.Hosting.Redis.Tests/", testProjects);
        Assert.DoesNotContain("tests/Aspire.Hosting.Testing.Tests/", testProjects);
    }

    [Fact]
    public void Evaluate_ProjectMapping_MultipleMappingsMatchSameFile()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {},
            "sourceToTestMappings": [
                {"source": "src/**", "test": "tests/All.Tests/"},
                {"source": "src/Components/{name}/**", "test": "tests/{name}.Tests/"}
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var resolver = new ProjectMappingResolver(config.SourceToTestMappings);

        var changedFiles = new[] { "src/Components/Aspire.Redis/Client.cs" };

        var testProjects = resolver.ResolveAllTestProjects(changedFiles);

        Assert.Equal(2, testProjects.Count);
        Assert.Contains("tests/All.Tests/", testProjects);
        Assert.Contains("tests/Aspire.Redis.Tests/", testProjects);
    }

    [Fact]
    public void Evaluate_ProjectMapping_PatternWithoutCaptureGroup()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {},
            "sourceToTestMappings": [
                {"source": "src/Dashboard/**", "test": "tests/Dashboard.Tests/"}
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var resolver = new ProjectMappingResolver(config.SourceToTestMappings);

        var changedFiles = new[] { "src/Dashboard/Components/Chart.cs" };

        var testProjects = resolver.ResolveAllTestProjects(changedFiles);

        Assert.Single(testProjects);
        Assert.Contains("tests/Dashboard.Tests/", testProjects);
    }

    [Fact]
    public void Evaluate_ProjectMapping_BatchResolutionReturnsUniqueProjects()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {},
            "sourceToTestMappings": [
                {"source": "src/Components/{name}/**", "test": "tests/{name}.Tests/"}
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var resolver = new ProjectMappingResolver(config.SourceToTestMappings);

        // Multiple files in the same component should resolve to the same test project
        var changedFiles = new[]
        {
            "src/Components/Aspire.Redis/Client.cs",
            "src/Components/Aspire.Redis/Extensions.cs",
            "src/Components/Aspire.Redis/Options.cs"
        };

        var testProjects = resolver.ResolveAllTestProjects(changedFiles);

        Assert.Single(testProjects);
        Assert.Contains("tests/Aspire.Redis.Tests/", testProjects);
    }

    #endregion

    #region Path Normalization Tests

    [Fact]
    public void Evaluate_PathNormalization_BackslashToForwardSlash()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "categories": {
                "integrations": {
                    "triggerPaths": ["src/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var categoryMapper = new CategoryMapper(config.Categories);

        // Windows-style paths should be normalized
        var changedFiles = new[] { @"src\Components\Aspire.Redis\Client.cs" };

        var (categories, matchedFiles) = categoryMapper.GetCategoriesTriggeredByFiles(changedFiles);

        Assert.True(categories["integrations"]);
        Assert.Single(matchedFiles);
    }

    #endregion

    #region TriggerAll Category Tests

    [Fact]
    public void Evaluate_TriggerAllPaths_Detected()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "triggerAllPaths": ["global.json", "Directory.Build.props", "tests/Shared/**"],
            "categories": {
                "integrations": {
                    "triggerPaths": ["src/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var criticalDetector = new CriticalFileDetector(config.TriggerAllPaths);

        // Test various critical files
        Assert.True(criticalDetector.IsCriticalFile("global.json", out _));
        Assert.True(criticalDetector.IsCriticalFile("Directory.Build.props", out _));
        Assert.True(criticalDetector.IsCriticalFile("tests/Shared/TestHelper.cs", out _));

        // Non-critical files
        Assert.False(criticalDetector.IsCriticalFile("src/SomeFile.cs", out _));
    }

    [Fact]
    public void Evaluate_TriggerAllPaths_FindFirstCriticalFile()
    {
        var configJson = """
        {
            "ignorePaths": [],
            "triggerAllPaths": ["global.json", "*.slnx"],
            "categories": {},
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var criticalDetector = new CriticalFileDetector(config.TriggerAllPaths);

        var result = criticalDetector.FindFirstCriticalFile(["global.json"]);

        Assert.NotNull(result.File);
        Assert.Equal("global.json", result.File);
    }

    #endregion

    #region Mixed Scenario Tests

    [Fact]
    public void Evaluate_MixedScenario_IgnoredAndActiveFiles()
    {
        var configJson = """
        {
            "ignorePaths": ["**/*.md", "docs/**"],
            "categories": {
                "cli": {
                    "triggerPaths": ["src/Aspire.Cli/**"]
                },
                "extension": {
                    "triggerPaths": ["extension/**"]
                }
            },
            "sourceToTestMappings": []
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var ignoreFilter = new IgnorePathFilter(config.IgnorePaths);
        var categoryMapper = new CategoryMapper(config.Categories);

        var changedFiles = new[]
        {
            "README.md",
            "docs/api.md",
            "src/Aspire.Cli/Program.cs",
            "extension/package.json"
        };

        var (ignoredFiles, activeFiles) = ignoreFilter.SplitFiles(changedFiles);
        var (categories, matchedFiles) = categoryMapper.GetCategoriesTriggeredByFiles(activeFiles);

        Assert.Equal(2, ignoredFiles.Count);
        Assert.Equal(2, activeFiles.Count);
        Assert.True(categories["cli"]);
        Assert.True(categories["extension"]);
        Assert.Equal(2, matchedFiles.Count);
    }

    [Fact]
    public void Evaluate_RealWorldScenario_DashboardChange()
    {
        var configJson = """
        {
            "ignorePaths": ["**/*.md", "docs/**", ".github/**", "eng/**"],
            "triggerAllPaths": ["global.json", "Directory.Build.props", "*.slnx", "src/Aspire.Hosting/**", "tests/Shared/**"],
            "categories": {
                "integrations": {
                    "triggerPaths": ["src/**", "tests/Aspire.*.Tests/**"],
                    "excludePaths": ["src/Aspire.Cli/**", "src/Aspire.ProjectTemplates/**"]
                },
                "cli_e2e": {
                    "triggerPaths": ["src/Aspire.Cli/**", "tests/Aspire.Cli.EndToEnd.Tests/**"]
                },
                "templates": {
                    "triggerPaths": ["src/Aspire.ProjectTemplates/**", "tests/Aspire.Templates.Tests/**"]
                },
                "extension": {
                    "triggerPaths": ["extension/**"]
                }
            },
            "sourceToTestMappings": [
                {"source": "src/Components/{name}/**", "test": "tests/{name}.Tests/"},
                {"source": "src/Aspire.Dashboard/**", "test": "tests/Aspire.Dashboard.Tests/"},
                {"source": "tests/{name}.Tests/**", "test": "tests/{name}.Tests/"}
            ]
        }
        """;

        var config = TestSelectorConfig.LoadFromJson(configJson);
        var ignoreFilter = new IgnorePathFilter(config.IgnorePaths);
        var criticalDetector = new CriticalFileDetector(config.TriggerAllPaths);
        var categoryMapper = new CategoryMapper(config.Categories);
        var projectResolver = new ProjectMappingResolver(config.SourceToTestMappings);

        var changedFiles = new[]
        {
            "src/Aspire.Dashboard/Components/Layout.razor",
            "tests/Aspire.Dashboard.Tests/LayoutTests.cs"
        };

        // Step 1: Filter
        var (_, activeFiles) = ignoreFilter.SplitFiles(changedFiles);
        Assert.Equal(2, activeFiles.Count);

        // Step 2: No critical files
        var criticalFile = criticalDetector.FindFirstCriticalFile(activeFiles);
        Assert.Null(criticalFile.File);

        // Step 3: Category mapping
        var (categories, matchedFiles) = categoryMapper.GetCategoriesTriggeredByFiles(activeFiles);
        Assert.True(categories["integrations"]);
        Assert.False(categories["cli_e2e"]);
        Assert.False(categories["templates"]);
        Assert.False(categories["extension"]);
        Assert.Equal(2, matchedFiles.Count);

        // Step 4: Project mapping
        var testProjects = projectResolver.ResolveAllTestProjects(activeFiles);
        Assert.Single(testProjects);
        Assert.Contains("tests/Aspire.Dashboard.Tests/", testProjects);
    }

    #endregion
}
