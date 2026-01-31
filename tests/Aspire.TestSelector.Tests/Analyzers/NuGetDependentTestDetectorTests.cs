// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Analyzers;
using Xunit;

namespace Aspire.TestSelector.Tests.Analyzers;

public class NuGetDependentTestDetectorTests
{
    private static string GetFixturesDir()
    {
        // The test fixtures are copied to the output directory
        return Path.Combine(AppContext.BaseDirectory, "TestFixtures");
    }

    [Fact]
    public void IsPackableProject_ReturnsTrueForPackableProject()
    {
        var path = Path.Combine(GetFixturesDir(), "PackableProject.csproj");
        Assert.True(NuGetDependentTestDetector.IsPackableProject(path));
    }

    [Fact]
    public void IsPackableProject_ReturnsFalseForNonPackableProject()
    {
        var path = Path.Combine(GetFixturesDir(), "TestProject.csproj");
        Assert.False(NuGetDependentTestDetector.IsPackableProject(path));
    }

    [Fact]
    public void IsPackableProject_ReturnsFalseForConditionalIsPackable()
    {
        var path = Path.Combine(GetFixturesDir(), "ConditionalPackableProject.csproj");
        // Conditional <IsPackable> should not count as definitively packable
        Assert.False(NuGetDependentTestDetector.IsPackableProject(path));
    }

    [Fact]
    public void IsPackableProject_ReturnsFalseForMissingFile()
    {
        Assert.False(NuGetDependentTestDetector.IsPackableProject("/nonexistent/path.csproj"));
    }

    [Fact]
    public void HasRequiredNuGetsForTesting_ReturnsTrueForNuGetDependentProject()
    {
        var path = Path.Combine(GetFixturesDir(), "NuGetDependentTestProject.csproj");
        Assert.True(NuGetDependentTestDetector.HasRequiredNuGetsForTesting(path));
    }

    [Fact]
    public void HasRequiredNuGetsForTesting_ReturnsFalseForRegularProject()
    {
        var path = Path.Combine(GetFixturesDir(), "TestProject.csproj");
        Assert.False(NuGetDependentTestDetector.HasRequiredNuGetsForTesting(path));
    }

