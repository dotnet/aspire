# GitHub Issue Creation Request

## Status: REQUIRES MANUAL ACTION

This directory contains documentation for a follow-up GitHub issue that needs to be created manually.

### Background
This PR was created in response to a request to "open a new issue" for test coverage of the `--localhost-tld` template option mentioned in PR #12267.

### Limitation
The GitHub Copilot agent does not have permission to create GitHub issues directly. Therefore, this PR contains documentation that can be used to manually create the issue on GitHub.

### Files Created

1. **GITHUB_ISSUE_CONTENT.md** - Concise issue content ready to paste into GitHub's issue form
2. **FOLLOW_UP_ISSUE_LOCALHOST_TLD_TEST.md** - Comprehensive documentation with detailed test examples and context

### How to Create the Issue

#### Option 1: Using the Web UI
1. Go to https://github.com/dotnet/aspire/issues/new/choose
2. Select "💡 Feature request"
3. Copy content from `GITHUB_ISSUE_CONTENT.md`
4. Add labels: `area-templates`, `enhancement`, `testing`
5. Link to PR #12267
6. Submit the issue

#### Option 2: Using GitHub CLI
```bash
gh issue create \
  --title "Add test coverage for --localhost-tld template option" \
  --body-file GITHUB_ISSUE_CONTENT.md \
  --label "area-templates,enhancement,testing" \
  --repo dotnet/aspire
```

### After Issue Creation
1. Delete these documentation files from the repository:
   - `README_ISSUE_CREATION.md` (this file)
   - `GITHUB_ISSUE_CONTENT.md`
   - `FOLLOW_UP_ISSUE_LOCALHOST_TLD_TEST.md`
2. Reference the new issue number in PR #12267
3. Close this PR

### Related Links
- Original PR: https://github.com/dotnet/aspire/pull/12267
- Triggering Comment: https://github.com/dotnet/aspire/pull/12267#issuecomment-3438427114
