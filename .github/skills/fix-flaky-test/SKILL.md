---
name: fix-flaky-test
description: Reproduces and fixes flaky or quarantined tests. Tries local reproduction first (fast), then falls back to CI reproduce workflow (reproduce-flaky-tests.yml). Use this when asked to investigate, reproduce, debug, or fix a flaky test, a quarantined test, or an intermittently failing test.
---

You are a specialized agent for reproducing and fixing flaky tests in the dotnet/aspire repository. You try local reproduction first using `run-test-repeatedly.sh` (Linux/macOS) or `run-test-repeatedly.ps1` (Windows) for fast feedback, and fall back to the CI reproduce workflow (`reproduce-flaky-tests.yml`) when local reproduction fails or the current OS doesn't match the failing OS.

## ⛔ MANDATORY: Follow the investigate→reproduce→fix→verify cycle

**Do NOT skip ahead to writing a code fix.** Even if you think you already know the root cause, you MUST follow every step in order:

1. **Step 1** — Gather failure data from the issue and read the test code for understanding
2. **Step 1.5** — Analyze existing quarantine failure logs (may reveal root cause without reproduction)
3. **Step 2** — Try to reproduce locally using `run-test-repeatedly.sh`/`.ps1` (fast path) ← try this FIRST
4. **Step 3** — If local reproduction fails, reproduce on CI using `reproduce-flaky-tests.yml` (can be skipped — see Step 1.5)
5. **Step 4** — Analyze failure logs to confirm root cause
6. **Step 5** — Apply fix and verify (local verification first, then CI verification for final validation)
7. **Step 6** — Verify the fix did not introduce regressions in the CI workflow
8. **Step 7** — Clean up investigation branch and create final PR

Each step has a **checkpoint** at the end. Do not proceed to the next step until the checkpoint is satisfied. Skipping reproduction leads to incomplete or incorrect fixes that waste reviewer time.

## Two-Branch Workflow

This skill uses two branches to keep investigation artifacts separate from the final clean fix:

### Investigation Branch (draft PR)
- Created from the working branch (or `main`)
- Named: `<base-branch>-investigate` (e.g., `flaky-test0-investigate`)
- Contains: disabled `ci.yml`, configured `reproduce-flaky-tests.yml`, code fix
- Opened as a **draft PR** with prominent WIP marking
- Purpose: CI verification of the fix using the reproduce workflow without triggering full CI

### Fix Branch (final PR)
- The original working branch (e.g., `flaky-test0`)
- Contains: only the code fix (clean diff)
- `ci.yml` enabled, `reproduce-flaky-tests.yml` at defaults
- Created/updated after verification succeeds on the investigation branch

**Why two branches?** Pushing workflow changes (disable ci.yml, configure reproduce workflow) to the same branch as the fix would trigger unwanted CI runs and pollute the final PR diff. The investigation branch isolates this.

## Top-Level Tracking

Use SQL to track the overall investigation state. This keeps the main context clean and allows recovery if work is interrupted.

### Initialize tracking at the start of every investigation:

```sql
INSERT INTO todos (id, title, description, status) VALUES
  ('gather-data', 'Gather failure data', 'Read issue, find test code, determine failure rates per OS', 'pending'),
  ('analyze-existing', 'Analyze existing quarantine logs', 'Download logs from recent quarantine failures to understand the error', 'pending'),
  ('reproduce-local', 'Reproduce locally', 'Try local reproduction with run-test-repeatedly.sh/.ps1 (fast path)', 'pending'),
  ('reproduce-ci', 'Reproduce on CI', 'If local fails, configure and run reproduce-flaky-tests.yml to confirm the failure', 'pending'),
  ('analyze', 'Analyze failure logs', 'Download CI logs or review local logs, identify root cause', 'pending'),
  ('fix', 'Apply fix', 'Write the code fix based on root cause analysis', 'pending'),
  ('verify', 'Verify fix on CI', 'Re-run reproduce workflow to confirm fix works', 'pending'),
  ('cleanup', 'Clean up investigation', 'Close investigation PR, create clean fix PR', 'pending');

INSERT INTO todo_deps (todo_id, depends_on) VALUES
  ('analyze-existing', 'gather-data'),
  ('reproduce-local', 'analyze-existing'),
  ('reproduce-ci', 'reproduce-local'),
  ('analyze', 'reproduce-local'),
  ('fix', 'analyze'),
  ('verify', 'fix'),
  ('cleanup', 'verify');
```

### Store key parameters in session state:

```sql
CREATE TABLE IF NOT EXISTS session_state (key TEXT PRIMARY KEY, value TEXT);
INSERT OR REPLACE INTO session_state (key, value) VALUES
  ('test_method', '<FullyQualifiedMethodName>'),
  ('test_project', '<ProjectShortname>'),
  ('issue_url', '<GitHubIssueURL>'),
  ('failure_rate_linux', '<rate or unknown>'),
  ('failure_rate_windows', '<rate or unknown>'),
  ('failure_rate_macos', '<rate or unknown>'),
  ('max_failure_rate', '<highest rate across OSes>'),
  ('reproduce_attempt', '1'),
  ('fix_attempt', '1'),
  ('reproduce_run_id', ''),
  ('verify_run_id', ''),
  ('investigation_branch', ''),
  ('fix_branch', ''),
  ('user_interaction', 'false');
```

**Always update todo status as you work** — set to `in_progress` before starting, `done` when complete. Query `SELECT * FROM todos;` to check progress. Store CI run IDs and attempt counts in `session_state`.

### Tracking User Interaction

If at any point during the investigation you use the `ask_user` tool to get input from the user, immediately update the session state:

```sql
INSERT OR REPLACE INTO session_state (key, value) VALUES ('user_interaction', 'true');
```

This flag determines whether the final PR is labeled as `[automated]` (see Step 6.2).

### Investigation Notes

