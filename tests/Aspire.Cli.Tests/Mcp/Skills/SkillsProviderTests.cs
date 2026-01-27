// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Mcp.Skills;
using Aspire.Cli.Tests.TestServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Mcp.Skills;

public class SkillsProviderTests
{
    private static SkillsProvider CreateProvider() =>
        new(NullLogger<SkillsProvider>.Instance, new TestConsoleInteractionService());

    [Fact]
    public async Task ListSkillsAsync_ReturnsBuiltInSkills()
    {
        var provider = CreateProvider();

        var skills = await provider.ListSkillsAsync();

        Assert.NotEmpty(skills);
        Assert.Contains(skills, s => s.Name == KnownSkills.AspirePairProgrammer);
        Assert.Contains(skills, s => s.Name == KnownSkills.TroubleshootApp);
        Assert.Contains(skills, s => s.Name == KnownSkills.DebugResource);
        Assert.Contains(skills, s => s.Name == KnownSkills.AddIntegration);
        Assert.Contains(skills, s => s.Name == KnownSkills.DeployApp);
    }

    [Fact]
    public async Task ListSkillsAsync_AllSkillsHaveDescriptions()
    {
        var provider = CreateProvider();

        var skills = await provider.ListSkillsAsync();

        foreach (var skill in skills)
        {
            Assert.False(string.IsNullOrWhiteSpace(skill.Description), $"Skill {skill.Name} has no description");
        }
    }

    [Fact]
    public async Task ListSkillsAsync_AllSkillsHaveValidUris()
    {
        var provider = CreateProvider();

        var skills = await provider.ListSkillsAsync();

        foreach (var skill in skills)
        {
            Assert.StartsWith("skill://", skill.Uri);
            Assert.Equal($"skill://{skill.Name}", skill.Uri);
        }
    }

    [Fact]
    public async Task GetSkillAsync_WithValidName_ReturnsSkill()
    {
        var provider = CreateProvider();

        var skill = await provider.GetSkillAsync(KnownSkills.AspirePairProgrammer);

        Assert.NotNull(skill);
        Assert.Equal(KnownSkills.AspirePairProgrammer, skill.Name);
        Assert.Equal("text/markdown", skill.MimeType);
        Assert.False(string.IsNullOrWhiteSpace(skill.Content));
    }

    [Fact]
    public async Task GetSkillAsync_WithInvalidName_ReturnsNull()
    {
        var provider = CreateProvider();

        var skill = await provider.GetSkillAsync("nonexistent-skill");

        Assert.Null(skill);
    }

    [Fact]
    public async Task GetSkillAsync_IsCaseInsensitive()
    {
        var provider = CreateProvider();

        var skill1 = await provider.GetSkillAsync("aspire-pair-programmer");
        var skill2 = await provider.GetSkillAsync("ASPIRE-PAIR-PROGRAMMER");
        var skill3 = await provider.GetSkillAsync("Aspire-Pair-Programmer");

        Assert.NotNull(skill1);
        Assert.NotNull(skill2);
        Assert.NotNull(skill3);
        Assert.Equal(skill1.Content, skill2.Content);
        Assert.Equal(skill2.Content, skill3.Content);
    }

    [Theory]
    [InlineData(KnownSkills.AspirePairProgrammer)]
    [InlineData(KnownSkills.TroubleshootApp)]
    [InlineData(KnownSkills.DebugResource)]
    [InlineData(KnownSkills.AddIntegration)]
    [InlineData(KnownSkills.DeployApp)]
    public async Task GetSkillAsync_AllKnownSkills_HaveContent(string skillName)
    {
        var provider = CreateProvider();

        var skill = await provider.GetSkillAsync(skillName);

        Assert.NotNull(skill);
        Assert.False(string.IsNullOrWhiteSpace(skill.Content));
        Assert.Contains("#", skill.Content); // Should have markdown headers
    }

    [Theory]
    [InlineData("skill://aspire-pair-programmer", "aspire-pair-programmer")]
    [InlineData("skill://troubleshoot-app", "troubleshoot-app")]
    [InlineData("skill://debug-resource", "debug-resource")]
    [InlineData("skill://add-integration", "add-integration")]
    [InlineData("skill://deploy-app", "deploy-app")]
    public void ParseSkillName_WithValidUri_ReturnsSkillName(string uri, string expectedName)
    {
        var result = SkillsProvider.ParseSkillName(uri);

        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com")]
    [InlineData("resource://something")]
    [InlineData("skill://")]
    public void ParseSkillName_WithInvalidUri_ReturnsNull(string? uri)
    {
        var result = SkillsProvider.ParseSkillName(uri);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("skill://my-skill?param=value", "my-skill")]
    [InlineData("skill://my-skill#section", "my-skill")]
    [InlineData("skill://my-skill?param=value#section", "my-skill")]
    public void ParseSkillName_WithQueryOrFragment_StripsExtras(string uri, string expectedName)
    {
        var result = SkillsProvider.ParseSkillName(uri);

        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetDefaultSkillsDirectory_ReturnsAspireSkillsPath()
    {
        var provider = CreateProvider();

        var directory = provider.GetDefaultSkillsDirectory();

        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspire", "skills");
        Assert.Equal(expected, directory);
    }

    [Fact]
    public async Task SaveSkillAsync_CreatesSkillFile()
    {
        var provider = CreateProvider();
        var tempDir = Path.Combine(Path.GetTempPath(), $"skills-test-{Guid.NewGuid()}");

        try
        {
            var skillName = "test-skill";
            var content = "# Test Skill\n\nThis is a test skill.";

            var savedPath = await provider.SaveSkillAsync(skillName, content, targetDirectory: tempDir);

            Assert.True(File.Exists(savedPath));
            var fileContent = await File.ReadAllTextAsync(savedPath);
            Assert.Contains("---", fileContent);
            Assert.Contains($"name: {skillName}", fileContent);
            Assert.Contains("# Test Skill", fileContent);
            Assert.Equal(Path.Combine(tempDir, skillName, "SKILL.md"), savedPath);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveSkillAsync_WithDescription_AddsFrontmatter()
    {
        var provider = CreateProvider();
        var tempDir = Path.Combine(Path.GetTempPath(), $"skills-test-{Guid.NewGuid()}");

        try
        {
            var skillName = "test-skill-with-desc";
            var content = "# Test Skill\n\nThis is a test skill.";
            var description = "A helpful test skill";

            var savedPath = await provider.SaveSkillAsync(skillName, content, description: description, targetDirectory: tempDir);

            Assert.True(File.Exists(savedPath));
            var fileContent = await File.ReadAllTextAsync(savedPath);
            Assert.Contains("---", fileContent);
            Assert.Contains($"name: {skillName}", fileContent);
            Assert.Contains($"description: {description}", fileContent);
            Assert.Contains("# Test Skill", fileContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveSkillAsync_WithNullSkillName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        // ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null values
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.SaveSkillAsync(null!, "content", targetDirectory: Path.GetTempPath()).AsTask());
    }

    [Fact]
    public async Task SaveSkillAsync_WithEmptyContent_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.SaveSkillAsync("skill-name", "", targetDirectory: Path.GetTempPath()).AsTask());
    }
}
