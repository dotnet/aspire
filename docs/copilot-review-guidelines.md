# Copilot-Assisted Review Guidelines

This document provides structured guidelines for Copilot-assisted reviews and contributors in the `dotnet/aspire` repository. These guidelines help ensure consistent and thorough validation processes for different types of changes.

## Overview

When reviewing or authoring pull requests, different types of changes require specific validation steps and considerations. This document outlines actionable checklists and processes for various scenarios to ensure critical validation and testing steps are not overlooked.

## 1. Infrastructure-Related Changes

**When this applies:** Changes affecting build processes, test execution, infrastructure configurations, or Azure DevOps (AzDO) workflows.

### Relevant Files and Directories

- `eng/` - Build scripts, engineering infrastructure, and tooling
- `tests/helix/` - Helix test configuration and infrastructure
- `.github/workflows/` - GitHub Actions workflows
- `eng/pipelines/` - Azure DevOps pipeline definitions
- `Directory.Build.props`, `Directory.Build.targets` - MSBuild configuration
- `global.json` - .NET SDK and tooling versions
- `eng/common/` - Arcade SDK shared infrastructure
- `eng/testing/` - Test infrastructure and configuration
- `eng/Signing.props`, `eng/Publishing.props` - Release infrastructure

### Author Checklist

When making infrastructure-related changes, add this checklist to your PR description:

```markdown
## Infrastructure Changes Validation

- [ ] **AzDO Tests**: Verified that Azure DevOps tests have run successfully
  - [ ] All required pipeline runs completed without failures
  - [ ] Any pipeline configuration changes have been validated
- [ ] **Build Infrastructure**: Confirmed changes don't break build processes
  - [ ] Local build completed successfully: `./build.sh` (or `./build.cmd`)
  - [ ] Package generation works: `./build.sh --pack`
- [ ] **Test Infrastructure**: Verified test execution infrastructure works
  - [ ] Test discovery and execution not impacted
  - [ ] Helix test submission works (if applicable)
- [ ] **Dependency Changes**: Validated any tooling or dependency updates
  - [ ] Version updates are compatible and intentional
  - [ ] No unintended side effects on other components
```

### Reviewer Guidelines

- Verify that infrastructure changes are minimal and focused
- Check that AzDO pipeline runs are referenced and successful
- Ensure adequate testing of infrastructure modifications
- Validate that changes follow established patterns in `eng/` directory

## 2. Manual Workflows

**When this applies:** Changes impacting workflows or actions that do not run automatically in PRs, such as Outer Loop and Quarantine workflows.

### Relevant Files and Directories

- `.github/workflows/tests-outerloop.yml` - Outer Loop test workflow  
- `.github/workflows/tests-quarantine.yml` - Quarantine test workflow
- `.github/workflows/build-packages.yml` - Package building workflow
- `.github/workflows/refresh-manifests.yml` - Manifest refresh workflow
- `.github/workflows/update-dependencies.yml` - Dependency update workflow
- `eng/OuterloopTestRunsheetBuilder/` - Outer Loop test configuration
- `eng/QuarantinedTestRunsheetBuilder/` - Quarantine test configuration
- Any workflow with `workflow_dispatch` or `schedule` triggers

### Author Checklist

When making changes affecting manual workflows, add this checklist to your PR description:

```markdown
## Manual Workflow Validation

- [ ] **Outer Loop Tests**: Manually executed outer loop workflows
  - [ ] Workflow run link: [Provide workflow run URL]
  - [ ] Results: ✅ Pass / ❌ Fail (explain if failed)
- [ ] **Quarantine Tests**: Verified quarantine test workflow (if applicable)
  - [ ] Workflow run link: [Provide workflow run URL] 
  - [ ] Results: ✅ Pass / ❌ Fail (explain if failed)
- [ ] **Other Manual Workflows**: Executed relevant manual workflows
  - [ ] Workflow: [Name] - Link: [URL] - Result: ✅/❌
  - [ ] Workflow: [Name] - Link: [URL] - Result: ✅/❌
- [ ] **Schedule Impact**: Considered impact on scheduled workflow runs
  - [ ] No breaking changes to scheduled workflows
  - [ ] Schedule timing remains appropriate
```

### Workflow Execution Instructions

To manually run workflows:

1. Navigate to the **Actions** tab in the repository
2. Select the relevant workflow (e.g., "Outer Loop Tests", "Quarantine Tests")
3. Click **"Run workflow"** button
4. Select the branch containing your changes
5. Provide any required parameters
6. Click **"Run workflow"** to execute

### Reviewer Guidelines

- Verify manual workflow execution results are provided and successful
- Check that workflow changes don't impact scheduled execution negatively
- Ensure appropriate testing coverage for workflow modifications

## 3. CLI, Signing, and Publishing Changes

**When this applies:** Changes to CLI builds, extensions, signing processes, or publishing pipelines.

### Relevant Files and Directories

