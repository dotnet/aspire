# Release Automation Scripts

This directory contains PowerShell scripts used by the automated release workflows in `.github/workflows/`.

## Scripts

### bump-baseline-version.ps1

Updates the `PackageValidationBaselineVersion` property in `eng/Versions.props` after a release.

**Usage:**
```powershell
./eng/scripts/bump-baseline-version.ps1 -Version "13.1.0"
```

**Parameters:**
- `-Version` (required): The version to set as the baseline
- `-VersionsPropsPath` (optional): Path to Versions.props file (defaults to `eng/Versions.props`)

**What it does:**
- Reads `eng/Versions.props`
- Updates or adds the `PackageValidationBaselineVersion` property
- Preserves XML formatting

**Called by:**
- `.github/workflows/post-release-version-bump.yml` - Automatically after releases
- `.github/workflows/release-coordinator.yml` - As part of release orchestration

---

### update-sample-versions.ps1

Updates all Aspire package references in the aspire-samples repository to a new version.

**Usage:**
```powershell
./eng/scripts/update-sample-versions.ps1 -Version "13.1.0" -SamplesPath "../aspire-samples"
```

**Parameters:**
- `-Version` (required): The Aspire version to update to
- `-SamplesPath` (optional): Path to aspire-samples repository (defaults to current directory)
- `-DryRun` (optional): Preview changes without modifying files

**What it does:**
- Recursively finds all `.csproj` files
- Updates `Aspire.*` package references to the specified version
- Reports summary of changes made

**Example output:**
```
Processing samples/WebApp/WebApp.csproj...
  ✓ Updated 3 Aspire package reference(s)

Summary:
  Files processed: 15
  Files updated: 12
  Total package references updated: 45
```

**Called by:**
- `.github/workflows/update-samples.yml` - Creates PR to aspire-samples
- `.github/workflows/release-coordinator.yml` - As part of release orchestration

---

## Related Workflows

These scripts are used by the following GitHub Actions workflows:

1. **release-coordinator.yml** - Main orchestration workflow
   - Coordinates all release tasks
   - Can selectively run individual tasks
   - Creates release checklist issue

2. **post-release-version-bump.yml** - Version baseline updates
   - Uses: `bump-baseline-version.ps1`
   - Creates PR to bump PackageValidationBaselineVersion

3. **update-samples.yml** - aspire-samples updates
   - Uses: `update-sample-versions.ps1`
   - Creates PR to update package references

4. **update-docker.yml** - dotnet-docker updates
   - Updates dashboard image references
   - Creates PR to dotnet-docker repository

5. **release.yml** - GitHub release creation
   - Triggered by version tags (v*)
   - Builds and publishes CLI archives
   - Creates draft release with release notes

## Development

### Testing Scripts Locally

To test the baseline version bump script:
```powershell
# Dry run to see changes
./eng/scripts/bump-baseline-version.ps1 -Version "13.1.0-test"

# Check the changes
git diff eng/Versions.props

# Revert if needed
git checkout eng/Versions.props
```

To test the sample version update script:
```powershell
# Clone aspire-samples
git clone https://github.com/dotnet/aspire-samples ../aspire-samples

# Dry run to preview changes
./eng/scripts/update-sample-versions.ps1 -Version "13.1.0" -SamplesPath "../aspire-samples" -DryRun

# Actual update
./eng/scripts/update-sample-versions.ps1 -Version "13.1.0" -SamplesPath "../aspire-samples"

# Review changes
cd ../aspire-samples
git diff
```

### Adding New Scripts

When adding new release automation scripts:

1. **Follow naming conventions**: Use descriptive names with hyphens (e.g., `update-something.ps1`)
2. **Add documentation**: Include a synopsis, description, parameters, and examples
3. **Use strict mode**: Start with `Set-StrictMode -Version Latest`
4. **Error handling**: Set `$ErrorActionPreference = "Stop"`
5. **Return exit codes**: Use `exit 0` for success, `exit 1` for errors
6. **Test locally**: Verify the script works before integrating into workflows
7. **Update this README**: Document the new script and its usage

### Troubleshooting

**Script fails with "Versions.props not found":**
- Ensure you're running from the repository root or provide the correct path
- Check that the file path doesn't have typos

**No changes detected in samples repository:**
- Verify the samples repository contains `.csproj` files
- Check that the files contain `Aspire.*` package references
- Ensure the version format matches (e.g., "13.1.0", not "v13.1.0")

**PowerShell execution policy errors:**
- On Windows: `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`
- On Linux/macOS: Scripts should work with `pwsh` by default

## Security Considerations

- Scripts do not handle sensitive credentials directly
- Workflow secrets (VSCE_PAT, GitHub App tokens) are managed by GitHub Actions
- Scripts operate on local file system only
- No network calls are made by these scripts (workflows handle git operations)

## Future Enhancements

Potential improvements to the release automation:

1. **Validation checks**: Add pre-flight checks before creating releases
2. **Rollback capability**: Add scripts to revert failed releases
3. **Release notes generation**: Enhanced changelog generation from commits
4. **Multi-repo sync**: Parallel updates to multiple dependent repositories
5. **Notification system**: Slack/Teams notifications for release milestones
6. **Analytics**: Track release metrics and success rates

## References

- **Automation Plan**: [docs/infra/automation-plan.md](../../docs/infra/automation-plan.md)
- **Release Wiki**: https://github.com/dotnet/aspire/wiki/New-Release-tictoc
- **Workflows Directory**: [.github/workflows/](../../.github/workflows/)
- **Release Checklist Template**: [.github/ISSUE_TEMPLATE/release-checklist.md](../../.github/ISSUE_TEMPLATE/release-checklist.md)
