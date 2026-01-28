// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.TestSelector.Models;
using Xunit;

namespace Aspire.TestSelector.Tests.Models;

public class TestSelectionResultTests
{
    [Fact]
    public void CriticalPath_SetsRunAllTrueAndReason()
    {
        var result = TestSelectionResult.CriticalPath("eng/Build.props", "eng/**/*.props");

        Assert.True(result.RunAllTests);
        Assert.Equal("critical_path", result.Reason);
        Assert.Equal("eng/Build.props", result.TriggerFile);
        Assert.Equal("eng/**/*.props", result.TriggerPattern);
    }

    [Fact]
    public void NoChanges_SetsRunAllFalseWithNoChangesReason()
    {
        var result = TestSelectionResult.NoChanges();

        Assert.False(result.RunAllTests);
        Assert.Equal("no_changes", result.Reason);
        Assert.Null(result.TriggerFile);
        Assert.Null(result.TriggerPattern);
    }

    [Fact]
    public void AllIgnored_IncludesIgnoredFilesList()
    {
        var ignoredFiles = new List<string> { "README.md", "docs/guide.md" };
        var result = TestSelectionResult.AllIgnored(ignoredFiles);

        Assert.False(result.RunAllTests);
        Assert.Equal("all_ignored", result.Reason);
        Assert.Equal(2, result.IgnoredFiles.Count);
        Assert.Contains("README.md", result.IgnoredFiles);
        Assert.Contains("docs/guide.md", result.IgnoredFiles);
    }

    [Fact]
    public void WithError_SetsRunAllTrueConservatively()
    {
        var result = TestSelectionResult.WithError("dotnet-affected failed");

        Assert.True(result.RunAllTests);
        Assert.Equal("error", result.Reason);
        Assert.Equal("dotnet-affected failed", result.Error);
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var result = TestSelectionResult.CriticalPath("file.cs", "**/*.cs");
        result.Categories["integrations"] = true;
        result.AffectedTestProjects.Add("tests/Test1.csproj");

        var json = result.ToJson();

        var parsed = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, parsed.RootElement.ValueKind);

        Assert.True(parsed.RootElement.GetProperty("runAllTests").GetBoolean());
        Assert.Equal("critical_path", parsed.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public void ToJson_OmitsNullProperties()
    {
        var result = TestSelectionResult.NoChanges();

        var json = result.ToJson();

        var parsed = JsonDocument.Parse(json);
        Assert.False(parsed.RootElement.TryGetProperty("triggerFile", out _));
        Assert.False(parsed.RootElement.TryGetProperty("triggerPattern", out _));
        Assert.False(parsed.RootElement.TryGetProperty("error", out _));
        Assert.False(parsed.RootElement.TryGetProperty("nugetDependentTests", out _));
    }

    [Fact]
    public void ToJson_IncludesCategories()
    {
        var result = new TestSelectionResult
        {
            RunAllTests = false,
            Reason = "selective",
            Categories =
            {
                ["integrations"] = true,
                ["dashboard"] = false,
                ["cli"] = true
            }
        };

        var json = result.ToJson();
        var parsed = JsonDocument.Parse(json);

        var categories = parsed.RootElement.GetProperty("categories");
        Assert.True(categories.GetProperty("integrations").GetBoolean());
        Assert.False(categories.GetProperty("dashboard").GetBoolean());
        Assert.True(categories.GetProperty("cli").GetBoolean());
    }

    [Fact]
    public void ToJson_IncludesNuGetDependentTestsWhenSet()
    {
        var result = new TestSelectionResult
        {
            RunAllTests = false,
            Reason = "selective",
            NuGetDependentTests = new NuGetDependentTestsInfo
            {
                Triggered = true,
                Reason = "Packable project affected",
                AffectedPackableProjects = ["src/Aspire.Hosting/Aspire.Hosting.csproj"],
                Projects = ["tests/Aspire.Templates.Tests/Aspire.Templates.Tests.csproj"]
            }
        };

        var json = result.ToJson();
        var parsed = JsonDocument.Parse(json);

        var nugetTests = parsed.RootElement.GetProperty("nugetDependentTests");
        Assert.True(nugetTests.GetProperty("triggered").GetBoolean());
    }

    [Fact]
    public void ToJson_IsIndented()
    {
        var result = TestSelectionResult.NoChanges();
        var json = result.ToJson();

        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    [Fact]
    public void WriteGitHubOutput_IncludesRunIntegrations_WhenCategoryNotPresent()
    {
        var result = new TestSelectionResult
        {
            RunAllTests = false,
            Reason = "msbuild_analysis",
            Categories = { ["templates"] = true },
            IntegrationsProjects = ["tests/Aspire.Milvus.Client.Tests/"]
        };

        var output = CaptureGitHubOutput(result);

        Assert.Contains("run_integrations=true", output);
        Assert.Contains("run_templates=true", output);
    }

    [Fact]
    public void WriteGitHubOutput_IncludesRunIntegrations_WhenRunAllIsTrue()
    {
        var result = new TestSelectionResult
        {
            RunAllTests = true,
            Reason = "critical_path",
            Categories = { ["core"] = true }
        };

        var output = CaptureGitHubOutput(result);

        Assert.Contains("run_all=true", output);
        Assert.Contains("run_integrations=true", output);
    }

    [Fact]
    public void WriteGitHubOutput_DoesNotDuplicateRunIntegrations_WhenCategoryPresent()
    {
        var result = new TestSelectionResult
        {
            RunAllTests = false,
            Reason = "selective",
            Categories = { ["integrations"] = true },
            IntegrationsProjects = ["tests/Aspire.Milvus.Client.Tests/"]
        };

        var output = CaptureGitHubOutput(result);

        // Should only appear once (from category loop)
        var count = output.Split('\n').Count(l => l.StartsWith("run_integrations="));
        Assert.Equal(1, count);
    }

    [Fact]
    public void WriteGitHubOutput_RunIntegrationsFalse_WhenNoProjectsAndNotRunAll()
    {
        var result = new TestSelectionResult
        {
            RunAllTests = false,
            Reason = "all_ignored",
            Categories = { ["templates"] = false }
        };

        var output = CaptureGitHubOutput(result);

        Assert.Contains("run_integrations=false", output);
    }

    [Fact]
    public void WriteGitHubOutput_RunIntegrationsTrue_WhenCategoryFalseButProjectsExist()
    {
        var result = new TestSelectionResult
        {
            RunAllTests = false,
            Reason = "msbuild_analysis",
            Categories = { ["integrations"] = false, ["extension"] = true },
            IntegrationsProjects = ["tests/Infrastructure.Tests/"]
        };

        var output = CaptureGitHubOutput(result);

        Assert.Contains("run_integrations=true", output);
        Assert.Contains("run_extension=true", output);
        var count = output.Split('\n').Count(l => l.StartsWith("run_integrations="));
        Assert.Equal(1, count);
    }

    private static string CaptureGitHubOutput(TestSelectionResult result)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", tempFile);
            result.WriteGitHubOutput();
            return File.ReadAllText(tempFile);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", null);
            File.Delete(tempFile);
        }
    }
}
