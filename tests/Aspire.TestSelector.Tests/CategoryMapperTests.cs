// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Models;
using Xunit;

namespace Aspire.TestSelector.Tests;

public class CategoryMapperTests
{
    private static CategoryMapper CreateMapper()
    {
        var categories = new Dictionary<string, CategoryConfig>
        {
            ["core"] = new CategoryConfig
            {
                Description = "Critical paths",
                TriggerAll = true,
                TriggerPaths = ["global.json", "Directory.Build.props", "src/Aspire.Hosting/**"]
            },
            ["integrations"] = new CategoryConfig
            {
                Description = "Integration tests",
                TriggerPaths = ["src/Aspire.*/**", "src/Components/**", "tests/Aspire.*.Tests/**"],
                ExcludePaths = ["src/Aspire.Cli/**", "src/Aspire.ProjectTemplates/**"]
            },
            ["cli_e2e"] = new CategoryConfig
            {
                Description = "CLI E2E tests",
                TriggerPaths = ["src/Aspire.Cli/**", "tests/Aspire.Cli.EndToEnd.Tests/**"]
            },
            ["extension"] = new CategoryConfig
            {
                Description = "VS Code extension tests",
                TriggerPaths = ["extension/**"]
            }
        };

        return new CategoryMapper(categories);
    }