Keep investigation notes in the **session workspace** (not in the repo). This avoids commit noise from temporary artifacts:

```
~/.copilot/session-state/<session-id>/
├── plan.md                # Summary: test name, issue, root cause, fix, status
└── files/
    └── failure-logs/      # Downloaded CI failure logs (if any)
```

Use `plan.md` in the session workspace for running notes and observations. Only create files in the repo if the investigation needs to be resumed by another agent in a different session.

## Overview: The Investigate→Reproduce→Fix→Verify Cycle

The steps below are sequential and gated. Complete each step fully before moving to the next.

1. Gather failure data from the issue (OS-specific failure rates, error messages) and read the test code for understanding
2. Analyze existing quarantine failure logs — this often reveals the root cause without needing a separate reproduction
3. **Try to reproduce locally** using `run-test-repeatedly.sh` (Linux/macOS) or `run-test-repeatedly.ps1` (Windows) — this is the fast path (~minutes vs ~30 min for CI). Works when the current OS matches a failing OS.
4. If local reproduction fails (wrong OS, contention-sensitive, or low failure rate), **fall back to CI reproduction** using `reproduce-flaky-tests.yml`
5. Analyze failure logs to identify root cause
6. Apply a fix. Try local verification first with `run-test-repeatedly.sh`/`.ps1`, then **always validate on CI** as final verification.
7. Clean up: close investigation branch, create clean fix PR

**Prefer analyzing existing data first.** The quarantine CI runs every 6 hours and the tracking issue links to runs with failures. These logs are often sufficient to diagnose the root cause without a separate reproduction run.

## Step 1: Gather Failure Data

### Finding the Issue

The user may provide:
- A **test method name** (e.g., `DeployAsync_WithMultipleComputeEnvironments_Works`)
- A **GitHub issue URL** (e.g., `https://github.com/dotnet/aspire/issues/13287`)
- Both

**If you only have the test name**, find the tracking issue:

1. First check the test code for a `[QuarantinedTest]` attribute — it contains the issue URL:
   ```bash
   grep -rn "QuarantinedTest" tests/ --include="*.cs" | grep "TestMethodName"
   ```

2. If not found there, look up the test in the **quarantine tracking meta-issue** https://github.com/dotnet/aspire/issues/8813 — this issue tracks all quarantined tests with links to their individual issues:
   ```bash
   gh issue view 8813 --repo dotnet/aspire
   ```
   Search the output for the test name to find its linked issue.

3. If neither source has the issue, **proceed without historical failure data**. Use a default configuration (all 3 OSes, 5×5 iterations) since you don't know which OSes fail or the failure rate.

### From the Issue

Quarantined test issues contain tracking tables with per-OS failure rates over the last 100 runs. This data is critical:

- **Which OSes fail**: Target only those OSes to save runner time
- **Failure rate**: Determines how many iterations you need for reproduction
- **Error pattern**: Helps identify root cause before reproducing

```bash
# Read the issue to get failure data
gh issue view <issue-number> --repo dotnet/aspire
```

### From the Test Code

Find the test method, class, and project. **Read the test source code and its fixture/setup** to understand what the test does, how it waits for readiness, and what patterns it uses. This is essential for understanding what you're trying to reproduce and for matching against common flaky test patterns.

```bash
# Search for the test method
grep -rn "public.*async.*Task.*TestMethodName\|public.*void.*TestMethodName" tests/ --include="*.cs"
```

**Consult the flaky test patterns** in `.github/instructions/test-review-guidelines.instructions.md` early. If the test code matches a known pattern AND the error message from the issue matches the expected symptom, you have a strong hypothesis to validate during reproduction.

### Iteration Count Heuristic

Based on the failure rate from the issue tracking data, calculate iterations to achieve **95% probability of seeing at least one failure** (if the bug exists):

| Failure Rate | Runners × Iterations per OS | Total per OS | Confidence |
|---|---|---|---|
| >50% | 3 × 3 | 9 | >99% |
| 20-50% | 5 × 5 | 25 | >99% |
| 10-20% | 5 × 10 | 50 | >99% |
| 5-10% | 10 × 10 | 100 | >99% |
| <5% | 10 × 25 | 250 | >95% |

The math: for failure rate `p`, need `n ≥ log(0.05) / log(1-p)` iterations for 95% confidence. The table above provides comfortable margins.

### ✅ Step 1 Checkpoint

Before proceeding to Step 1.5, confirm you have:
- [ ] The test method name, class, and project path
- [ ] The issue URL (if available)
- [ ] Per-OS failure rates (to choose target OSes and iteration counts)
- [ ] The error message/pattern from the issue
- [ ] Read the test source code and its fixture/setup for understanding
- [ ] Checked the Common Flaky Test Patterns table for matches
- [ ] SQL tracking initialized with all parameters stored

**Do NOT write a fix yet.** You have a hypothesis, but proceed to Step 1.5 to validate it with existing failure data.

## Step 1.5: Analyze Existing Quarantine Failure Logs

Before running a separate reproduction, check if existing quarantine CI logs already contain the information you need. The quarantine workflow runs every 6 hours, and the tracking issue links to recent failures.

### Finding Failure Logs from Quarantine Runs

The tracking issue contains ❌ links to failed quarantine runs. Use those run IDs to find the specific job that failed:

```bash
# Find the failed job for your test project in a quarantine run
gh api "repos/dotnet/aspire/actions/runs/<run_id>/jobs?per_page=100&filter=latest" \
  --jq '.jobs[] | select(.name | contains("<ProjectShortname>")) | select(.conclusion == "failure") | {id: .id, name: .name}'
```

Then download the logs for that job:

```bash
# Get logs via the GitHub MCP tool (preferred — handles encoding automatically)
# Use get_job_logs with the job_id, return_content: true, tail_lines: 300

# Or via CLI
gh api "repos/dotnet/aspire/actions/jobs/<job_id>/logs" > quarantine-failure.log
```

