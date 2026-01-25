// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Analyzers;
using Xunit;

namespace Aspire.TestSelector.Tests.Analyzers;

public class CriticalFileDetectorTests
{
    [Theory]
    [InlineData("eng/**", "eng/Build.props", true)]
    [InlineData("eng/**", "eng/pipelines/ci.yml", true)]
    [InlineData("eng/**", "src/file.cs", false)]
    [InlineData("Directory.Build.props", "Directory.Build.props", true)]
    [InlineData("Directory.Build.props", "src/Directory.Build.props", false)]
    [InlineData("**/Directory.Build.props", "src/Directory.Build.props", true)]
    public void IsCriticalFile_WithTriggerPattern_ReturnsExpected(string pattern, string filePath, bool expected)
    {
        var detector = new CriticalFileDetector([pattern], []);

        var result = detector.IsCriticalFile(filePath, out _);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsCriticalFile_ExcludePatternTakesPrecedence()
    {
        var detector = new CriticalFileDetector(
            triggerAllPatterns: ["eng/**"],
            excludePatterns: ["eng/pipelines/**"]);

        Assert.True(detector.IsCriticalFile("eng/Build.props", out _));
        Assert.False(detector.IsCriticalFile("eng/pipelines/ci.yml", out _));
        Assert.False(detector.IsCriticalFile("eng/pipelines/pr.yml", out _));
    }

    [Theory]
    [InlineData("eng\\Build.props")]
    [InlineData("eng/Build.props")]
    public void IsCriticalFile_NormalizesPathSeparators(string filePath)
    {
        var detector = new CriticalFileDetector(["eng/**"], []);

        var result = detector.IsCriticalFile(filePath, out _);

        Assert.True(result);
    }

    [Fact]
    public void FindFirstCriticalFile_ReturnsFirstMatch()
    {
        var detector = new CriticalFileDetector(["eng/**", "*.props"], []);

        var files = new[]
        {
            "src/code.cs",
            "eng/Build.props",
            "Directory.Build.props",
            "README.md"
        };

        var (file, pattern) = detector.FindFirstCriticalFile(files);

        Assert.Equal("eng/Build.props", file);
        Assert.NotNull(pattern);
    }

    [Fact]
    public void FindFirstCriticalFile_NoMatch_ReturnsNull()
    {
        var detector = new CriticalFileDetector(["eng/**"], []);

        var files = new[] { "src/code.cs", "tests/Test.cs" };

        var (file, pattern) = detector.FindFirstCriticalFile(files);

        Assert.Null(file);
        Assert.Null(pattern);
    }

    [Fact]
    public void FindAllCriticalFiles_ReturnsAllMatches()
    {
        var detector = new CriticalFileDetector(["eng/**", "*.props"], []);

        var files = new[]
        {
            "src/code.cs",
            "eng/Build.props",
            "Directory.Build.props",
            "eng/ci.yml",
            "README.md"
        };

        var criticalFiles = detector.FindAllCriticalFiles(files);

        Assert.Equal(3, criticalFiles.Count);
        Assert.Contains(criticalFiles, cf => cf.File == "eng/Build.props");
        Assert.Contains(criticalFiles, cf => cf.File == "Directory.Build.props");
        Assert.Contains(criticalFiles, cf => cf.File == "eng/ci.yml");
    }

    [Fact]
    public void IsCriticalFile_ReportsMatchedPattern()
    {
        var detector = new CriticalFileDetector(
            triggerAllPatterns: ["eng/**/*.props", "*.targets"],
            excludePatterns: []);

        detector.IsCriticalFile("eng/Build.props", out var pattern);

        Assert.Equal("eng/**/*.props", pattern);
    }

    [Fact]
    public void IsCriticalFile_NoPatterns_ReturnsFalse()
    {
        var detector = new CriticalFileDetector([], []);

        Assert.False(detector.IsCriticalFile("any/file.cs", out _));
    }

    [Fact]
    public void TriggerPatterns_ReturnsConfiguredPatterns()
    {
        var patterns = new[] { "eng/**", "*.props" };
        var detector = new CriticalFileDetector(patterns, []);

        Assert.Equal(2, detector.TriggerPatterns.Count);
        Assert.Contains("eng/**", detector.TriggerPatterns);
    }

    [Fact]
    public void ExcludePatterns_ReturnsConfiguredPatterns()
    {
        var excludePatterns = new[] { "eng/pipelines/**" };
        var detector = new CriticalFileDetector(["eng/**"], excludePatterns);

        Assert.Single(detector.ExcludePatterns);
        Assert.Contains("eng/pipelines/**", detector.ExcludePatterns);
    }

    [Fact]
    public void IsCriticalFile_MultipleExcludes_AllRespected()
    {
        var detector = new CriticalFileDetector(
            triggerAllPatterns: ["eng/**"],
            excludePatterns: ["eng/pipelines/**", "eng/docs/**", "eng/README.md"]);

        Assert.True(detector.IsCriticalFile("eng/Build.props", out _));
        Assert.False(detector.IsCriticalFile("eng/pipelines/ci.yml", out _));
        Assert.False(detector.IsCriticalFile("eng/docs/guide.md", out _));
        Assert.False(detector.IsCriticalFile("eng/README.md", out _));
    }

    [Fact]
    public void FindAllCriticalFiles_WithExcludes_FiltersCorrectly()
    {
        var detector = new CriticalFileDetector(
            triggerAllPatterns: ["eng/**"],
            excludePatterns: ["eng/pipelines/**"]);

        var files = new[]
        {
            "eng/Build.props",
            "eng/pipelines/ci.yml",
            "eng/Versions.props"
        };

        var criticalFiles = detector.FindAllCriticalFiles(files);

        Assert.Equal(2, criticalFiles.Count);
        Assert.Contains(criticalFiles, cf => cf.File == "eng/Build.props");
        Assert.Contains(criticalFiles, cf => cf.File == "eng/Versions.props");
        Assert.DoesNotContain(criticalFiles, cf => cf.File == "eng/pipelines/ci.yml");
    }
}
