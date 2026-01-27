// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Mcp.Skills;

namespace Aspire.Cli.Tests.Mcp.Skills;

public class BuiltInSkillsSourceTests
{
    [Fact]
    public async Task ListSkillsAsync_ReturnsAllBuiltInSkills()
    {
        var skills = await BuiltInSkillsSource.ListSkillsAsync();

        Assert.Equal(5, skills.Count);
    }

    [Fact]
    public async Task ListSkillsAsync_AllSkillsHaveRequiredProperties()
    {
        var skills = await BuiltInSkillsSource.ListSkillsAsync();

        foreach (var skill in skills)
        {
            Assert.False(string.IsNullOrWhiteSpace(skill.Name), "Skill name should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(skill.Description), $"Skill {skill.Name} should have a description");
            Assert.StartsWith("skill://", skill.Uri);
        }
    }

    [Fact]
    public async Task GetSkillAsync_AspirePairProgrammer_ContainsExpectedContent()
    {
        var skill = await BuiltInSkillsSource.GetSkillAsync(KnownSkills.AspirePairProgrammer);

        Assert.NotNull(skill);
        Assert.Contains("Aspire", skill.Content);
        Assert.Contains("list_docs", skill.Content);
        Assert.Contains("search_docs", skill.Content);
        Assert.Contains("get_doc", skill.Content);
        Assert.Contains("list_resources", skill.Content);
    }

    [Fact]
    public async Task GetSkillAsync_TroubleshootApp_ContainsExpectedWorkflow()
    {
        var skill = await BuiltInSkillsSource.GetSkillAsync(KnownSkills.TroubleshootApp);

        Assert.NotNull(skill);
        Assert.Contains("Environment Check", skill.Content);
        Assert.Contains("Resource Status", skill.Content);
        Assert.Contains("Log Analysis", skill.Content);
        Assert.Contains("Trace Analysis", skill.Content);
        Assert.Contains("doctor", skill.Content);
    }

    [Fact]
    public async Task GetSkillAsync_DebugResource_ContainsExpectedWorkflow()
    {
        var skill = await BuiltInSkillsSource.GetSkillAsync(KnownSkills.DebugResource);

        Assert.NotNull(skill);
        Assert.Contains("list_resources", skill.Content);
        Assert.Contains("list_console_logs", skill.Content);
        Assert.Contains("list_structured_logs", skill.Content);
        Assert.Contains("list_traces", skill.Content);
    }

    [Fact]
    public async Task GetSkillAsync_AddIntegration_ContainsExpectedContent()
    {
        var skill = await BuiltInSkillsSource.GetSkillAsync(KnownSkills.AddIntegration);

        Assert.NotNull(skill);
        Assert.Contains("list_integrations", skill.Content);
        Assert.Contains("search_docs", skill.Content);
        Assert.Contains("AppHost", skill.Content);
    }

    [Fact]
    public async Task GetSkillAsync_DeployApp_ContainsExpectedContent()
    {
        var skill = await BuiltInSkillsSource.GetSkillAsync(KnownSkills.DeployApp);

        Assert.NotNull(skill);
        Assert.Contains("doctor", skill.Content);
        Assert.Contains("aspire publish", skill.Content);
        Assert.Contains("aspire deploy", skill.Content);
    }

    [Fact]
    public async Task GetSkillAsync_NonexistentSkill_ReturnsNull()
    {
        var skill = await BuiltInSkillsSource.GetSkillAsync("nonexistent-skill");

        Assert.Null(skill);
    }

    [Fact]
    public async Task GetSkillAsync_IsCaseInsensitive()
    {
        var skill1 = await BuiltInSkillsSource.GetSkillAsync("aspire-pair-programmer");
        var skill2 = await BuiltInSkillsSource.GetSkillAsync("ASPIRE-PAIR-PROGRAMMER");

        Assert.NotNull(skill1);
        Assert.NotNull(skill2);
        Assert.Equal(skill1.Content, skill2.Content);
    }
}
