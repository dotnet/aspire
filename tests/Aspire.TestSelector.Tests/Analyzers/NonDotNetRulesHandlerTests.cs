// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Analyzers;
using Aspire.TestSelector.Models;
using Xunit;

namespace Aspire.TestSelector.Tests.Analyzers;

public class NonDotNetRulesHandlerTests
{
    [Theory]
    [InlineData("src/Aspire.Dashboard/**", "dashboard", "src/Aspire.Dashboard/App.razor", true)]
    [InlineData("src/Aspire.Dashboard/**", "dashboard", "src/Aspire.Hosting/Resource.cs", false)]
    [InlineData("**/*.js", "frontend", "src/Dashboard/scripts/main.js", true)]
    [InlineData("**/*.js", "frontend", "src/api/handler.cs", false)]
    public void GetTriggeredCategories_MatchesPattern_ReturnsCategory(
        string pattern, string category, string filePath, bool shouldMatch)
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = pattern, Category = category }
        ]);

        var categories = handler.GetTriggeredCategories(filePath);

        if (shouldMatch)
        {
            Assert.Single(categories);
            Assert.Contains(category, categories);
        }
        else
        {
            Assert.Empty(categories);
        }
    }

    [Fact]
    public void GetTriggeredCategories_NoMatch_ReturnsEmpty()
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "src/Dashboard/**", Category = "dashboard" }
        ]);

        var categories = handler.GetTriggeredCategories("src/Hosting/file.cs");

        Assert.Empty(categories);
    }

    [Fact]
    public void GetAllTriggeredCategories_GroupsByCategory()
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "src/Dashboard/**", Category = "dashboard" },
            new NonDotNetRule { Pattern = "**/*.razor", Category = "dashboard" },
            new NonDotNetRule { Pattern = "src/Cli/**", Category = "cli" }
        ]);

        var files = new[]
        {
            "src/Dashboard/App.razor",
            "src/Dashboard/Components/Grid.razor",
            "src/Cli/Command.cs",
            "src/Hosting/Resource.cs"
        };

        var result = handler.GetAllTriggeredCategories(files);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("dashboard"));
        Assert.True(result.ContainsKey("cli"));
        Assert.Equal(2, result["dashboard"].Count);
        Assert.Single(result["cli"]);
    }

    [Theory]
    [InlineData("src\\Dashboard\\file.razor")]
    [InlineData("src/Dashboard/file.razor")]
    public void GetTriggeredCategories_NormalizesPathSeparators(string filePath)
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "src/Dashboard/**", Category = "dashboard" }
        ]);

        var categories = handler.GetTriggeredCategories(filePath);

        Assert.Single(categories);
        Assert.Contains("dashboard", categories);
    }

    [Fact]
    public void MatchesAnyRule_WithNoRules_ReturnsFalse()
    {
        var handler = new NonDotNetRulesHandler([]);

        Assert.False(handler.MatchesAnyRule("any/file.js"));
    }

    [Fact]
    public void MatchesAnyRule_WithMatch_ReturnsTrue()
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "**/*.js", Category = "frontend" }
        ]);

        Assert.True(handler.MatchesAnyRule("src/scripts/app.js"));
        Assert.False(handler.MatchesAnyRule("src/code.cs"));
    }

    [Fact]
    public void RuleCount_ReturnsCorrectCount()
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "**/*.js", Category = "frontend" },
            new NonDotNetRule { Pattern = "**/*.css", Category = "frontend" },
            new NonDotNetRule { Pattern = "src/Cli/**", Category = "cli" }
        ]);

        Assert.Equal(3, handler.RuleCount);
    }

    [Fact]
    public void GetFirstMatch_ReturnsPatternAndCategory()
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "src/Dashboard/**", Category = "dashboard" },
            new NonDotNetRule { Pattern = "**/*.razor", Category = "ui" }
        ]);

        var (pattern, category) = handler.GetFirstMatch("src/Dashboard/App.razor");

        Assert.Equal("src/Dashboard/**", pattern);
        Assert.Equal("dashboard", category);
    }

    [Fact]
    public void GetFirstMatch_NoMatch_ReturnsNulls()
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "src/Dashboard/**", Category = "dashboard" }
        ]);

        var (pattern, category) = handler.GetFirstMatch("src/Hosting/file.cs");

        Assert.Null(pattern);
        Assert.Null(category);
    }

    [Fact]
    public void GetTriggeredCategories_FileMatchesMultipleRules_ReturnsUniqueCategories()
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "src/Dashboard/**", Category = "dashboard" },
            new NonDotNetRule { Pattern = "**/*.razor", Category = "dashboard" },
            new NonDotNetRule { Pattern = "**/*.razor", Category = "ui" }
        ]);

        var categories = handler.GetTriggeredCategories("src/Dashboard/App.razor");

        Assert.Equal(2, categories.Count);
        Assert.Contains("dashboard", categories);
        Assert.Contains("ui", categories);
    }

    [Fact]
    public void GetTriggeredCategories_SameCategoryMultipleRules_ReturnsSingleCategory()
    {
        var handler = new NonDotNetRulesHandler([
            new NonDotNetRule { Pattern = "src/Dashboard/**", Category = "dashboard" },
            new NonDotNetRule { Pattern = "**/*.razor", Category = "dashboard" }
        ]);

        var categories = handler.GetTriggeredCategories("src/Dashboard/App.razor");

        Assert.Single(categories);
        Assert.Contains("dashboard", categories);
    }
}
