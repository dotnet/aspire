# Release Automation Plan for .NET Aspire

## Overview

Automate the release checklist tasks from the [New-Release-tictoc wiki](https://github.com/dotnet/aspire/wiki/New-Release-tictoc) by building on existing CI/CD infrastructure (GitHub Actions, Azure Pipelines, Arcade SDK).

## Current State

**Already Automated:**
- Package building and signing (Azure Pipelines)
- Multi-platform native CLI builds
- NuGet package publishing via Darc/Maestro
- Release notes analysis tools exist in `/tools/ReleaseNotes/`

**Not Automated (Target for this plan):**
- GitHub Release creation from tags
- Cross-repo updates (aspire-samples, eshop, dotnet-docker)
- VS Code extension publishing
- Package validation version bumping
- aka.ms link updates
- Release coordination/orchestration

---

## Phase 1: GitHub Release Automation

**Goal:** Automatically create GitHub releases when a version tag is pushed.

**Implementation:**
1. Create `.github/workflows/release.yml` workflow
2. Trigger on tag push matching `v*` pattern
3. Use `gh release create` to generate release with:
   - Auto-generated release notes from commits
   - Links to NuGet packages
   - CLI archive attachments from build artifacts

**Files to create/modify:**
- `.github/workflows/release.yml` (new)

---

## Phase 2: Cross-Repository Update Automation

**Goal:** Automatically create PRs to update dependent repositories when a new version is released.

### 2a. aspire-samples Update Bot

**Implementation:**
1. Create workflow that triggers on successful release
2. Use GitHub Actions to:
   - Clone aspire-samples repo
   - Update package references to new version
   - Create PR via `gh pr create`

### 2b. dotnet-docker Dashboard Image Update

**Implementation:**
1. Create workflow to update dashboard image reference
2. Trigger after dashboard artifacts are published
3. Create PR to dotnet/dotnet-docker with new image tag

**Files to create:**
- `.github/workflows/update-samples.yml` (new)
- `.github/workflows/update-docker.yml` (new)
- `eng/scripts/update-sample-versions.ps1` (new)

---

## Phase 3: VS Code Extension Publishing

**Goal:** Automate VS Code extension publishing to marketplace.

**Implementation:**
1. Add publishing stage to existing Azure Pipeline
2. Store `VSCE_PAT` token in Azure DevOps variable group
3. Run `vsce publish` after successful build on release tags
4. Gate publishing on release/* or internal/release/* branches

**Files to modify:**
- `eng/pipelines/azure-pipelines.yml` (add publish stage)

---

## Phase 4: Version Bump Automation

**Goal:** Automate package validation baseline version updates after releases.

**Implementation:**
1. Create script to update `PackageValidationBaselineVersion` in `eng/Versions.props`
2. Create workflow that runs after release, opens PR to main branch

**Files to create:**
- `eng/scripts/bump-baseline-version.ps1` (new)
- `.github/workflows/post-release-version-bump.yml` (new)

---

## Phase 5: Release Orchestration Workflow

**Goal:** Create a single "release coordinator" workflow that orchestrates all release tasks.

**Implementation:**
1. Create `workflow_dispatch` triggered workflow
2. Accept version number as input
3. Orchestrate:
   - Tag creation
   - Wait for builds to complete
   - Trigger cross-repo updates
   - Generate release checklist issue with manual task links

**Files to create:**
- `.github/workflows/release-coordinator.yml` (new)
- `.github/ISSUE_TEMPLATE/release-checklist.md` (new)

---

## Implementation Order

All phases will be implemented sequentially:

| Step | Phase | Description |
|------|-------|-------------|
| 1 | Phase 1 | GitHub Release automation |
| 2 | Phase 4 | Version bump automation |
| 3 | Phase 3 | VS Code extension publishing (Azure Pipelines) |
| 4 | Phase 2a | aspire-samples cross-repo updates |
| 5 | Phase 2b | dotnet-docker cross-repo updates |
| 6 | Phase 5 | Release orchestration workflow |

**Decisions:**
- Cross-repo workflows will live in this repo (dotnet/aspire)
- VS Code extension publishing will be added to Azure Pipelines

---

## Files Summary

**New files to create:**
```
.github/workflows/release.yml                    # Phase 1
.github/workflows/post-release-version-bump.yml  # Phase 4
.github/workflows/update-samples.yml             # Phase 2a
.github/workflows/update-docker.yml              # Phase 2b
.github/workflows/release-coordinator.yml        # Phase 5
.github/ISSUE_TEMPLATE/release-checklist.md      # Phase 5
eng/scripts/update-sample-versions.ps1           # Phase 2a
eng/scripts/bump-baseline-version.ps1            # Phase 4
```

**Existing files to modify:**
```
eng/pipelines/azure-pipelines.yml                # Phase 3 (VS Code extension publishing)
```

---

## Out of Scope (Requires Human)

These tasks cannot be automated and remain manual:
- Blog post writing and coordination
- Compliance sign-offs
- Validation team communication
- Feature coordination with area owners