Search the logs for the test name, error message, and stack trace:

```bash
grep -i "TestMethodName\|TaskCanceled\|Assert\|Exception\|FAIL" quarantine-failure.log | head -30
```

### Identifying Contention-Sensitive Tests

A test is likely **contention-sensitive** (fails only when running alongside other tests) if:

1. **It uses `randomizePorts: false`** — fixed ports can conflict with other concurrent tests
2. **It uses a shared fixture** (collection fixture or class fixture) — startup timing depends on other tests
3. **It uses `WaitForTextAsync`** — log-based readiness checks are fragile under contention
4. **It shares a `CancellationTokenSource` across startup and readiness phases** — one phase can starve the other's timeout budget
5. **The tracking issue shows 0% failure on macOS** (which often has less CI contention) but failures on Linux/Windows

If you identify the test as contention-sensitive, the reproduce workflow (which runs the test in isolation) is unlikely to reproduce the failure. In this case, you may **skip Step 2** and proceed directly to Step 3 (root cause analysis) using the quarantine logs as your evidence.

### ✅ Step 1.5 Checkpoint

Before deciding whether to skip reproduction:
- [ ] Downloaded and examined at least 1-2 quarantine failure logs for the test
- [ ] Confirmed the error matches the pattern in the tracking issue
- [ ] Assessed whether the test is contention-sensitive

**If contention-sensitive**: Mark `reproduce-local` and `reproduce-ci` as `done` in SQL, set `analyze` to `in_progress`, and proceed to Step 4 using the quarantine logs as your failure evidence.

**If NOT contention-sensitive** (or you're unsure): Proceed to Step 2 for local reproduction.

## Step 2: Try Local Reproduction (Fast Path)

Before going to CI, try reproducing the failure locally. This gives feedback in minutes instead of 30+ minutes.

### 2.1: Check OS Compatibility

```bash
uname -s  # Linux, Darwin (macOS), or Windows (via MSYS/Git Bash)
```

Compare your OS against the failing OSes from Step 1. Local reproduction is viable when:
- Your OS matches one of the failing OSes, OR
- The test fails on **all** OSes (OS-independent flakiness)

If the test only fails on an OS you don't have (e.g., fails only on Windows and you're on Linux), skip to Step 3 (CI reproduction).

### 2.2: Build the Test Project

```bash
# Restore first if not already done
./restore.sh  # or ./restore.cmd on Windows

# Build the specific test project
dotnet build tests/<TestProject>.Tests/<TestProject>.Tests.csproj -v:q
```

### 2.3: Run with run-test-repeatedly script

Use the `run-test-repeatedly.sh` (Linux/macOS) or `run-test-repeatedly.ps1` (Windows) script in `.github/skills/fix-flaky-test/`. It runs the test command repeatedly with process cleanup between iterations.

**Linux/macOS:**

```bash
# Basic usage — run a single test 20 times (stop on first failure)
./.github/skills/fix-flaky-test/run-test-repeatedly.sh -n 20 -- \
  dotnet test tests/<TestProject>.Tests/<TestProject>.Tests.csproj --no-build \
  -- --filter-method "*.<TestMethodName>" \
  --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

**Windows (PowerShell):**

```powershell
# Basic usage — run a single test 20 times (stop on first failure)
./.github/skills/fix-flaky-test/run-test-repeatedly.ps1 -n 20 -- dotnet test tests/<TestProject>.Tests/<TestProject>.Tests.csproj --no-build `
  -- --filter-method "*.<TestMethodName>" `
  --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

**For quarantined tests**, you need `/p:RunQuarantinedTests=true` during both build **and** test to prevent the build system from filtering them out:

```bash
dotnet build tests/<TestProject>.Tests/<TestProject>.Tests.csproj -v:q /p:RunQuarantinedTests=true

# Linux/macOS
./.github/skills/fix-flaky-test/run-test-repeatedly.sh -n 20 -- \
  dotnet test tests/<TestProject>.Tests/<TestProject>.Tests.csproj --no-build \
  /p:RunQuarantinedTests=true \
  -- --filter-method "*.<TestMethodName>"

# Windows (PowerShell)
./.github/skills/fix-flaky-test/run-test-repeatedly.ps1 -n 20 -- dotnet test tests/<TestProject>.Tests/<TestProject>.Tests.csproj --no-build `
  -- --filter-method "*.<TestMethodName>"
```

**Choose iteration count** based on failure rate (same heuristic as CI):

| Failure Rate | Local Iterations | Expected failures |
|---|---|---|
| >50% | 10 | ~5+ |
| 20-50% | 20 | ~4-10 |
| 10-20% | 30 | ~3-6 |
| 5-10% | 50 | ~2-5 |
| <5% | 100 | ~1-5 |

**Script options** (same for both `.sh` and `.ps1`):
- `-n <count>` — Number of iterations (default: 100)
- `--run-all` — Don't stop on first failure, run all iterations
- `--help` — Show usage

Results are saved to `/tmp/test-results-<timestamp>/` (Linux/macOS) or `$env:TEMP\test-results-<timestamp>\` (Windows). Failure logs are in `failure-*.log` files.

### 2.4: Handle Local Reproduction Results

**If the test fails locally**: Reproduction successful ✅. Examine the failure log:

```bash
# The script prints the results directory path
cat /tmp/test-results-*/failure-*.log
```

Mark `reproduce-local` as `done` in SQL and proceed to Step 4 (root cause analysis) using the local failure logs.

```sql
UPDATE todos SET status = 'done' WHERE id = 'reproduce-local';
UPDATE todos SET status = 'done' WHERE id = 'reproduce-ci';  -- skip CI reproduction
UPDATE todos SET status = 'in_progress' WHERE id = 'analyze';
```

**If the test passes all local iterations**: Local reproduction failed. This can happen because:
- The failure is OS-specific and you're on the wrong OS
- The failure is contention-sensitive (only happens with parallel tests)
- The failure rate is very low and you didn't run enough iterations

Proceed to Step 3 (CI reproduction) for cross-OS, parallel-runner reproduction.

```sql
UPDATE todos SET status = 'done' WHERE id = 'reproduce-local';
INSERT OR REPLACE INTO session_state (key, value) VALUES ('local_result', 'no_failures');
```

### ✅ Step 2 Checkpoint

- [ ] Checked OS compatibility
- [ ] Ran `run-test-repeatedly.sh`/`.ps1` with appropriate iteration count (or skipped due to OS mismatch)
- [ ] Recorded result: local failure found → proceed to Step 4, or no failures → proceed to Step 3

## Step 3: Reproduce on CI (Fallback)

### 3.0: Create the Investigation Branch

Create a separate branch for CI investigation. This branch will have `ci.yml` disabled and `reproduce-flaky-tests.yml` configured, keeping the fix branch clean.

```bash
# Create investigation branch from the current working branch
git checkout -b <fix-branch>-investigate