    [Fact]
    public void Detect_ReturnsNull_WhenNoProducersAffected()
    {
        var detector = new NuGetDependentTestDetector(["eng/clipack/**"]);

        var result = detector.Detect(
            affectedProjects: ["tests/Foo.Tests/Foo.Tests.csproj"],
            activeFiles: ["src/Foo/Bar.cs"],
            workingDir: "/nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void Detect_MatchesGlobPattern()
    {
        // Create a temp directory structure to test with
        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-test-{Guid.NewGuid():N}");
        try
        {
            // Set up test directory with a nuget-dependent test project
            var testsDir = Path.Combine(tempDir, "tests", "FakeNuGetTests");
            Directory.CreateDirectory(testsDir);
            File.WriteAllText(Path.Combine(testsDir, "FakeNuGetTests.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <RequiredNuGetsForTesting>true</RequiredNuGetsForTesting>
                  </PropertyGroup>
                </Project>
                """);

            var detector = new NuGetDependentTestDetector(["eng/clipack/**"]);
            var result = detector.Detect(
                affectedProjects: [],
                activeFiles: ["eng/clipack/build.sh"],
                workingDir: tempDir);

            Assert.NotNull(result);
            Assert.True(result.Triggered);
            Assert.Contains("eng/clipack/build.sh", result.AffectedPackableProjects);
            Assert.Contains("tests/FakeNuGetTests/", result.Projects);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Detect_MatchesIsPackableCsproj()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-test-{Guid.NewGuid():N}");
        try
        {
            // Set up a packable source project
            var srcDir = Path.Combine(tempDir, "src", "MyLib");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "MyLib.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <IsPackable>true</IsPackable>
                  </PropertyGroup>
                </Project>
                """);

            // Set up a nuget-dependent test project
            var testsDir = Path.Combine(tempDir, "tests", "NuGetTests");
            Directory.CreateDirectory(testsDir);
            File.WriteAllText(Path.Combine(testsDir, "NuGetTests.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <RequiredNuGetsForTesting>true</RequiredNuGetsForTesting>
                  </PropertyGroup>
                </Project>
                """);

            var detector = new NuGetDependentTestDetector([]);
            var result = detector.Detect(
                affectedProjects: ["src/MyLib/MyLib.csproj"],
                activeFiles: [],
                workingDir: tempDir);

            Assert.NotNull(result);
            Assert.True(result.Triggered);
            Assert.Contains("src/MyLib/MyLib.csproj", result.AffectedPackableProjects);
            Assert.Contains("tests/NuGetTests/", result.Projects);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Detect_ReturnsNull_WhenEmptyPatternsAndNoCsprojCandidates()
    {
        // No glob patterns configured, and no .csproj paths in candidates
        var detector = new NuGetDependentTestDetector([]);

        var result = detector.Detect(
            affectedProjects: [],
            activeFiles: ["src/Foo/Bar.cs", "eng/scripts/build.sh"],
            workingDir: "/nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void Detect_SkipsNonPackableCsproj()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-test-{Guid.NewGuid():N}");
        try
        {
            // Set up a non-packable source project (IsPackable=false explicitly)
            var srcDir = Path.Combine(tempDir, "src", "MyTestLib");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "MyTestLib.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <IsPackable>false</IsPackable>
                  </PropertyGroup>
                </Project>
                """);

            // Set up a nuget-dependent test project (should NOT be triggered)
            var testsDir = Path.Combine(tempDir, "tests", "NuGetTests");
            Directory.CreateDirectory(testsDir);
            File.WriteAllText(Path.Combine(testsDir, "NuGetTests.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <RequiredNuGetsForTesting>true</RequiredNuGetsForTesting>
                  </PropertyGroup>
                </Project>
                """);

            var detector = new NuGetDependentTestDetector([]);
            var result = detector.Detect(
                affectedProjects: ["src/MyTestLib/MyTestLib.csproj"],
                activeFiles: [],
                workingDir: tempDir);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Detect_DeduplicatesCandidatesFromBothSources()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-test-{Guid.NewGuid():N}");
        try
        {
            var srcDir = Path.Combine(tempDir, "src", "MyLib");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "MyLib.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <IsPackable>true</IsPackable>
                  </PropertyGroup>
                </Project>
                """);

            var testsDir = Path.Combine(tempDir, "tests", "NuGetTests");
            Directory.CreateDirectory(testsDir);
            File.WriteAllText(Path.Combine(testsDir, "NuGetTests.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <RequiredNuGetsForTesting>true</RequiredNuGetsForTesting>
                  </PropertyGroup>
                </Project>
                """);

            // Same csproj appears in both affectedProjects and activeFiles
            var detector = new NuGetDependentTestDetector([]);
            var result = detector.Detect(
                affectedProjects: ["src/MyLib/MyLib.csproj"],
                activeFiles: ["src/MyLib/MyLib.csproj"],
                workingDir: tempDir);

            Assert.NotNull(result);
            // Should only appear once due to Distinct()
            Assert.Single(result.AffectedPackableProjects);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Detect_FindsMultipleNuGetDependentTestProjects()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-test-{Guid.NewGuid():N}");
        try
        {
            var testsDir1 = Path.Combine(tempDir, "tests", "Templates.Tests");
            var testsDir2 = Path.Combine(tempDir, "tests", "EndToEnd.Tests");
            var testsDir3 = Path.Combine(tempDir, "tests", "Regular.Tests");
            Directory.CreateDirectory(testsDir1);
            Directory.CreateDirectory(testsDir2);
            Directory.CreateDirectory(testsDir3);

            File.WriteAllText(Path.Combine(testsDir1, "Templates.Tests.csproj"),
                """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><RequiredNuGetsForTesting>true</RequiredNuGetsForTesting></PropertyGroup></Project>""");
            File.WriteAllText(Path.Combine(testsDir2, "EndToEnd.Tests.csproj"),
                """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><RequiredNuGetsForTesting>true</RequiredNuGetsForTesting></PropertyGroup></Project>""");
            File.WriteAllText(Path.Combine(testsDir3, "Regular.Tests.csproj"),
                """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup></Project>""");

            var detector = new NuGetDependentTestDetector(["eng/clipack/**"]);
            var result = detector.Detect(
                affectedProjects: [],
                activeFiles: ["eng/clipack/build.sh"],
                workingDir: tempDir);

            Assert.NotNull(result);
            Assert.Equal(2, result.Projects.Count);
            Assert.Contains("tests/Templates.Tests/", result.Projects);
            Assert.Contains("tests/EndToEnd.Tests/", result.Projects);
            Assert.DoesNotContain("tests/Regular.Tests/", result.Projects);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FindNuGetDependentTestProjects_ReturnsEmpty_WhenNoTestsDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = NuGetDependentTestDetector.FindNuGetDependentTestProjects(tempDir);
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsPackableProject_ReturnsFalseForExplicitlyNotPackable()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var csprojPath = Path.Combine(tempDir, "NotPackable.csproj");
            File.WriteAllText(csprojPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <IsPackable>false</IsPackable>
                  </PropertyGroup>
                </Project>
                """);
            Assert.False(NuGetDependentTestDetector.IsPackableProject(csprojPath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsPackableProject_ReturnsFalseForProjectWithNoIsPackableElement()
    {
        var path = Path.Combine(GetFixturesDir(), "SourceProject.csproj");
        Assert.False(NuGetDependentTestDetector.IsPackableProject(path));
    }

    [Fact]
    public void HasRequiredNuGetsForTesting_ReturnsFalseForMissingFile()
    {
        Assert.False(NuGetDependentTestDetector.HasRequiredNuGetsForTesting("/nonexistent/path.csproj"));
    }

    [Fact]
    public void Detect_ReturnsNull_WhenNoNuGetDependentTestsExist()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-test-{Guid.NewGuid():N}");
        try
        {
            // Set up a packable source project but no nuget-dependent test projects
            var srcDir = Path.Combine(tempDir, "src", "MyLib");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "MyLib.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <IsPackable>true</IsPackable>
                  </PropertyGroup>
                </Project>
                """);

            var testsDir = Path.Combine(tempDir, "tests", "RegularTests");
            Directory.CreateDirectory(testsDir);
            File.WriteAllText(Path.Combine(testsDir, "RegularTests.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <IsTestProject>true</IsTestProject>
                  </PropertyGroup>
                </Project>
                """);

            var detector = new NuGetDependentTestDetector([]);
            var result = detector.Detect(
                affectedProjects: ["src/MyLib/MyLib.csproj"],
                activeFiles: [],
                workingDir: tempDir);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
