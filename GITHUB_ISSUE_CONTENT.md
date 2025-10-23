# Add test coverage for `--localhost-tld` template option

## Description
PR #12267 introduces a new `--localhost-tld` option for Aspire templates. We should add test coverage for this new template option.

## Context
From https://github.com/dotnet/aspire/pull/12267#issuecomment-3438427114:
> "We could add a new case which is the `--localhost-tld` option." - @DamianEdwards

## Changes in PR #12267
- Aspire AppHost templates now use `*.dev.localhost` URLs by default
- New `--localhost-tld` option added to control this behavior
- `ApplicationOrchestrator` prioritizes `*.localhost` URLs in dashboard

## Proposed Tests

Test cases to add in `tests/Aspire.Templates.Tests/`:

1. Verify default behavior uses `*.dev.localhost` URLs
2. Verify `--localhost-tld <value>` generates correct URLs in launch profiles
3. Verify opting out (e.g., `--localhost-tld false`) uses plain localhost
4. Verify projects build successfully with different option values
5. Test across relevant template types (aspire-starter, aspire-empty, aspire)

## Example Test

```csharp
[Theory]
[InlineData("dev.localhost")]
[InlineData("false")]
public async Task TemplateWithLocalhostTldOption_GeneratesCorrectLaunchProfile(string optionValue)
{
    string projectId = $"test-tld-{optionValue.Replace(".", "_")}";
    await using var project = await AspireProject.CreateNewTemplateProjectAsync(
        projectId,
        "aspire-starter",
        _testOutput,
        BuildEnvironment.ForDefaultFramework,
        extraArgs: $"--localhost-tld {optionValue}");
    
    // Verify launchSettings.json contains expected URL format
    // Build to ensure validity
    await project.BuildAsync(workingDirectory: project.RootDir);
}
```

## Acceptance Criteria
- [ ] Tests for `--localhost-tld` with various values
- [ ] Tests verify default behavior
- [ ] Tests verify launch profile URLs
- [ ] Tests verify projects build successfully
- [ ] Tests follow existing `Aspire.Templates.Tests` patterns