    [Theory]
    [InlineData("src/Components/Aspire.Redis/Client.cs", "integrations", true)]
    [InlineData("src/Aspire.Dashboard/Dashboard.cs", "integrations", true)]
    [InlineData("src/Aspire.Cli/Program.cs", "cli_e2e", true)]
    [InlineData("src/Aspire.Cli/Program.cs", "integrations", false)] // Excluded by excludePaths
    [InlineData("extension/package.json", "extension", true)]
    [InlineData("extension/src/index.ts", "extension", true)]
    [InlineData("global.json", "core", true)]
    [InlineData("docs/README.md", "integrations", false)]
    public void FileTriggersCategory_MatchesCorrectly(string filePath, string categoryName, bool expected)
    {
        var mapper = CreateMapper();

        var result = mapper.FileTriggersCategory(filePath, categoryName);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FileTriggersCategory_NonExistentCategory_ReturnsFalse()
    {
        var mapper = CreateMapper();

        var result = mapper.FileTriggersCategory("any/file.cs", "nonexistent");

        Assert.False(result);
    }

    [Theory]
    [InlineData("src\\Components\\Aspire.Redis\\Client.cs")]
    [InlineData("src/Components/Aspire.Redis/Client.cs")]
    public void FileTriggersCategory_NormalizesPath(string filePath)
    {
        var mapper = CreateMapper();

        var result = mapper.FileTriggersCategory(filePath, "integrations");

        Assert.True(result);
    }

    [Fact]
    public void GetCategoriesTriggeredByFiles_InitializesAllCategoriesFalse()
    {
        var mapper = CreateMapper();

        var (categories, matchedFiles) = mapper.GetCategoriesTriggeredByFiles([]);

        Assert.Equal(4, categories.Count);
        Assert.False(categories["core"]);
        Assert.False(categories["integrations"]);
        Assert.False(categories["cli_e2e"]);
        Assert.False(categories["extension"]);
        Assert.Empty(matchedFiles);
    }

    [Fact]
    public void GetCategoriesTriggeredByFiles_MarksMatchingCategoriesTrue()
    {
        var mapper = CreateMapper();

        var files = new[]
        {
            "src/Components/Aspire.Redis/Client.cs",
            "extension/package.json"
        };

        var (categories, matchedFiles) = mapper.GetCategoriesTriggeredByFiles(files);

        Assert.True(categories["integrations"]);
        Assert.True(categories["extension"]);
        Assert.False(categories["core"]);
        Assert.False(categories["cli_e2e"]);
        Assert.Equal(2, matchedFiles.Count);
    }

    [Fact]
    public void GetCategoriesTriggeredByFiles_ExcludePathsRespected()
    {
        var mapper = CreateMapper();

        var files = new[]
        {
            "src/Aspire.Cli/Program.cs" // Matches cli_e2e, but excluded from integrations
        };

        var (categories, matchedFiles) = mapper.GetCategoriesTriggeredByFiles(files);

        Assert.True(categories["cli_e2e"]);
        Assert.False(categories["integrations"]); // Excluded
        Assert.Single(matchedFiles);
    }

    [Fact]
    public void GetCategoriesTriggeredByFiles_ReturnsMatchedFiles()
    {
        var mapper = CreateMapper();

        var files = new[]
        {
            "src/Components/Aspire.Redis/Client.cs",
            "docs/README.md", // Doesn't match any category
            "extension/package.json"
        };

        var (_, matchedFiles) = mapper.GetCategoriesTriggeredByFiles(files);

        Assert.Equal(2, matchedFiles.Count);
        Assert.Contains("src/Components/Aspire.Redis/Client.cs", matchedFiles);
        Assert.Contains("extension/package.json", matchedFiles);
        Assert.DoesNotContain("docs/README.md", matchedFiles);
    }

    [Fact]
    public void GetTriggeredCategories_ReturnsOnlyCategories()
    {
        var mapper = CreateMapper();

        var files = new[] { "src/Components/Aspire.Redis/Client.cs" };

        var categories = mapper.GetTriggeredCategories(files);

        Assert.Equal(4, categories.Count);
        Assert.True(categories["integrations"]);
    }

    [Fact]
    public void GetAllCategories_ReturnsAllConfiguredCategories()
    {
        var mapper = CreateMapper();

        var categories = mapper.GetAllCategories().ToList();

        Assert.Equal(4, categories.Count);
        Assert.Contains("core", categories);
        Assert.Contains("integrations", categories);
        Assert.Contains("cli_e2e", categories);
        Assert.Contains("extension", categories);
    }

    [Theory]
    [InlineData("core", true)]
    [InlineData("integrations", false)]
    [InlineData("cli_e2e", false)]
    [InlineData("nonexistent", false)]
    public void IsTriggerAllCategory_ReturnsCorrectValue(string categoryName, bool expected)
    {
        var mapper = CreateMapper();

        var result = mapper.IsTriggerAllCategory(categoryName);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetCategoryConfig_ReturnsConfig()
    {
        var mapper = CreateMapper();

        var config = mapper.GetCategoryConfig("core");

        Assert.NotNull(config);
        Assert.True(config.TriggerAll);
        Assert.Equal("Critical paths", config.Description);
    }

    [Fact]
    public void GetCategoryConfig_NonExistent_ReturnsNull()
    {
        var mapper = CreateMapper();

        var config = mapper.GetCategoryConfig("nonexistent");

        Assert.Null(config);
    }

    [Fact]
    public void CategoryMapper_EmptyCategories_NoMatches()
    {
        var mapper = new CategoryMapper([]);

        var (categories, matchedFiles) = mapper.GetCategoriesTriggeredByFiles(["any/file.cs"]);

        Assert.Empty(categories);
        Assert.Empty(matchedFiles);
    }

    [Fact]
    public void GetCategoriesTriggeredByFiles_FileMatchesMultipleCategories()
    {
        var mapper = CreateMapper();

        // src/Aspire.Hosting/Host.cs matches both "core" (triggerAll) and "integrations"
        var files = new[] { "src/Aspire.Hosting/Host.cs" };

        var (categories, matchedFiles) = mapper.GetCategoriesTriggeredByFiles(files);

        Assert.True(categories["core"]);
        Assert.True(categories["integrations"]);
        Assert.Single(matchedFiles);
    }

    [Fact]
    public void GetCategoriesTriggeredByFiles_AllFilesMatch_AllCategoriesTriggered()
    {
        var mapper = CreateMapper();

        var files = new[]
        {
            "global.json",                          // core
            "src/Components/Aspire.Redis/Client.cs", // integrations
            "src/Aspire.Cli/Program.cs",            // cli_e2e
            "extension/package.json"                 // extension
        };

        var (categories, matchedFiles) = mapper.GetCategoriesTriggeredByFiles(files);

        Assert.True(categories["core"]);
        Assert.True(categories["integrations"]);
        Assert.True(categories["cli_e2e"]);
        Assert.True(categories["extension"]);
        Assert.Equal(4, matchedFiles.Count);
    }
}
