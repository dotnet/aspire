# Git Bisect Helper for WithHttpCommand Test Investigation

This document provides instructions for using the git bisect helper scripts to investigate when the `WithHttpCommand_ResultsInExpectedResultForHttpMethod` test started failing repeatedly.

## Overview

The helper scripts automate the git bisect process to find the specific commit that introduced repeated failures for the `WithHttpCommand_ResultsInExpectedResultForHttpMethod` test in `Aspire.Hosting.Tests`. The scripts run the test multiple times at each commit to catch intermittent failures.

## Scripts Location

- **Unix/macOS/Linux**: `eng/bisect/withhttpcommand-bisect.sh`
- **Windows**: `eng/bisect/withhttpcommand-bisect.cmd`

## Usage

### Basic Syntax

```bash
# Unix/macOS/Linux
./eng/bisect/withhttpcommand-bisect.sh <good-commit> [bad-commit]

# Windows
eng\bisect\withhttpcommand-bisect.cmd <good-commit> [bad-commit]
```

### Parameters

- `<good-commit>`: A known good commit hash where the test was passing consistently
- `[bad-commit]`: A known bad commit hash where the test fails (defaults to HEAD if not specified)

### Examples

```bash
# Bisect between a specific good commit and HEAD
./eng/bisect/withhttpcommand-bisect.sh abc123def

# Bisect between two specific commits
./eng/bisect/withhttpcommand-bisect.sh abc123def def456ghi

# Bisect between a commit and main branch
./eng/bisect/withhttpcommand-bisect.sh abc123def main
```

## How It Works

1. **Validation**: The script validates that both commits exist and the repository is in a clean state
2. **Git Bisect Setup**: Starts `git bisect` with the provided good and bad commits
3. **Automated Testing**: For each commit tested:
   - Builds the project using the appropriate build script (`build.sh` or `build.cmd`)
   - Runs the `WithHttpCommand_ResultsInExpectedResultForHttpMethod` test **10 times**
   - Marks the commit as:
     - **Good**: If all 10 iterations pass
     - **Bad**: If any iteration fails
     - **Skip**: If the build fails (commit will be skipped)
4. **Result**: Reports the first bad commit that introduced the repeated failures
5. **Cleanup**: Automatically resets the repository state and saves a bisect log

## Test Details

- **Test Project**: `tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj`
- **Test Filter**: `WithHttpCommand_ResultsInExpectedResultForHttpMethod`
- **Iterations**: 10 runs per commit (configurable in the script)
- **Build Configuration**: Debug

## Output

The script provides detailed logging with timestamps and saves a bisect log file in the repository root with the format:
```
bisect-withhttpcommand-YYYYMMDD-HHMMSS.log
```

## Prerequisites

- Git repository in a clean state (no uncommitted changes)
- Both good and bad commits must exist in the repository
- Ability to build the project (all build dependencies available)

## Troubleshooting

### Common Issues

1. **Repository has uncommitted changes**
   - Solution: Commit, stash, or discard your changes before running the script

2. **Build failures on certain commits**
   - The script automatically skips commits that fail to build
   - This is normal for bisecting across major refactoring periods

3. **Test timeouts or infrastructure failures**
   - The script may need adjustment of timeout values for slower CI environments
   - Consider running on a dedicated build machine for consistency

### Customization

You can modify the following variables in the scripts:

- `ITERATIONS`: Number of test runs per commit (default: 10)
- `TEST_FILTER`: Test name pattern to match
- Build configuration and other parameters

## Example Output

```
[2024-01-15 10:30:15] Starting git bisect for WithHttpCommand_ResultsInExpectedResultForHttpMethod test
[2024-01-15 10:30:15] Good commit: abc123def
[2024-01-15 10:30:15] Bad commit: HEAD
[2024-01-15 10:30:15] Test iterations per commit: 10
[2024-01-15 10:30:16] Starting git bisect...
[2024-01-15 10:30:16] Running automated bisect...
[2024-01-15 10:30:17] Testing commit: def456g
[2024-01-15 10:30:17] Building project...
[2024-01-15 10:30:45] Build successful
[2024-01-15 10:30:45] Running test 10 times...
[2024-01-15 10:30:46] Iteration 1/10
[2024-01-15 10:30:48] Iteration 2/10
...
[2024-01-15 10:31:15] Test failed on iteration 7
[2024-01-15 10:31:15] This commit is BAD
...
[2024-01-15 10:45:30] Bisect completed!
[2024-01-15 10:45:30] The problematic commit is:
[2024-01-15 10:45:30] ghi789jkl Fix HTTP command processing
[2024-01-15 10:45:30] Bisect log saved to: bisect-withhttpcommand-20240115-104530.log
[2024-01-15 10:45:30] Repository state restored
```

## Notes

- The bisect process may take significant time depending on the commit range and build times
- Results are most reliable when run in a consistent environment (same machine, same dependencies)
- The script automatically handles cleanup even if interrupted (Ctrl+C)
- For Windows users, ensure you have the Windows build tools properly configured