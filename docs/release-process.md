# Aspire Release Process

This document describes the release process for dotnet/aspire, including both the automated workflows and manual steps required by the release manager.

## Overview

The Aspire release process involves two main automation components:

1. **Azure DevOps Pipeline** (`eng/pipelines/release-publish-nuget.yml`)
   - Publishes NuGet packages to NuGet.org
   - Promotes the build to the GA channel via darc

2. **GitHub Actions Workflow** (`.github/workflows/release-github-tasks.yml`)
   - Creates Git tags
   - Creates GitHub Releases
   - Creates merge-back PRs
   - Creates baseline version update PRs

## Prerequisites

Before starting a release:

1. **Signed Build**: Have a successful signed build from the official `dotnet-aspire` pipeline
   - The build will be selected from a dropdown when running the release pipeline
   - The build should have a `BAR ID - NNNNNN` tag (auto-extracted by the pipeline)

2. **Version Number**: Know the release version (e.g., `13.2.0` or `13.2.0-preview.1`)

3. **Release Branch**: Ensure the release branch exists (e.g., `release/9.2`)

4. **Permissions**:
   - Access to run Azure DevOps pipelines with the publishing pool
   - GitHub write access for creating tags/releases/PRs

## Step-by-Step Release Process

### Step 1: Publish NuGet Packages (Azure DevOps)

1. Navigate to the Azure DevOps pipeline: `release-publish-nuget`
2. Click "Run pipeline"
3. Under **Resources**, select the source build from the `aspire-build` dropdown
   - This shows recent builds from the `dotnet-aspire` pipeline
   - Select the specific signed build you want to release
4. Fill in the parameters:

   | Parameter | Description | Example |
   |-----------|-------------|---------|
   | `ReleaseVersion` | The version being released | `13.2.0` |
   | `GaChannelName` | Target GA channel | `Aspire 9.x GA` |
   | `DryRun` | Set `true` to test without publishing | `false` |
   | `SkipNuGetPublish` | Set `true` if re-running after NuGet success | `false` |
   | `SkipChannelPromotion` | Set `true` if re-running after darc success | `false` |

5. Click "Run" and monitor the pipeline
6. Verify packages appear on NuGet.org

### Step 2: GitHub Tasks (GitHub Actions)

1. Navigate to Actions → "Release GitHub Tasks"
2. Click "Run workflow"
3. Fill in the parameters:

   | Parameter | Description | Example |
   |-----------|-------------|---------|
   | `release_version` | The version being released | `13.2.0` |
   | `commit_sha` | Full 40-char commit SHA from the build | `abc123...` |
   | `release_branch` | Release branch name | `release/9.2` |
   | `is_prerelease` | `true` for preview releases | `false` |
   | `dry_run` | `true` to validate without making changes | `false` |
   | `skip_tagging` | Skip if tag already created | `false` |
   | `skip_github_release` | Skip if release already exists | `false` |
   | `skip_merge_pr` | Skip if PR already created | `false` |
   | `skip_baseline_pr` | Skip if PR already created | `false` |

4. Click "Run workflow" and monitor progress

> **Tip**: Use `dry_run: true` to test the workflow without creating any tags, releases, or PRs. This is useful for validating inputs and checking what actions would be taken.

### Step 3: Post-Release Tasks (Manual)

After automation completes:

1. **Review and merge PRs**:
   - Merge-back PR: `$RELEASE_BRANCH` → `main`
   - Baseline version PR: Updates `PackageValidationBaselineVersion`