# Store the branch names
```

```sql
INSERT OR REPLACE INTO session_state (key, value) VALUES
  ('investigation_branch', '<fix-branch>-investigate'),
  ('fix_branch', '<fix-branch>');
```

### 3.0a: Disable ci.yml

Disable `ci.yml` so pushing to the investigation branch doesn't trigger full CI:

```yaml
# .github/workflows/ci.yml — add this at the top level, after `name:`
# Change the `on:` trigger to disable automatic runs:
on:
  workflow_dispatch: {}  # Only manual trigger, no automatic PR/push triggers
```

This prevents CI from running on every push to the investigation branch. You will re-enable it when creating the final fix PR.

### 3.1: Configure the Reproduce Workflow

Edit `.github/workflows/reproduce-flaky-tests.yml` — change only the `env:` section at the top:

```yaml
env:
  TEST_PROJECT: "Hosting.Azure"  # Project shortname
  TEST_FILTER: '--filter-method "*.DeployAsync_WithMultipleComputeEnvironments_Works"'
  TARGET_OSES: "windows-latest"  # Focus on highest-failure-rate OS
  RUNNERS_PER_OS: "5"
  ITERATIONS_PER_RUNNER: "5"
```

**OS targeting strategy:**
- **High failure rate (>20%) on one OS**: Target that OS only first — fastest feedback
- **High rate on multiple OSes**: Target all failing OSes
- **Low rate or can't reproduce**: Focus on the OS with the highest failure rate, increase iterations
- **Unknown rates**: Target `ubuntu-latest,windows-latest` with moderate iterations

**Test project shortname mapping**: The workflow resolves `TEST_PROJECT` to a path:
- Tries `tests/{name}.Tests/{name}.Tests.csproj` first
- Then `tests/Aspire.{name}.Tests/Aspire.{name}.Tests.csproj`
- Examples: `Hosting` → `Aspire.Hosting.Tests`, `Hosting.Azure` → `Aspire.Hosting.Azure.Tests`

**Common filter patterns**:
```yaml
# Single test method
TEST_FILTER: '--filter-method "*.TestMethodName"'
# All tests in a class
TEST_FILTER: '--filter-class "*.TestClassName"'
# Multiple test methods
TEST_FILTER: '--filter-method "*.Test1" --filter-method "*.Test2"'
```

**For quarantined tests**: The workflow automatically disables the quarantine exclusion filter for both build and test phases (via `/p:_NonQuarantinedTestRunAdditionalArgs=""`), so quarantined tests are included regardless of their trait. You do NOT need to add any special flags.

**Zero-test detection**: The workflow detects when zero tests execute (e.g., due to a misconfigured filter) and treats it as a failure. If you see "Zero tests executed" errors, verify that `TEST_FILTER` matches the actual test name and that quarantine settings are correct.

### 3.2: Push and Open Draft PR

Commit the workflow changes and open a **draft PR** with the investigation template:

```bash
git add .github/workflows/ci.yml .github/workflows/reproduce-flaky-tests.yml
git commit -m "🔍 Investigation: configure CI for flaky test reproduction

⚠️ DO NOT MERGE — This is a temporary investigation branch.
ci.yml disabled, reproduce workflow configured for <test name>."
git push --set-upstream origin <fix-branch>-investigate
```

Open a draft PR with prominent WIP marking:

```bash
gh pr create --draft --repo dotnet/aspire \
  --title "🔍 [DO NOT MERGE] Investigation: <test name>" \
  --body "## ⚠️ DO NOT MERGE — Investigation Branch

This is a temporary branch for reproducing and verifying a fix for a flaky test.

**Issue**: #<issue-number>
**Test**: \`<FullyQualifiedTestName>\`

### What's changed on this branch
- \`ci.yml\` disabled (prevents full CI on investigation pushes)
- \`reproduce-flaky-tests.yml\` configured for the target test
- Code fix (will be applied after reproduction)

### Status
- [ ] Reproduction confirmed
- [ ] Fix applied
- [ ] Fix verified on CI
- [ ] Clean fix PR created

This branch will be deleted after the fix is verified and a clean PR is created."
```

### 3.3: Trigger the Reproduce Workflow

```bash
gh workflow run reproduce-flaky-tests.yml --repo dotnet/aspire --ref <fix-branch>-investigate
```

This dispatches the workflow from `main` but runs the version from your branch, so your env var edits will be used.

**If the workflow dispatch fails** (e.g. HTTP 403 "Resource not accessible by integration"): your GitHub token lacks `actions:write` permission on the repository. This is a non-fatal blocker — continue with the investigation, but you **must** document this in every PR you open (both investigation and fix PRs). Include the exact error, and provide the manual trigger command so a reviewer or maintainer can run it. See the PR template in Step 6.2 for the required format.

### 3.4: Monitor and Cancel

**Monitor the run using polling** (CI runs take 10-30+ minutes):

