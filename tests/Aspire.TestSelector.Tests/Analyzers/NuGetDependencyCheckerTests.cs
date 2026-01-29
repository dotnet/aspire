// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Analyzers;
using Xunit;

namespace Aspire.TestSelector.Tests.Analyzers;

public class NuGetDependencyCheckerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MSBuildProjectEvaluator _evaluator;
    private readonly TestProjectFilter _projectFilter;

    static NuGetDependencyCheckerTests()
    {
        // Initialize MSBuild once for all tests in this class
        MSBuildProjectEvaluator.Initialize();
    }

    public NuGetDependencyCheckerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"NuGetDependencyCheckerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _evaluator = new MSBuildProjectEvaluator(_tempDir);
        _projectFilter = new TestProjectFilter(_tempDir, _evaluator);
    }

    public void Dispose()
    {
        _projectFilter.ClearCache();
        _evaluator.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Check_WithPackableProjects_TriggersNuGetTests()
    {
        var packableProj = CreateProjectFile("src", "Packable.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
            </Project>
            """);

        var nugetDependentTests = new[] { "tests/Templates.Tests.csproj" };
        var checker = new NuGetDependencyChecker(_projectFilter, nugetDependentTests);

        var result = checker.Check([packableProj]);

        Assert.True(result.Triggered);
        Assert.Single(result.AffectedPackableProjects);
        Assert.Single(result.Projects);
        Assert.Contains("tests/Templates.Tests.csproj", result.Projects);
    }

    [Fact]
    public void Check_WithNoPackableProjects_DoesNotTrigger()
    {
        var testProj = CreateProjectFile("tests", "Test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
            </Project>
            """);

        var checker = new NuGetDependencyChecker(_projectFilter, ["tests/Templates.Tests.csproj"]);

        var result = checker.Check([testProj]);

        Assert.False(result.Triggered);
        Assert.Empty(result.AffectedPackableProjects);
        Assert.Equal("No packable projects affected", result.Reason);
    }

    [Fact]
    public void Check_MixedProjects_OnlyCountsPackable()
    {
        var packableProj = CreateProjectFile("src", "Packable.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
            </Project>
            """);

        var testProj = CreateProjectFile("tests", "Test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
            </Project>
            """);

        var nonPackableProj = CreateProjectFile("lib", "NonPackable.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
            </Project>
            """);

        var checker = new NuGetDependencyChecker(_projectFilter, ["tests/Templates.Tests.csproj"]);

        var result = checker.Check([packableProj, testProj, nonPackableProj]);

        Assert.True(result.Triggered);
        Assert.Single(result.AffectedPackableProjects);
        Assert.Contains(packableProj, result.AffectedPackableProjects);
    }

    [Fact]
    public void Check_Triggered_ReturnsCorrectProjects()
    {
        var packableProj = CreateProjectFile("src", "Packable.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
            </Project>
            """);

        var nugetDependentTests = new[]
        {
            "tests/Aspire.Templates.Tests.csproj",
            "tests/Aspire.EndToEnd.Tests.csproj",
            "tests/Aspire.Cli.EndToEnd.Tests.csproj"
        };

        var checker = new NuGetDependencyChecker(_projectFilter, nugetDependentTests);

        var result = checker.Check([packableProj]);

        Assert.True(result.Triggered);
        Assert.Equal(3, result.Projects.Count);
        Assert.Contains("tests/Aspire.Templates.Tests.csproj", result.Projects);
        Assert.Contains("tests/Aspire.EndToEnd.Tests.csproj", result.Projects);
        Assert.Contains("tests/Aspire.Cli.EndToEnd.Tests.csproj", result.Projects);
    }

    [Fact]
    public void NuGetDependentTestProjects_ReturnsConfiguredProjects()
    {
        var nugetDependentTests = new[]
        {
            "tests/Templates.Tests.csproj",
            "tests/EndToEnd.Tests.csproj"
        };

        var checker = new NuGetDependencyChecker(_projectFilter, nugetDependentTests);

        Assert.Equal(2, checker.NuGetDependentTestProjects.Count);
        Assert.Contains("tests/Templates.Tests.csproj", checker.NuGetDependentTestProjects);
    }

    [Fact]
    public void Create_DiscoversMSBuildProjects()
    {
        // Create a test project with RequiresNuGets=true in a tests subdirectory
        var testsDir = Path.Combine(_tempDir, "tests", "MyTest");
        Directory.CreateDirectory(testsDir);
        var testProjectPath = Path.Combine(testsDir, "MyTest.csproj");
        File.WriteAllText(testProjectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsTestProject>true</IsTestProject>
                <RequiresNuGets>true</RequiresNuGets>
              </PropertyGroup>
            </Project>
            """);

        var checker = NuGetDependencyChecker.Create(_projectFilter, _evaluator);

        // The discovered project should be in the list
        Assert.Contains(checker.NuGetDependentTestProjects, p => p.Contains("MyTest.csproj"));
    }

    [Fact]
    public void Check_MultiplePackableProjects_ReportsAllInReason()
    {
        var packable1 = CreateProjectFile("src", "Packable1.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
            </Project>
            """);

        var packable2 = CreateProjectFile("src", "Packable2.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
            </Project>
            """);

        var checker = new NuGetDependencyChecker(_projectFilter, ["tests/Templates.Tests.csproj"]);

        var result = checker.Check([packable1, packable2]);

        Assert.True(result.Triggered);
        Assert.Equal(2, result.AffectedPackableProjects.Count);
        Assert.Contains("Packable1", result.Reason);
        Assert.Contains("Packable2", result.Reason);
    }

    [Fact]
    public void Check_EmptyProjectList_DoesNotTrigger()
    {
        var checker = new NuGetDependencyChecker(_projectFilter, ["tests/Templates.Tests.csproj"]);

        var result = checker.Check([]);

        Assert.False(result.Triggered);
    }

    private string CreateProjectFile(string subDir, string name, string content)
    {
        var dir = Path.Combine(_tempDir, subDir);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }
}
