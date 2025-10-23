# Follow-up Issue: Add test coverage for `--localhost-tld` template option

**Note**: This file documents a follow-up issue that should be created on GitHub for PR #12267.

## Issue Title
Add test coverage for `--localhost-tld` template option

## Issue Labels
- `area-templates`
- `enhancement`
- `testing`

## Issue Description

PR #12267 introduces a new `--localhost-tld` option for Aspire templates that allows users to configure whether to use `*.dev.localhost` URLs for the AppHost. We should add test coverage for this new template option to ensure it works correctly across different scenarios.

### Related Work

- PR: #12267 - Update the templates for .NET 10
- Comment: https://github.com/dotnet/aspire/pull/12267#issuecomment-3438427114

From @DamianEdwards:
> Same combinations apply and are covered. Tests found a bunch of stuff while I was making this so they're doing their job. We could add a new case which is the `--localhost-tld` option.

### Background

In PR #12267, Aspire templates were updated for .NET 10 with the following changes:
- All Aspire AppHost templates now use a `*.dev.localhost` URL by default (e.g., `mygreataspireapp_apphost.dev.localhost`)
- A new `--localhost-tld` option was added to allow users to control this behavior
- The `ApplicationOrchestrator` was updated to prioritize `*.localhost` resource URLs in the dashboard

### Proposed Test Cases

Add test cases in the `Aspire.Templates.Tests` project to verify:

1. **Default behavior**: Templates created without `--localhost-tld` option should use `*.dev.localhost` URLs by default
2. **Explicit option**: Templates created with `--localhost-tld` set to specific values work correctly
3. **Opting out**: Templates created with `--localhost-tld false` or equivalent should use plain `localhost` URLs
4. **Launch profile verification**: Generated launch profiles contain the correct URL format based on the option
5. **Build and run**: Projects created with different `--localhost-tld` values can build and run successfully

### Implementation Notes

The tests should follow the existing pattern in `Aspire.Templates.Tests`:
- Use `AspireProject.CreateNewTemplateProjectAsync()` with the `extraArgs` parameter to pass `--localhost-tld` option
- Test with different template types (aspire-starter, aspire-empty, aspire, etc.)
- Verify the generated `launchSettings.json` files contain expected URLs
- Consider testing across different target frameworks if relevant

**Example test structure:**
```csharp
[Theory]
[InlineData("dev.localhost")]
[InlineData("false")]
public async Task TemplateWithLocalhostTldOption_GeneratesCorrectLaunchProfile(string localhostTldValue)
{
    string projectId = $"test-localhost-tld-{localhostTldValue.Replace(".", "_")}";
    await using var project = await AspireProject.CreateNewTemplateProjectAsync(
        projectId,
        "aspire-starter",
        _testOutput,
        BuildEnvironment.ForDefaultFramework,
        extraArgs: $"--localhost-tld {localhostTldValue}");
    
    // Verify launch profile contains expected URL format
    var launchSettingsPath = Path.Combine(
        project.AppHostProjectDirectory,
        "Properties",
        "launchSettings.json");
    
    Assert.True(File.Exists(launchSettingsPath), "launchSettings.json should exist");
    var launchSettingsContent = File.ReadAllText(launchSettingsPath);
    
    // Verify URL format based on option value
    if (localhostTldValue == "false")
    {
        Assert.DoesNotContain(".dev.localhost", launchSettingsContent);
        Assert.Contains("localhost", launchSettingsContent);
    }
    else
    {
        Assert.Contains($".{localhostTldValue}", launchSettingsContent);
    }
    
    // Build the project to ensure it's valid
    await project.BuildAsync(workingDirectory: project.RootDir);
}

[Fact]
public async Task TemplateWithoutLocalhostTldOption_UsesDevLocalhostByDefault()
{
    string projectId = "test-localhost-tld-default";
    await using var project = await AspireProject.CreateNewTemplateProjectAsync(
        projectId,
        "aspire-starter",
        _testOutput,
        BuildEnvironment.ForDefaultFramework);
    
    // Verify launch profile contains .dev.localhost by default
    var launchSettingsPath = Path.Combine(
        project.AppHostProjectDirectory,
        "Properties",
        "launchSettings.json");
    
    Assert.True(File.Exists(launchSettingsPath), "launchSettings.json should exist");
    var launchSettingsContent = File.ReadAllText(launchSettingsPath);
    Assert.Contains(".dev.localhost", launchSettingsContent);
    
    // Build the project to ensure it's valid
    await project.BuildAsync(workingDirectory: project.RootDir);
}
```

### Test File Location
Add tests to: `tests/Aspire.Templates.Tests/AppHostTemplateTests.cs` or create a new test file like `tests/Aspire.Templates.Tests/LocalhostTldOptionTests.cs`

### Acceptance Criteria

- [ ] Tests added for `--localhost-tld` option with various values (e.g., "dev.localhost", "false")
- [ ] Tests verify default behavior (*.dev.localhost URLs when option not specified)
- [ ] Tests verify launch profile URLs match the option value
- [ ] Tests verify projects build successfully with different option values
- [ ] Tests follow existing patterns in `Aspire.Templates.Tests`
- [ ] Tests pass on all supported platforms
- [ ] Tests are added for all relevant template types (aspire-starter, aspire-empty, aspire, etc.)

## Instructions for Creating the Issue

1. Go to: https://github.com/dotnet/aspire/issues/new/choose
2. Select "Feature request" template
3. Copy the content from the "Issue Description" section above
4. Apply the labels: `area-templates`, `enhancement`, `testing`
5. Link to PR #12267 in the issue description
6. Assign to appropriate team member(s) if needed

---

**This file should be deleted after the GitHub issue is created.**
