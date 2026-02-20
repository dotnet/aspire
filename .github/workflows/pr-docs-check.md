---
description: |
  Analyzes merged pull requests for documentation needs. When a PR is merged
  against main or release/* branches, this workflow reviews the changes and
  determines if documentation updates are required on microsoft/aspire.dev.
  If so, it creates a tracking issue and comments on the PR with a link.

on:
  pull_request:
    types: [closed]
    branches:
      - main
      - release/*
  workflow_dispatch:
    inputs:
      pr_number:
        description: "PR number to analyze"
        required: true
        type: string

if: >-
  (github.event.pull_request.merged == true || github.event_name == 'workflow_dispatch')
  && github.repository_owner == 'dotnet'

permissions:
  contents: read
  pull-requests: read
  issues: read

network:
  allowed:
    - defaults
    - github

tools:
  github:
    toolsets: [repos, issues, pull_requests]
    app:
      app-id: ${{ vars.DOCS_APP_ID }}
      private-key: ${{ secrets.DOCS_APP_PRIVATE_KEY }}
      owner: "microsoft"
      repositories: ["aspire.dev"]
  web-fetch:

safe-outputs:
  create-issue:
    title-prefix: "[docs] "
    labels: [docs-from-code]
    target-repo: "microsoft/aspire.dev"
    expires: false
  assign-milestone:
    target-repo: "microsoft/aspire.dev"
  add-comment:
    hide-older-comments: true

timeout-minutes: 15
---

# PR Documentation Check

Analyze a merged pull request in `dotnet/aspire` and determine whether documentation
updates are needed on the `microsoft/aspire.dev` documentation site.

## Context

- **Repository**: `${{ github.repository }}`
- **PR Number**: `${{ github.event.pull_request.number || github.event.inputs.pr_number }}`
- **PR Title**: `${{ github.event.pull_request.title }}`

## Step 1: Gather PR Information

Use the GitHub tools to read the full pull request details for the PR number above,
including the title, description, author, base branch, and the full diff of changes.
Pay special attention to the **base branch** (e.g., `main` or `release/X.Y`) and the
**PR author** username, as both are needed in later steps.

If this was triggered via `workflow_dispatch`, use the `pr_number` input to look up
the PR details.

## Step 2: Analyze Changes for Documentation Needs

Review the PR diff and metadata for the following types of user-facing changes that
warrant documentation:

- **New public APIs**: new methods, classes, interfaces, or extension methods
- **New features or capabilities**: new hosting integrations, client integrations,
  CLI commands, or dashboard features
- **Breaking changes**: removed or renamed APIs, behavioral changes, migration needs
- **New configuration options**: new settings, environment variables, or parameters
- **New resource types**: new Aspire hosting resources or cloud service integrations
- **Significant behavioral changes**: changes to service discovery, health checks,
  telemetry, or deployment

Changes that do NOT typically need documentation:
- Internal refactoring with no public API surface changes
- Test-only changes
- Build/CI infrastructure changes
- Bug fixes that don't change documented behavior
- Dependency version bumps
- Code style or formatting changes

## Step 3: Check Existing Documentation

If you determine documentation may be needed, use the GitHub tools to browse the
`microsoft/aspire.dev` repository to:

- Identify existing documentation pages that cover the affected feature area
- Determine whether existing pages need updates or new pages should be created
- Understand the current documentation structure and naming conventions

## Step 4: Determine Milestone

Based on the PR's base branch, determine which milestone to assign to the docs issue
on `microsoft/aspire.dev`:

- If the base branch is **`main`**: list the open milestones on `microsoft/aspire.dev`
  and pick the one that appears to be the current active milestone (typically the
  highest version number that is not yet closed).
- If the base branch is **`release/X.Y`** (e.g., `release/9.2`): look for an open
  milestone on `microsoft/aspire.dev` that matches version `X.Y` or the next patch
  release. If no exact match exists, use the closest matching open milestone.

## Step 5: Take Action

### If documentation IS needed:

1. **Create an issue** on `microsoft/aspire.dev` with:
   - A clear title describing the documentation work needed
   - A structured body that includes:
     - Link to the source PR in `dotnet/aspire`
     - PR author mention
     - Whether to UPDATE existing pages or CREATE new pages
     - Specific file paths in the `aspire.dev` repo that need updating (if applicable)
     - Detailed description of what sections need to be created or updated
     - Key points that must be covered
     - Suggested code examples (with placeholders if needed)
     - Cross-references to related documentation pages

2. **Assign the milestone** determined in Step 4 to the created issue.

3. **Comment on the PR** in `dotnet/aspire` with:
   - A summary indicating documentation updates are needed
   - A link to the created docs issue
   - A brief description of what documentation changes are recommended

### If documentation is NOT needed:

1. **Comment on the PR** in `dotnet/aspire` with:
   - A brief message confirming no documentation updates are required
   - A short explanation of why (e.g., "internal refactoring only", "test changes only")
