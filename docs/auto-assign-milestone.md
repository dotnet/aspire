# Auto-Assign PR Milestone Workflow

This repository uses a GitHub Actions workflow to automatically assign milestones to pull requests based on their target branch. This improves consistency and makes it easy to find all PRs for a given milestone or release.

## How It Works

- **Release Branches (`release/{version}`):**  
  When a PR targets a release branch (e.g., `release/9.5`), the workflow assigns the corresponding milestone (e.g., `9.5`) to the PR.

- **Main Branch:**  
  When a PR targets `main`, the workflow:
  - Finds the highest closed milestone (e.g., `9.4`).
  - Assigns the lowest open milestone with a version greater than the highest closed (e.g., assigns `9.5` if `9.4` is closed and `9.5`, `9.6` are open).

- **Milestone Updates:**  
  On every PR event (`opened`, `synchronize`, `reopened`, `edited`), the workflow re-evaluates and updates the milestone if necessary. This means if milestones change or a PR is left open through a release, it will always be in the correct milestone.

## Branch and Milestone Naming

- Milestones should be named with the version number (e.g., `9.5`, `9.6`).
- Release branches must follow `release/{version}` (e.g., `release/9.5`). The version must match the milestone exactly for assignment.

## Implementation Details

- The workflow uses GitHub CLI (`gh`) and bash scripting.
- It fetches all open and closed milestones, sorts them, and applies the logic described above.
- If a PR is already in the correct milestone, no changes are made.
- If no matching milestone is found, no assignment occurs (a message is logged).

## Customization

- For custom milestone naming or branch patterns, update the parsing logic in the workflow.
- For questions or improvements, open an issue or pull request!

## File Location

- Workflow: `.github/workflows/auto-assign-milestone.yml`
- This documentation: `docs/auto-assign-milestone.md`