```bash
# Find the run ID
gh run list --repo dotnet/aspire --branch <branch> --limit 1 --json databaseId,status
```

Store the run ID, then poll periodically for completion:
```sql
INSERT OR REPLACE INTO session_state (key, value) VALUES ('reproduce_run_id', '<run-id>');
```

```bash
# Poll for completion (use bash mode="async", then read_bash with increasing delays)
# Avoid `gh run watch` — it produces excessive output that floods the context window.
gh run view <run-id> --repo dotnet/aspire --json status,conclusion --jq '{status, conclusion}'

# Check individual job results as they complete
gh run view <run-id> --repo dotnet/aspire --json jobs \
  --jq '.jobs[] | select(.status == "completed") | {name: .name, conclusion: .conclusion}'
```

**Cancel old runs** when starting new ones to avoid wasting CI resources:

```bash
# Cancel a specific run
gh run cancel <run-id> --repo dotnet/aspire

# Cancel all in-progress runs on your branch (useful when iterating)
gh run list --repo dotnet/aspire --branch <branch> --status in_progress --json databaseId --jq '.[].databaseId' | \
  xargs -I {} gh run cancel {} --repo dotnet/aspire
```

Always cancel previous reproduce/verify runs before pushing a new configuration. `workflow_dispatch` runs are NOT auto-cancelled, so you must cancel them manually.

### 3.5: Handle Reproduction Results

**⛔ GATE: Do not proceed past this point until the CI run has completed.**

If there are failure artifacts, download them:

```bash
# Download failure artifacts
gh run download <run-id> --repo dotnet/aspire --dir /tmp/failure-logs

# Or get logs directly via the GitHub API / MCP tools
gh api "repos/dotnet/aspire/actions/jobs/<job_id>/logs" > /tmp/failure.log
```

**Distinguishing test failures from infrastructure failures:**

CI runners sometimes fail due to infrastructure issues, NOT the test itself. Common infrastructure failures include:
- `Failed to install or invoke dotnet...` (exit code -1073741502 on Windows)
- `The runner has received a shutdown signal` or runner timeouts
- Network connectivity errors during `dotnet restore`

**These do NOT count as reproductions.** Check the actual error message — only count iterations where the **test itself** failed with the expected error pattern from the tracking issue.

**If some runners show test failures (the expected error)**: Reproduction successful ✅. Proceed to Step 4.

**If no runners show the expected test failure — scale up and retry:**

```sql
-- Track the scaling attempt
INSERT OR REPLACE INTO session_state (key, value)
VALUES ('reproduce_attempt', CAST((SELECT CAST(value AS INTEGER) FROM session_state WHERE key = 'reproduce_attempt') + 1 AS TEXT));
```

Scale up progressively, focusing on the OS most likely to fail first (based on per-OS failure rates from the issue). Go back to Step 3.1 after each change:

| Attempt | `TARGET_OSES` | `RUNNERS_PER_OS` | `ITERATIONS_PER_RUNNER` | Notes |
|---------|---------------|-------------------|--------------------------|-------|
| 1 | Highest-failure-rate OS only | From heuristic table | From heuristic table | Start narrow — one OS, sized by failure rate |
| 2 | Same single OS | Same | 2× previous | Double `ITERATIONS_PER_RUNNER` only |
| 3 | Add second-worst OS (if available) | Same | Same as attempt 2 | Expand OS coverage, keep iteration count |

**Upper bounds**: Do not exceed `RUNNERS_PER_OS=10` or `ITERATIONS_PER_RUNNER=50` (total matrix entries must stay ≤ 256 per GitHub Actions limits).

**If 2+ attempts at ≥95% confidence produce zero test failures**: The test is likely **contention-sensitive** — it only fails when running alongside other tests, which the reproduce workflow doesn't simulate. In this case:
1. Fall back to analyzing existing quarantine failure logs (Step 1.5)
2. Read the test code to identify contention indicators (shared ports, shared fixtures, sequential waits)
3. Proceed to Step 4 using quarantine logs as your failure evidence
4. The verification run (Step 5) will still validate your fix in isolation, which is useful even if you can't reproduce the original failure

**CRITICAL: Windows log encoding gotcha**

Windows CI log files downloaded as artifacts are encoded as **UTF-16LE**. Running `cat` on them produces garbled output. Convert first:

```bash
# Convert Windows log to readable UTF-8
iconv -f UTF-16LE -t UTF-8 /tmp/failure-logs/failures-windows-latest-1/test-output.log > /tmp/readable-windows.log
cat /tmp/readable-windows.log
```

**Tip**: Using `get_job_logs` via GitHub API/MCP tools returns UTF-8 directly, avoiding encoding issues entirely. Prefer API-based log retrieval when possible.

**Alternatively**, search for the error directly:

```bash
# Search across all failure logs (handles encoding)
find /tmp/failure-logs -name "*.log" -exec grep -l "Assert\|Error\|Exception" {} \;
```

## Step 4: Identify Root Cause

### Interpreting Reproduction Results

- **Some runners fail, some pass**: This is the expected pattern for a flaky test. Proceed to analyze the failures.
- **All runners fail (100%)**: Compare against the failure rate from the tracking issue. If the issue says e.g. 84% and you see 100%, that's consistent — proceed. But if the issue says e.g. 10% and you see 100%, this may be an **unrelated issue** (e.g., a build break, a new dependency problem). Investigate whether the failure is the same error as reported in the issue before attempting a fix.
- **No runners fail**: The test may not be reliably reproducible with your current iteration count. Increase `RUNNERS_PER_OS` and `ITERATIONS_PER_RUNNER` and try again.

### Analyzing Failure Logs

Failure logs may come from local runs (Step 2, in `/tmp/test-results-*/`), CI reproduce runs (Step 3), or existing quarantine runs (Step 1.5). All are valid sources.

**Preferred: Use GitHub API/MCP tools** to get logs directly (avoids encoding issues):