2. **Verify the release**:
   - Check the [GitHub Releases page](https://github.com/dotnet/aspire/releases)
   - Verify packages on [NuGet.org](https://www.nuget.org/packages?q=owner%3Adotnet+aspire)
   - Test installation: `dotnet new install Aspire.ProjectTemplates::VERSION`

3. **Communicate**:
   - Update any tracking issues
   - Notify stakeholders

## Handling Failures

Both automations are designed to be **idempotent** and safe to re-run.

### Azure DevOps Pipeline Failures

| Stage Failed | Resolution |
|--------------|------------|
| Validate | Fix the input parameters and re-run |
| ExtractBarBuildId | Check that the build has a `BAR ID - NNNNNN` tag |
| PublishNuGet | Check NuGet.org for partial success, then re-run (uses `--skip-duplicate`) |
| PromoteToChannel | Re-run with `SkipNuGetPublish: true` |

### GitHub Actions Failures

| Job Failed | Resolution |
|------------|------------|
| validate | Fix the input parameters and re-run |
| create-tag | If tag exists with wrong SHA, requires manual resolution |
| create-release | Re-run with `skip_tagging: true` |
| create-merge-pr | Re-run with `skip_tagging: true`, `skip_github_release: true` |
| create-baseline-pr | Re-run with all prior skips set to `true` |

## Configuration

### 1ES Pipeline Compliance

The AzDO pipeline extends the 1ES Pipeline Templates (`v1/1ES.Official.PipelineTemplate.yml`) to be compliant with Microsoft organization requirements. This provides:
- SDL (Security Development Lifecycle) compliance scanning
- Proper pool configuration for internal pipelines
- Component governance integration

### Variable Groups (Azure DevOps)

The pipeline requires the `Aspire-Release-Secrets` variable group containing:

| Variable | Description |
|----------|-------------|
| `NuGetApiKey` | API key for publishing to NuGet.org |

### Service Connections (Azure DevOps)

- `Darc: Maestro Production` - Used for darc channel promotion

### Approved GitHub Actions

The workflow uses only pre-approved actions:

- `actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683` (v4)
- `dotnet/actions-create-pull-request@e8d799aa1f8b17f324f9513832811b0a62f1e0b1` (v1)

## Troubleshooting

### "Could not find BAR ID tag"

The pipeline expects the build to have a tag in format `BAR ID - NNNNNN`. This is normally added automatically by the Maestro publishing process. If missing:

1. Check if the build completed its post-build steps
2. Manually look up the BAR ID in Maestro and add the tag
3. Contact the engineering team if the issue persists

### "Tag already exists but points to different commit"

This indicates a mismatch between the expected release commit and an existing tag. Resolution:

1. Verify you're using the correct commit SHA
2. If the existing tag is wrong, it must be manually deleted (requires admin)
3. If the SHA is wrong, correct it and re-run

### NuGet publish rate limiting

The pipeline implements exponential backoff retry (3 attempts). If you still hit rate limits:

1. Wait 5-10 minutes
2. Re-run with `SkipNuGetPublish: false` (it will skip already-published packages)

### PR creation fails

The workflow checks for existing PRs before creating. If a PR exists with a different title:

1. Close or merge the existing PR
2. Re-run the workflow

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         RELEASE PROCESS FLOW                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    Azure DevOps Pipeline                         │   │
│  │                release-publish-nuget.yml                         │   │
│  │                                                                  │   │
│  │  Resource: aspire-build (select from dropdown)                   │   │
│  │  Input: ReleaseVersion, GaChannelName                            │   │
│  │                                                                  │   │
│  │  ┌─────────────┐   ┌──────────────┐   ┌──────────────────────┐   │   │
│  │  │  Validate   │──▶│ Extract BAR  │──▶│  Publish to NuGet    │   │   │
│  │  │   Inputs    │   │   Build ID   │   │   (--skip-duplicate) │   │   │
│  │  └─────────────┘   └──────────────┘   └──────────────────────┘   │   │
│  │                                                    │             │   │
│  │                                                    ▼             │   │
│  │                                       ┌──────────────────────┐   │   │
│  │                                       │  Promote to Channel  │   │   │
│  │                                       │     (via darc)       │   │   │
│  │                                       └──────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                    │                                    │
│                                    ▼                                    │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    GitHub Actions Workflow                       │   │
│  │              .github/workflows/release-github-tasks.yml          │   │
│  │                                                                  │   │
│  │  Input: release_version, commit_sha, release_branch              │   │
│  │                                                                  │   │
│  │  ┌─────────────┐   ┌──────────────┐   ┌──────────────────────┐   │   │
│  │  │  Validate   │──▶│ Create Tag   │──▶│  Create GitHub       │   │   │
│  │  │   Inputs    │   │  v{version}  │   │    Release           │   │   │
│  │  └─────────────┘   └──────────────┘   └──────────────────────┘   │   │
│  │                                                    │             │   │
│  │                          ┌─────────────────────────┼─────────┐   │   │
│  │                          ▼                         ▼         │   │   │
│  │              ┌───────────────────────┐  ┌────────────────────┐   │   │
│  │              │   Create Merge PR     │  │ Create Baseline PR │   │   │
│  │              │ release/X.Y → main    │  │ Update version     │   │   │
│  │              └───────────────────────┘  └────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Related Documentation

- [Contributing Guide](contributing.md)
- [Quarantined Tests](quarantined-tests.md)
