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
            ["integrations"] = new CategoryConfig
            {
                Description = "Integration tests",
                TestProjects = new TestProjectsValue { IsAuto = true }
            },
            ["dashboard"] = new CategoryConfig
            {
                Description = "Dashboard tests",
                TestProjects = new TestProjectsValue
                {
                    IsAuto = false,
                    Projects = [
                        "tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj",
                        "tests/Aspire.Dashboard.Components.Tests/Aspire.Dashboard.Components.Tests.csproj"
                    ]
                }
            },
            ["cli"] = new CategoryConfig
            {
                Description = "CLI tests",
                TestProjects = new TestProjectsValue
                {
                    IsAuto = false,
                    Projects = [
                        "tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj"
                    ]
                }
            }
        };

        return new CategoryMapper(categories);
    }

    [Theory]
    [InlineData("tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj", "dashboard")]
    [InlineData("tests/Aspire.Dashboard.Components.Tests/Aspire.Dashboard.Components.Tests.csproj", "dashboard")]
    [InlineData("tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj", "cli")]
    public void GetCategoryForProject_MapsToCorrectCategory(string projectPath, string expectedCategory)
    {
        var mapper = CreateMapper();

        var category = mapper.GetCategoryForProject(projectPath);

        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public void GetCategoryForProject_UnmappedProject_DefaultsToIntegrations()
    {
        var mapper = CreateMapper();

        var category = mapper.GetCategoryForProject("tests/SomeOther.Tests/SomeOther.Tests.csproj");

        Assert.Equal("integrations", category);
    }

    [Theory]
    [InlineData("tests\\Aspire.Dashboard.Tests\\Aspire.Dashboard.Tests.csproj")]
    [InlineData("tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj")]
    [InlineData("tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj/")]
    public void GetCategoryForProject_NormalizesPath(string projectPath)
    {
        var mapper = CreateMapper();

        var category = mapper.GetCategoryForProject(projectPath);

        Assert.Equal("dashboard", category);
    }

    [Fact]
    public void GetTriggeredCategories_InitializesAllCategoriesFalse()
    {
        var mapper = CreateMapper();

        var triggered = mapper.GetTriggeredCategories([]);

        Assert.Equal(3, triggered.Count);
        Assert.False(triggered["integrations"]);
        Assert.False(triggered["dashboard"]);
        Assert.False(triggered["cli"]);
    }

    [Fact]
    public void GetTriggeredCategories_MarksMatchingCategoriesTrue()
    {
        var mapper = CreateMapper();

        var testProjects = new[]
        {
            "tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj",
            "tests/SomeIntegration.Tests/SomeIntegration.Tests.csproj"
        };

        var triggered = mapper.GetTriggeredCategories(testProjects);

        Assert.True(triggered["dashboard"]);
        Assert.True(triggered["integrations"]);
        Assert.False(triggered["cli"]);
    }

    [Theory]
    [InlineData("integrations", true)]
    [InlineData("dashboard", false)]
    [InlineData("cli", false)]
    public void IsAutoCategory_ReturnsCorrectValue(string categoryName, bool expectedIsAuto)
    {
        var mapper = CreateMapper();

        var isAuto = mapper.IsAutoCategory(categoryName);

        Assert.Equal(expectedIsAuto, isAuto);
    }

    [Fact]
    public void IsAutoCategory_NonExistentCategory_ReturnsFalse()
    {
        var mapper = CreateMapper();

        var isAuto = mapper.IsAutoCategory("nonexistent");

        Assert.False(isAuto);
    }

    [Fact]
    public void GetAllCategories_ReturnsAllConfiguredCategories()
    {
        var mapper = CreateMapper();

        var categories = mapper.GetAllCategories().ToList();

        Assert.Equal(3, categories.Count);
        Assert.Contains("integrations", categories);
        Assert.Contains("dashboard", categories);
        Assert.Contains("cli", categories);
    }

    [Fact]
    public void GetProjectsForCategory_ReturnsConfiguredProjects()
    {
        var mapper = CreateMapper();

        var projects = mapper.GetProjectsForCategory("dashboard");

        Assert.Equal(2, projects.Count);
        Assert.Contains("tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj", projects);
        Assert.Contains("tests/Aspire.Dashboard.Components.Tests/Aspire.Dashboard.Components.Tests.csproj", projects);
    }

    [Fact]
    public void GetProjectsForCategory_AutoCategory_ReturnsEmpty()
    {
        var mapper = CreateMapper();

        var projects = mapper.GetProjectsForCategory("integrations");

        Assert.Empty(projects);
    }

    [Fact]
    public void GetProjectsForCategory_NonExistentCategory_ReturnsEmpty()
    {
        var mapper = CreateMapper();

        var projects = mapper.GetProjectsForCategory("nonexistent");

        Assert.Empty(projects);
    }

    [Fact]
    public void GroupByCategory_GroupsCorrectly()
    {
        var mapper = CreateMapper();

        var testProjects = new[]
        {
            "tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj",
            "tests/Aspire.Dashboard.Components.Tests/Aspire.Dashboard.Components.Tests.csproj",
            "tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj",
            "tests/SomeOther.Tests/SomeOther.Tests.csproj"
        };

        var groups = mapper.GroupByCategory(testProjects);

        Assert.Equal(3, groups.Count);
        Assert.Equal(2, groups["dashboard"].Count);
        Assert.Single(groups["cli"]);
        Assert.Single(groups["integrations"]);
    }

    [Fact]
    public void GetCategoryForProject_PartialPathMatch_FindsCategory()
    {
        var mapper = CreateMapper();

        var category = mapper.GetCategoryForProject("/full/path/to/tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj");

        Assert.Equal("dashboard", category);
    }

    [Fact]
    public void CategoryMapper_EmptyCategories_AllDefaultToIntegrations()
    {
        var mapper = new CategoryMapper([]);

        var category = mapper.GetCategoryForProject("tests/Any.Tests.csproj");

        Assert.Equal("integrations", category);
    }

    [Fact]
    public void GetTriggeredCategories_AllProjectsMapped_AllCategoriesTriggered()
    {
        var mapper = CreateMapper();

        var testProjects = new[]
        {
            "tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj",
            "tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj",
            "tests/Unknown.Tests/Unknown.Tests.csproj"
        };

        var triggered = mapper.GetTriggeredCategories(testProjects);

        Assert.True(triggered["dashboard"]);
        Assert.True(triggered["cli"]);
        Assert.True(triggered["integrations"]);
    }
}
