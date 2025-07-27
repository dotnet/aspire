# Auto-Assign Milestone Workflow

This directory contains the GitHub Actions workflow for automatically assigning milestones to pull requests.

- **Workflow file:** [auto-assign-milestone.yml](./auto-assign-milestone.yml)
- **Documentation:** See [../docs/auto-assign-milestone.md](../../docs/auto-assign-milestone.md)

## Summary

- Assigns milestones to PRs targeting both `main` and `release/{version}` branches.
- Ensures PRs are always up to date, even if milestones or target branches change.
- See documentation for the detailed logic and customization.