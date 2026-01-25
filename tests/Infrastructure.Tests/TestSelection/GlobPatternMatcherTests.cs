// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Infrastructure.Tests.Helpers;
using Xunit;

namespace Infrastructure.Tests.TestSelection;

/// <summary>
/// Tests for glob pattern matching functionality.
/// </summary>
public class GlobPatternMatcherTests
{
    [Theory]
    [InlineData("*.md", "^[^/]*\\.md$")]
    [InlineData("**/*.cs", "^.*/[^/]*\\.cs$")]
    [InlineData("src/**", "^src/.*$")]
    [InlineData("docs/**", "^docs/.*$")]
    [InlineData("*.?", "^[^/]*\\..$")]
    [InlineData("file.txt", "^file\\.txt$")]
    public void ConvertGlobToRegex_ProducesExpectedPattern(string glob, string expectedRegex)
    {
        var result = GlobPatternMatcher.ConvertGlobToRegex(glob);
        Assert.Equal(expectedRegex, result);
    }

    [Theory]
    // Single wildcard tests
    [InlineData("*.md", "README.md", true)]
    [InlineData("*.md", "CONTRIBUTING.md", true)]
    [InlineData("*.md", "src/README.md", false)] // * doesn't match /
    [InlineData("*.cs", "Program.cs", true)]
    [InlineData("*.cs", "Program.txt", false)]
    // Double wildcard tests
    [InlineData("**/*.cs", "src/Program.cs", true)]
    [InlineData("**/*.cs", "src/nested/deep/File.cs", true)]
    [InlineData("**/*.cs", "Program.cs", false)] // ** matches any path, but /**/ requires at least one /
    [InlineData("src/**", "src/file.txt", true)]
    [InlineData("src/**", "src/nested/file.txt", true)]
    [InlineData("src/**", "tests/file.txt", false)]
    // Path matching
    [InlineData("docs/**", "docs/getting-started.md", true)]
    [InlineData("docs/**", "docs/api/reference.md", true)]
    [InlineData("docs/**", "README.md", false)]
    // Exact matching
    [InlineData("global.json", "global.json", true)]
    [InlineData("global.json", "other.json", false)]
    [InlineData("Directory.Build.props", "Directory.Build.props", true)]
    // Complex patterns from test-selection-rules.json
    [InlineData("eng/**", "eng/Version.Details.xml", true)]
    [InlineData("eng/**", "eng/scripts/test.ps1", true)]
    [InlineData(".github/workflows/**", ".github/workflows/ci.yml", true)]
    [InlineData(".github/actions/**", ".github/actions/setup/action.yml", true)]
    [InlineData("tests/Shared/**", "tests/Shared/TestHelper.cs", true)]
    [InlineData("*.sln", "Aspire.sln", true)]
    [InlineData("*.slnx", "Aspire.slnx", true)]
    [InlineData("src/Aspire.Hosting/**", "src/Aspire.Hosting/Resource.cs", true)]
    [InlineData("src/Aspire.ProjectTemplates/**", "src/Aspire.ProjectTemplates/templates/Program.cs", true)]
    [InlineData("tests/Aspire.*.Tests/**", "tests/Aspire.Dashboard.Tests/DashboardTests.cs", true)]
    [InlineData("tests/Aspire.*.Tests/**", "tests/Aspire.Hosting.Redis.Tests/RedisTests.cs", true)]
    [InlineData("src/Aspire.*/**", "src/Aspire.Dashboard/Components/Layout.razor", true)]
    [InlineData("extension/**", "extension/package.json", true)]
    [InlineData("extension/**", "extension/src/extension.ts", true)]
    [InlineData("playground/**", "playground/TestShop/Program.cs", true)]
    public void IsMatch_MatchesCorrectly(string pattern, string filePath, bool expectedMatch)
    {
        var result = GlobPatternMatcher.IsMatch(filePath, pattern);
        Assert.Equal(expectedMatch, result);
    }

    [Theory]
    [InlineData("src/Components/{name}/**", "src/Components/Aspire.Npgsql/NpgsqlExtensions.cs", true, "Aspire.Npgsql")]
    [InlineData("src/Components/{name}/**", "src/Components/Aspire.Microsoft.Data.SqlClient/SqlClient.cs", true, "Aspire.Microsoft.Data.SqlClient")]
    [InlineData("src/Aspire.Hosting.{name}/**", "src/Aspire.Hosting.Redis/RedisExtensions.cs", true, "Redis")]
    [InlineData("src/Aspire.Hosting.{name}/**", "src/Aspire.Hosting.PostgreSQL/PostgresExtensions.cs", true, "PostgreSQL")]
    [InlineData("tests/{name}.Tests/**", "tests/Aspire.Dashboard.Tests/DashboardTests.cs", true, "Aspire.Dashboard")]
    [InlineData("tests/{name}.Tests/**", "tests/Aspire.Hosting.Redis.Tests/RedisTests.cs", true, "Aspire.Hosting.Redis")]
    [InlineData("src/Components/{name}/**", "src/Other/Aspire.Npgsql/File.cs", false, null)]
    public void TryMatchSourcePattern_ExtractsName(string pattern, string filePath, bool expectedMatch, string? expectedName)
    {
        var result = GlobPatternMatcher.TryMatchSourcePattern(filePath, pattern, out var capturedName);
        Assert.Equal(expectedMatch, result);
        Assert.Equal(expectedName, capturedName);
    }

    [Theory]
    [InlineData("README.md", new[] { "*.md", "docs/**" }, true)]
    [InlineData("src/file.cs", new[] { "*.md", "docs/**" }, false)]
    [InlineData("docs/api/ref.md", new[] { "*.md", "docs/**" }, true)]
    [InlineData("eng/scripts/test.ps1", new[] { "eng/**", ".github/**" }, true)]
    public void MatchesAny_MatchesAnyPattern(string filePath, string[] patterns, bool expectedMatch)
    {
        var result = GlobPatternMatcher.MatchesAny(filePath, patterns);
        Assert.Equal(expectedMatch, result);
    }

    [Theory]
    // Edge case: special regex characters in file names
    [InlineData("file[1].txt", "file[1].txt", true)]
    [InlineData("file(1).txt", "file(1).txt", true)]
    [InlineData("file+1.txt", "file+1.txt", true)]
    [InlineData("file$1.txt", "file$1.txt", true)]
    // Edge case: question mark matches single character
    [InlineData("file?.txt", "file1.txt", true)]
    [InlineData("file?.txt", "filea.txt", true)]
    [InlineData("file?.txt", "file12.txt", false)]
    public void IsMatch_HandlesEdgeCases(string pattern, string filePath, bool expectedMatch)
    {
        var result = GlobPatternMatcher.IsMatch(filePath, pattern);
        Assert.Equal(expectedMatch, result);
    }
}
