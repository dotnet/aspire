// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Analyzers;
using Xunit;

namespace Aspire.TestSelector.Tests.Analyzers;

public class TestProjectFilterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TestProjectFilter _filter;

    public TestProjectFilterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"TestProjectFilterTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _filter = new TestProjectFilter(_tempDir);
    }

    public void Dispose()
    {
        _filter.ClearCache();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void IsTestProject_WithExplicitProperty_ReturnsTrue()
    {
        var projectPath = CreateProjectFile("TestProject.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
            </Project>
            """);

        Assert.True(_filter.IsTestProject(projectPath));
    }

    [Fact]
    public void IsTestProject_WithTestSdkReference_ReturnsTrue()
    {
        var projectPath = CreateProjectFile("ProjectWithTestSdk.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
              </ItemGroup>
            </Project>
            """);

        Assert.True(_filter.IsTestProject(projectPath));
    }

    [Fact]
    public void IsTestProject_WithXunitReference_ReturnsTrue()
    {
        var projectPath = CreateProjectFile("XunitProject.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="xunit.v3" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);

        Assert.True(_filter.IsTestProject(projectPath));
    }

    [Fact]
    public void IsTestProject_InTestsDirectory_FileDoesNotExist_FallsBackToPathDetection()
    {
        var testsDir = Path.Combine(_tempDir, "tests");
        Directory.CreateDirectory(testsDir);

        var projectPath = Path.Combine(testsDir, "MyProject.Tests.csproj");
        // Note: we intentionally don't create the file to trigger path-based detection

        Assert.True(_filter.IsTestProject(projectPath));
    }

    [Fact]
    public void IsTestProject_InTestsDirectoryWithNunitReference_ReturnsTrue()
    {
        var testsDir = Path.Combine(_tempDir, "tests");
        Directory.CreateDirectory(testsDir);

        var projectPath = Path.Combine(testsDir, "MyProject.Tests.csproj");
        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="NUnit" Version="3.0.0" />
              </ItemGroup>
            </Project>
            """);

        Assert.True(_filter.IsTestProject(projectPath));
    }

    [Fact]
    public void IsTestProject_SourceProject_ReturnsFalse()
    {
        var projectPath = CreateProjectFile("SourceProject.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        Assert.False(_filter.IsTestProject(projectPath));
    }

    [Fact]
    public void IsPackable_WithExplicitProperty_ReturnsTrue()
    {
        var projectPath = CreateProjectFile("PackableProject.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
            </Project>
            """);

        Assert.True(_filter.IsPackable(projectPath));
    }

    [Fact]
    public void IsPackable_InSrcDirectory_DefaultsToTrue()
    {
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);

        var projectPath = Path.Combine(srcDir, "MyLibrary.csproj");
        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        Assert.True(_filter.IsPackable(projectPath));
    }

    [Fact]
    public void IsPackable_TestProject_ReturnsFalse()
    {
        var projectPath = CreateProjectFile("TestProject.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
            </Project>
            """);

        Assert.False(_filter.IsPackable(projectPath));
    }

    [Fact]
    public void SplitProjects_SeparatesTestAndSourceProjects()
    {
        var testProj = CreateProjectFile("Test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
            </Project>
            """);

        var sourceProj = CreateProjectFile("Source.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var (testProjects, sourceProjects) = _filter.SplitProjects([testProj, sourceProj]);

        Assert.Single(testProjects);
        Assert.Single(sourceProjects);
        Assert.Contains(testProj, testProjects);
        Assert.Contains(sourceProj, sourceProjects);
    }

    [Fact]
    public void IsTestProject_FileNotFound_FallsBackToPathDetection()
    {
        var testsDir = Path.Combine(_tempDir, "tests");
        Directory.CreateDirectory(testsDir);

        var nonExistentPath = Path.Combine(testsDir, "NonExistent.Tests.csproj");

        Assert.True(_filter.IsTestProject(nonExistentPath));
    }

    [Fact]
    public void IsTestProject_FileNotFoundNotInTests_ReturnsFalse()
    {
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);

        var nonExistentPath = Path.Combine(srcDir, "NonExistent.csproj");

        Assert.False(_filter.IsTestProject(nonExistentPath));
    }

    [Fact]
    public void IsTestProject_InvalidXml_FallsBackToPathDetection()
    {
        var testsDir = Path.Combine(_tempDir, "tests");
        Directory.CreateDirectory(testsDir);

        var projectPath = Path.Combine(testsDir, "InvalidXml.Tests.csproj");
        File.WriteAllText(projectPath, "{ not xml }");

        Assert.True(_filter.IsTestProject(projectPath));
    }

    [Fact]
    public void GetProjectInfo_CachesResults()
    {
        var projectPath = CreateProjectFile("CachedProject.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
            </Project>
            """);

        var info1 = _filter.GetProjectInfo(projectPath);
        var info2 = _filter.GetProjectInfo(projectPath);

        Assert.Same(info1, info2);
    }

    [Fact]
    public void ClearCache_RemovesCachedEntries()
    {
        var projectPath = CreateProjectFile("ClearedProject.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
            </Project>
            """);

        var info1 = _filter.GetProjectInfo(projectPath);
        _filter.ClearCache();
        var info2 = _filter.GetProjectInfo(projectPath);

        Assert.NotSame(info1, info2);
    }

    [Fact]
    public void FilterTestProjects_ReturnsOnlyTestProjects()
    {
        var testProj1 = CreateProjectFile("Test1.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup>
            </Project>
            """);

        var testProj2 = CreateProjectFile("Test2.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup><PackageReference Include="xunit" Version="2.0.0" /></ItemGroup>
            </Project>
            """);

        var sourceProj = CreateProjectFile("Source.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        var filtered = _filter.FilterTestProjects([testProj1, testProj2, sourceProj]);

        Assert.Equal(2, filtered.Count);
        Assert.Contains(testProj1, filtered);
        Assert.Contains(testProj2, filtered);
        Assert.DoesNotContain(sourceProj, filtered);
    }

    [Fact]
    public void FilterPackableProjects_ReturnsOnlyPackable()
    {
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);

        var packableProj = Path.Combine(srcDir, "Packable.csproj");
        File.WriteAllText(packableProj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
            </Project>
            """);

        var nonPackableProj = CreateProjectFile("NonPackable.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
            </Project>
            """);

        var filtered = _filter.FilterPackableProjects([packableProj, nonPackableProj]);

        Assert.Single(filtered);
        Assert.Contains(packableProj, filtered);
    }

    private string CreateProjectFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }
}