```bash
# Get job logs via GitHub MCP tool: get_job_logs with job_id, return_content: true, tail_lines: 300
# Or via CLI:
gh api "repos/dotnet/aspire/actions/jobs/<job_id>/logs" > /tmp/failure.log
```

**Delegate log analysis to a sub-agent** to keep the main context clean:

```
Use a task agent (explore or general-purpose) to analyze the failure logs:
- Pass the log file paths or content
- Ask it to identify the specific assertion/exception
- Ask it to read the test source code and identify the concurrency/timing model
- Have it return a structured root cause summary
```

Look for the assertion or exception that failed:

```bash
# Find the actual test failure in logs
grep -A 10 "FAIL\|Assert\.\|Exception" /tmp/failure.log | head -50

# For .trx files (XML test results) from downloaded artifacts
find /tmp/failure-logs -name "*.trx" -exec grep -l 'outcome="Failed"' {} \;
```

Then find the corresponding test code and understand the concurrency/timing model.

### ✅ Step 4 Checkpoint

Before proceeding to Step 5, confirm you have:
- [ ] Examined CI failure logs (from reproduce runs OR existing quarantine runs)
- [ ] Identified the specific error (assertion failure, exception, timeout)
- [ ] Read the test source code and identified the root cause
- [ ] Documented the root cause in your session plan

**Now — and only now — proceed to write the fix.**

## Step 5: Apply Fix and Verify

> **⚠️ DO NOT remove the `[QuarantinedTest]` attribute or close the tracking issue.** Unquarantining is a separate process that happens after 21 days of zero failures in quarantine CI. Your fix PR should contain _only_ the code fix. See Step 6.4 for details.

### 5.1: Apply the Fix

1. Make the code change
2. **Build locally to confirm it compiles**:
   ```bash
   dotnet build tests/<TestProject>.Tests/<TestProject>.Tests.csproj --no-restore -v:q
   ```
3. Keep `reproduce-flaky-tests.yml` configured for the same test

### 5.2: Local Verification (Fast Pre-Check)

> **Principle: Local runs are a fast pre-check, not a substitute for CI.** Running a test N times on one machine does not have the same statistical power as N runs across separate CI runners. Some flakiness stems from environmental variation (machine load, Docker daemon state, network conditions) that a single machine cannot reproduce. Local verification catches obvious regressions quickly and saves CI round-trips, but CI verification is always required as the final gate.

If local reproduction succeeded in Step 2, run a quick local verification first:

```bash
# Rebuild with fix
dotnet build tests/<TestProject>.Tests/<TestProject>.Tests.csproj --no-restore -v:q

# Quick local check — same iteration count as reproduction
# Linux/macOS:
./.github/skills/fix-flaky-test/run-test-repeatedly.sh -n 20 -- \
  dotnet test tests/<TestProject>.Tests/<TestProject>.Tests.csproj --no-build \
  -- --filter-method "*.<TestMethodName>"
# Windows (PowerShell):
# ./.github/skills/fix-flaky-test/run-test-repeatedly.ps1 -n 20 -- dotnet test tests/<TestProject>.Tests/<TestProject>.Tests.csproj --no-build -- --filter-method "*.<TestMethodName>"
```

If local verification fails, iterate on the fix before going to CI. This saves ~30 minutes per CI round-trip.

### 5.3: Choose CI Verification Scale

CI verification is always required. However, the scale should reflect your **local confidence** — how much evidence you already have that the fix is correct.

#### Assessing Local Confidence

Consider these factors to determine how aggressively to scale CI verification:

**Higher confidence** (scale CI down):
- Root cause matches a well-known flaky pattern (from the patterns reference)
- Fix is a mechanical pattern application (e.g., replacing bare `HttpClient` with resilient one)
- Local reproduction succeeded and the fix eliminated the failure
- Current OS matches the primarily affected OS
- Failure rate is high (>20%), making local runs more meaningful

**Lower confidence** (scale CI up):
- Root cause is a hypothesis, not confirmed by reproduction
- Fix involves behavioral changes or new logic
- Local reproduction failed (wrong OS, contention-sensitive, or very low failure rate)
- Failure is OS-specific and you're on a different OS
- Failure rate is low (<10%), meaning local runs provide weak signal

#### CI Scale Heuristic

Use the **original failure rate** combined with your local confidence to size the CI verification. The base scale ensures that if the bug were still present, it would manifest with ≥95% probability (`n ≥ log(0.05) / log(1-p)`):

| Original Failure Rate | High Confidence (CI scale) | Low Confidence (CI scale) |
|---|---|---|
| >50% | 3 × 3 per OS (9 total) | 3 × 3 per OS (9 total) |
| 20-50% | 3 × 5 per OS (15 total) | 5 × 5 per OS (25 total) |
| 10-20% | 5 × 5 per OS (25 total) | 5 × 10 per OS (50 total) |
| 5-10% | 5 × 10 per OS (50 total) | 10 × 10 per OS (100 total) |
| <5% | 10 × 10 per OS (100 total) | 10 × 25 per OS (250 total) |

For tests with very low failure rates (<5%), consider whether the verification is practical within CI budget constraints. If not, document the limitation and rely on the 21-day quarantine monitoring to confirm.