- `src/Aspire.Cli/` - CLI source code and configuration
- `eng/clipack/` - CLI native packaging projects
- `eng/dashboardpack/` - Dashboard packaging
- `eng/dcppack/` - DCP (DevEx Control Plane) packaging
- `eng/Signing.props` - Code signing configuration
- `eng/Publishing.props` - Publishing and release configuration
- `.github/workflows/build-cli-native-archives.yml` - CLI build workflow
- `extension/` - VS Code extension source
- Any files affecting NuGet package publishing or signing

### Author Checklist

When making CLI, signing, or publishing changes, add this checklist to your PR description:

```markdown
## CLI/Signing/Publishing Validation

- [ ] **Internal Pipeline**: Manually ran internal pipeline for validation
  - [ ] Pipeline run link: [Provide internal pipeline URL]
  - [ ] Results: ✅ Pass / ❌ Fail (explain if failed)
- [ ] **CLI Build**: Verified CLI builds successfully
  - [ ] Native AOT compilation works: `./build.sh` (without `/p:SkipNativeBuild=true`)
  - [ ] CLI functionality tested: `aspire --help`, basic commands work
- [ ] **Extension Build**: Validated extension builds (if applicable)
  - [ ] Extension packaging successful
  - [ ] No breaking changes to extension functionality
- [ ] **Signing Process**: Confirmed signing configuration changes are valid
  - [ ] No accidental changes to signing requirements
  - [ ] Test signing works in development environment
- [ ] **Publishing Impact**: Assessed impact on release/publishing process
  - [ ] Package metadata is correct
  - [ ] No breaking changes to publishing workflow
```

### Internal Pipeline Access

Internal pipeline execution requires appropriate permissions. Contact the engineering systems team (`@aspire/area-engineering-systems`) if you need access or assistance running internal pipelines.

### Reviewer Guidelines

- Verify internal pipeline execution results are provided
- Check CLI functionality hasn't been compromised
- Validate signing and publishing changes are intentional and safe
- Ensure adequate testing of packaging and distribution mechanisms

## 4. File Tracking and Maintenance

### Updating File Lists

As the repository evolves, the file and directory lists in these guidelines should be updated. When adding new infrastructure, workflows, or build processes:

1. **Update Relevant File Lists**: Add new files/directories to appropriate sections
2. **Test Guidelines**: Ensure new files trigger appropriate validation processes
3. **Update Documentation**: Modify this document to reflect new patterns or processes

### Tracking Changes

Contributors and reviewers should watch for:

- New files in `eng/` directory that affect build or test infrastructure
- New GitHub Actions workflows, especially those with manual triggers
- Changes to packaging, signing, or publishing processes
- Updates to core infrastructure files (`global.json`, `Directory.Build.*`)

## 5. General Review Guidelines

### For Authors

- **Use Appropriate Checklist**: Include the relevant checklist from this document in your PR description
- **Provide Evidence**: Link to workflow runs, pipeline results, and test outcomes  
- **Be Thorough**: Don't skip validation steps, even if changes seem minor
- **Ask for Help**: Contact area owners if you need assistance with validation processes

### For Reviewers

- **Verify Checklists**: Ensure authors have completed appropriate validation checklists
- **Check Links**: Validate provided links to workflow runs and pipeline results
- **Area Expertise**: Leverage area owners and experts for specialized reviews
- **Ask Questions**: Request clarification or additional validation if needed

### Area Owner Contact

Reference the [area-owners.md](area-owners.md) document for specific area experts:

- **Engineering Systems**: `@aspire/area-engineering-systems` - Infrastructure, build, and test systems
- **CLI**: Contact via appropriate area labels for CLI-related changes
- **Dashboard**: Contact dashboard area owners for dashboard packaging changes

## 6. Periodic Updates

### Review Schedule

This document should be reviewed and updated periodically to ensure accuracy and effectiveness:

- **Quarterly Reviews**: Check file lists and processes for accuracy
- **After Major Changes**: Update guidelines after significant infrastructure changes
- **Community Feedback**: Incorporate feedback from contributors and reviewers

### Update Process

1. **Review Current State**: Audit existing files and processes against documented guidelines
2. **Identify Gaps**: Look for new patterns or processes not covered
3. **Update Documentation**: Revise guidelines to reflect current practices
4. **Validate Changes**: Test updated guidelines with real scenarios
5. **Communicate Updates**: Announce significant changes to contributors

### Feedback

If you notice outdated information or missing scenarios in these guidelines:

1. **Create an Issue**: File an issue with the `area-docs` label
2. **Suggest Updates**: Provide specific suggestions for improvements
3. **Share Experience**: Describe challenges or gaps encountered during reviews

## Conclusion

These guidelines serve as a reference to make the Copilot-assisted review process more consistent, thorough, and effective. By following these structured approaches, we can ensure that critical validation steps are not overlooked and that changes to the `dotnet/aspire` repository maintain high quality and reliability.

For questions or suggestions about these guidelines, please refer to the [area-owners.md](area-owners.md) document for appropriate contacts.