**For contention-sensitive tests** (where reproduction in isolation didn't work): The verification run still validates that the fix doesn't break the test. Use the low-confidence column since you couldn't reproduce locally. The 21-day quarantine monitoring will provide the definitive confirmation under real contention.

### 5.4: Push and Verify on CI

Push the fix to the **investigation branch** (where reproduce workflow is already configured):

```bash
git add -A
git commit -m "Fix flaky test: <description of fix>"
git push
```

Then trigger the reproduce workflow to verify:

```bash
gh workflow run reproduce-flaky-tests.yml --repo dotnet/aspire --ref <fix-branch>-investigate
```

If the workflow dispatch fails due to permissions (HTTP 403), see the guidance in Step 3.3. Continue to Step 6 but document the failure in the PR description.

Store the verification run ID:
```sql
INSERT OR REPLACE INTO session_state (key, value) VALUES ('verify_run_id', '<run-id>');
INSERT OR REPLACE INTO session_state (key, value) VALUES ('fix_attempt', '1');
```

Wait for CI to complete. Monitor with polling (`gh run view --json status,conclusion`), not `gh run watch`.

### 5.5: Handle Verification Results

**If all iterations pass across all OSes**: The fix is validated ✅. Proceed to Step 6.

**If some iterations still fail**: The fix is incomplete or incorrect. Iterate:

```sql
-- Track the fix attempt
INSERT OR REPLACE INTO session_state (key, value)
VALUES ('fix_attempt', CAST((SELECT CAST(value AS INTEGER) FROM session_state WHERE key = 'fix_attempt') + 1 AS TEXT));
```

1. Download the new failure logs:
   ```bash
   gh run download <run-id> --repo dotnet/aspire --dir /tmp/failure-logs
   ```
2. Analyze the new failure pattern — is it the same error or a different one?
3. Refine the fix based on the new evidence
4. Push and re-verify

**After 3 failed fix attempts**: Stop and report findings to the user. The issue may require deeper architectural changes or domain expertise.

## Step 6: Clean Up and Create Final PR

After the fix is verified on the investigation branch, create a clean fix PR.

### 6.0: Cancel Any Remaining CI Runs

Cancel any in-progress reproduce or verify runs that are no longer needed:

```bash
# List and cancel any remaining runs on your branch
gh run list --repo dotnet/aspire --branch <branch> --status in_progress --json databaseId,name --jq '.[] | "\(.databaseId) \(.name)"'
gh run cancel <run-id> --repo dotnet/aspire
```

### 6.1: Cherry-Pick Fix to the Clean Branch

Switch back to the fix branch and cherry-pick only the code fix commits (not the workflow changes):

```bash
git checkout <fix-branch>

# Cherry-pick the fix commit(s) from the investigation branch
git cherry-pick <fix-commit-sha>

# Verify the fix branch has NO workflow changes
git diff main -- .github/workflows/  # Should be empty
```

### 6.2: Push and Open Final PR

```bash
git push
```

**Determine the PR title prefix**: Check whether any user interaction occurred during the investigation:

```sql
SELECT value FROM session_state WHERE key = 'user_interaction';
```

- If `user_interaction` is `'false'`: prefix the PR title with `[automated] `
- If `user_interaction` is `'true'`: no prefix

Open a non-draft PR with the fix. The PR body **must** include a note that it was created using the fix-flaky-test skill:

```bash
gh pr create --repo dotnet/aspire \
  --title "<prefix>Fix flaky test: <description>" \
  --body "## Flaky Test Fix

### Test
- **Method**: \`<fully qualified test name>\`
- **Issue**: #<issue-number>

### Root Cause
<1-2 sentence description of the root cause>

### Fix
<1-2 sentence description of what was changed>

### Verification
| Run | Config | Result |
|-----|--------|--------|
| Pre-fix (local) | <iterations>, <OS> | **<pass/fail>** |
| Post-fix (local) | <iterations>, <OS> | **<pass/fail>** |
| Post-fix (CI) | <runners × iters × OSes> | **<link to run>** |

> **If any verification step was skipped or failed** (e.g. workflow dispatch permission error), replace the CI row with a clear explanation:
> - What step failed and the exact error (e.g. \\\`HTTP 403: Resource not accessible by integration\\\`)
> - Why it could not be completed (e.g. agent token lacks \\\`actions:write\\\` permission)
> - The manual command a reviewer can run to complete verification
> - A link to the investigation PR/branch with the pre-configured reproduce workflow

### Verification Rationale
<Brief explanation of CI scale choice: local confidence level, why that scale was appropriate for the failure rate, and acknowledgment that local runs are a pre-check — not equivalent to CI runs across separate runners.>

### Notes
- \`[QuarantinedTest]\` attribute kept — unquarantining will happen separately after 21 days of zero failures in quarantine CI

---
*This fix was generated using the [fix-flaky-test skill](https://github.com/dotnet/aspire/blob/main/.github/skills/fix-flaky-test/SKILL.md).*"
```

### 6.3: Close the Investigation PR

```bash
# Close the investigation draft PR
gh pr close <investigation-pr-number> --repo dotnet/aspire --delete-branch
```

### 6.4: DO NOT Unquarantine or Close the Issue

**Important policy**: A code fix alone is not sufficient to unquarantine a test. The test must have **zero failures across all OSes for 21 consecutive days** in the quarantine CI runs before it can be unquarantined. See `docs/unquarantine-policy.md`.

- **DO NOT** remove the `[QuarantinedTest]` attribute
- **DO NOT** close the tracking issue
- A separate process monitors the quarantine CI and handles unquarantining when the 21-day criteria are met

### ✅ Final Validation Checklist

Before opening the final PR, verify **every item**. This is a hard gate — do not skip any item.

- [ ] Fix is verified on CI via the reproduce workflow (all iterations pass), **OR** if CI could not be triggered (e.g. permissions error), the PR description documents the failure, the exact error, and provides the manual trigger command for a reviewer
- [ ] **`[QuarantinedTest]` attribute is still present** on the test method (not removed)
- [ ] **Tracking issue is still open** (not closed)
- [ ] Clean fix PR is open with only code changes (no workflow modifications)
- [ ] PR description includes verification rationale (local confidence, CI scale reasoning)
- [ ] PR description documents any skipped or failed steps with exact errors and manual remediation commands
- [ ] Investigation draft PR is closed and branch deleted
- [ ] No remaining in-progress CI runs on the investigation branch
- [ ] Summary comment posted (see Response Format below)

**Self-check**: Run `git diff` on the fix branch and scan for any unintended changes — removed test attributes, workflow file edits, or unrelated modifications.

```sql
UPDATE todos SET status = 'done' WHERE id = 'cleanup';
```

## Key Technical Details

### Build System Quarantine Filtering

`eng/Testing.props` auto-appends `--filter-not-trait "quarantined=true"` to test arguments via the `TestRunnerAdditionalArguments` MSBuild property. This property is evaluated during `dotnet test` even with `--no-build`, so it must be handled in both build and test commands:

- **Reproduce workflow**: Overrides `_NonQuarantinedTestRunAdditionalArgs` to empty, removing the quarantine exclusion filter for all tests
- **Local reproduction**: Pass `/p:RunQuarantinedTests=true` to both `dotnet build` and `dotnet test`

`Testing.props` also adds `--ignore-exit-code 8`, which masks zero-test runs as successes. The workflow and `run-test-repeatedly` scripts detect this by checking test output for the `Total:` count indicator.

### test-reproduce.yml Architecture

The workflow:
1. **Setup job**: Parses env vars, generates a matrix of `{os, index}` combinations
2. **Reproduce jobs** (parallel): Each runner builds the test project once, then loops through iterations with DCP process cleanup between runs
3. **Results job**: Aggregates pass/fail across all runners into a summary table

Failed iterations upload their test output as artifacts named `failures-<os>-<index>`.

### workflow_dispatch Behavior

`workflow_dispatch` requires the workflow file to exist on the **default branch** (`main`). Key implications:

- You can dispatch it against any branch with `gh workflow run reproduce-flaky-tests.yml --ref <branch>`. GitHub discovers the workflow from `main` but runs the version from the specified `--ref`. This means your investigation branch's env var edits will be used.
- The investigation branch has `ci.yml` disabled, so pushes don't trigger full CI — only `workflow_dispatch` of the reproduce workflow is used.
- **Creating a new workflow file on a feature branch won't help** — GitHub won't discover it via `workflow_dispatch` until it's merged to `main`.

## Response Format

After completing a flaky test fix, provide a summary:

```markdown
## Flaky Test Fix Summary

### Test
- **Method**: `Namespace.Type.Method`
- **Issue**: #XXXXX
- **Project**: `tests/Aspire.{Project}.Tests/`

### Failure Data
| OS | Failure Rate |
|---|---|
| Windows | XX% |
| Linux | XX% |

### Root Cause
Brief description of what caused the flaky behavior.

### Fix
Description of the code change.

### Verification
| Run | Config | Result |
|-----|--------|--------|
| Pre-fix | X runners × Y iters × Z OSes | N failures ❌ |
| Post-fix | X runners × Y iters × Z OSes | All passed ✅ |

### Files Changed
- `path/to/file.cs` — description

### Next Steps
- Test remains quarantined — will be unquarantined after 21 days of zero failures
- Issue #XXXXX remains open — will be closed by the unquarantine process
```

## Important Constraints

- **Reproduce before fixing**: Always confirm the failure is reproducible before attempting a fix — try locally first, then CI. For contention-sensitive tests, existing quarantine logs may serve as sufficient evidence (see Step 1.5)
- **Try local first**: Use `run-test-repeatedly.sh` (Linux/macOS) or `run-test-repeatedly.ps1` (Windows) for fast feedback (~minutes). Fall back to CI when local reproduction fails (wrong OS, contention-sensitive, very low failure rate)
- **Detect your OS**: Check with `uname -s` to decide if local reproduction is viable for the failing OS
- **Quarantined tests need /p:RunQuarantinedTests=true**: The build system filters them out by default. Pass this property to both `dotnet build` and `dotnet test` commands for local reproduction. The CI reproduce workflow handles this automatically.
- **Watch for zero-test runs**: If the test runner reports zero tests executed (exit code 8, masked by `--ignore-exit-code 8`), the filter or quarantine settings are misconfigured. The `run-test-repeatedly` scripts and reproduce workflow detect this automatically.
- **Keep investigation notes in session workspace**: Use `plan.md` and `files/` in the session workspace, not a directory in the repo
- **Use two branches**: Investigation branch (draft PR with disabled ci.yml) for reproduce/verify; fix branch (clean PR with only code changes) for the final submission
- **Distinguish infrastructure vs test failures**: CI runners sometimes fail due to infrastructure issues (e.g., `Failed to install or invoke dotnet...` on Windows). These do NOT count as test reproductions. Always verify the error matches the expected test failure pattern.
- **DO NOT unquarantine or close issue**: The test stays quarantined until 21 days of zero failures (see `docs/unquarantine-policy.md`)
- **Scale verification to failure rate**: A 50% failure rate test needs fewer verification iterations than a 5% failure rate test. Use the verification heuristic table.
- **Target specific OSes**: Focus on the OS with the highest failure rate first. Only expand to multiple OSes when the rate is high (>20%) on multiple OSes or when initial attempts don't reproduce.
- **Build-verify everything**: After fixes, after any test attribute changes
- **Don't fix unrelated issues**: If you encounter unrelated test failures, ignore them
- **Windows UTF-16LE**: Always handle encoding when reading Windows CI logs downloaded as files (not needed when using `get_job_logs` via GitHub API/MCP, which returns UTF-8)
- **Prefer polling over `gh run watch`**: Use `gh run view --json status,conclusion` to check CI status — `gh run watch` produces excessive output that floods the context window
- **Use sub-agents for heavy work**: Delegate log analysis and CI monitoring to sub-agents to keep main context clean
- **Track state in SQL**: Use the todos table and session_state for tracking progress across the investigate→reproduce→fix→verify cycle

## Appendix: Flaky Test Patterns Reference

Common flaky test patterns are documented in `.github/instructions/test-review-guidelines.instructions.md`. Consult that file during Step 1 (gather data) to form hypotheses, and during Step 4 (analysis) to confirm root causes.
