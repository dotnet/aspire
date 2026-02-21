# Fix-Flaky-Test Skill: Investigation Findings & Improvement Proposals

## Context

This document captures observations from using the `fix-flaky-test` skill to investigate [dotnet/aspire#10977](https://github.com/dotnet/aspire/issues/10977) — a quarantined test `DependentResourceWaitsForOpenAIModelResourceWithHealthCheckToBeHealthy` with a ~7% failure rate on Linux.

## Problems Hit During Investigation

### 1. Failure logs are inaccessible or missing

**Problem:** The quarantine issue body contains ✅/❌ emoji-linked run URLs, but:
- No actual error output is included in the issue itself
- Old CI run logs expire (GitHub returns 410 Gone for logs older than ~90 days)
- Finding the specific failed *job* within a run requires iterating through all jobs via API
- In some ❌ runs, the OpenAI test job actually *passed* — the ❌ was for a different job (e.g., Hosting.Azure). The issue only tracks whether the *run* failed, not whether the *specific test* failed.

**Impact:** The agent spent significant context on API calls to locate failed jobs, only to find logs were expired or the test wasn't the actual failure.

**Examples of expired/missing logs during this investigation:**

- **Original failure run (16983665005, Aug 2025):** The issue was filed from this run. When the agent tried to download logs for the failed job (ID 48148497418), GitHub returned `410 Gone` — the logs had expired after ~90 days.
  ```
  MCP server 'github-mcp-server': failed to get job logs for job 48148497418:
  unexpected status code: 410 Gone
  ```
- **Feb 18 failure run (22147848990):** The issue's ❌ link pointed to this run. The agent iterated through all 24 jobs to find the OpenAI failure, but discovered the OpenAI jobs all showed a "success" conclusion. However, this was a **misdiagnosis**: the quarantine workflow uses `ignoreTestFailures: true`, so jobs succeed even when individual tests fail. The agent should have downloaded the `.trx` artifact files to check for individual test failures instead of relying on job-level conclusion status. The ❌ on the run was likely caused by a different job failing, but the OpenAI test may still have failed within its successful job.
- **Feb 13 failure run (21979527885):** Same pattern — the agent checked job conclusions and found them all "success", but this does not mean the OpenAI test passed. The `.trx` files in the uploaded artifacts would contain the actual per-test pass/fail results.
- **Earlier failure runs (21954725767, 21950312313, 21946082059):** The agent checked three more ❌ runs using the same flawed methodology (job conclusion filtering). The actual test-level results were not verified.

In total, the agent checked **6 different ❌ runs** from the issue but used job-level conclusion status to determine test outcomes. Since the quarantine workflow uses `ignoreTestFailures: true`, all test jobs show "success" regardless of individual test failures. The agent should have downloaded `.trx` artifacts to check per-test results. The issue's run-level ❌ tracking is also misleading — it tracks whether the *run* failed, not whether a *specific test* failed.

**Suggestions:**
- ✅ **FIXED**: The skill now instructs the agent to download `.trx` artifacts and parse them for per-test results, instead of relying on job conclusion status
- ✅ **FIXED**: Added `retention-days: 30` to test result artifact upload in `run-tests.yml`
- The quarantine bot should include **job IDs** for the specific failed jobs in the issue body
- Include a **10-20 line error snippet** from the failing test directly in the issue
- Distinguish between "this specific test failed" vs "the run failed but this test passed"
- Consider storing failure logs as GitHub Actions artifacts (they persist longer than job logs)

### 2. Could not reproduce the failure

**Problem:** Two CI reproduction attempts (100 iterations single-test, 15 iterations quarantine-project) produced zero failures. With a 7% failure rate, we'd expect ~7 failures in 100 iterations.

**Possible reasons:**
- The failure depends on **external service availability** (OpenAI status page at `status.openai.com`)
- The failure may require **contention** from other tests running in parallel (quarantine suite runs many test projects concurrently)
- The 7% rate may be **bursty** (correlated with OpenAI outages), not uniformly distributed
- The reproduce workflow runs the test in isolation, but the quarantine workflow runs the full test project

**Suggestions:**
- The skill should recognize when a test depends on external services and adjust strategy (e.g., mock the external dependency, or skip reproduction)
- Consider a "stress mode" that runs with higher parallelism to simulate contention
- If the failure rate is low and depends on external factors, allow proceeding with a code-analysis-only fix

### 3. Main context fills up with CI polling boilerplate

**Problem:** The stash/commit/push/poll/read cycle for CI reproduction consumed a large portion of the context window:
- Stashing the fix, committing the reproduce config, pushing, waiting for run to start
- Polling every 2-3 minutes (each poll = tool call + response)
- Popping the stash, re-pushing with different config
- Multiple runs (pre-fix single-test, pre-fix quarantine-project, post-fix verify)

**Impact:** By the time the investigation reached the fix verification phase, context was significantly consumed.

**Suggestions:**
- Use a **sub-agent (task tool)** for all CI workflow management (push, poll, report results)
- Instead of frequent polling, wait a **fixed duration** (e.g., 10 minutes) then check once
- The reproduce workflow could **post results as a PR comment** so the agent reads a summary instead of polling
- Reduce the number of CI runs — skip pre-fix reproduction if root cause is clear from code analysis

### 4. Skill forces linear reproduction even when root cause is obvious

**Problem:** The skill's process requires: gather → reproduce → analyze → fix → verify. But in this case, the root cause was immediately clear from comparing the quarantined test with its non-quarantined sibling:
- Non-quarantined test explicitly removes the parent's `resource_check` health check (which calls `status.openai.com`)
- Quarantined test does NOT remove it, so the model inherits an external HTTP health check
- The fix is simply adding the same removal code

**Impact:** Time spent on reproduction (Steps 2-3.5) yielded no useful signal.

**Suggestions:**
- Add a "fast path" when code analysis reveals an obvious root cause with clear evidence (e.g., sibling test demonstrates the fix pattern)
- Allow the agent to propose skipping reproduction with justification
- The skill could include a decision point: "Root cause identified via code analysis. Confidence: HIGH. Reproduce or proceed to fix?"

## Specific Observations About the Quarantine Issue Format

### What's useful in the current format
- Failure rate statistics (last 7/14/30 days)
- Per-OS breakdown
- Key runs with dates
- Original stack trace in the issue body

### What's missing or could be improved

| Gap | Suggestion |
|-----|-----------|
| No failed job IDs | Include the specific job ID for each ❌ run where the test failed |
| No inline error logs | Paste 10-20 lines of the actual test failure output for each ❌ run |
| Run-level vs test-level tracking | Distinguish "this run failed because of this test" vs "this run failed for unrelated reasons" |
| No categorization of failure type | Tag failures as timeout, assertion, infrastructure, external-dependency, etc. |
| No link to test source | Include a permalink to the test method in the repo |
| Staleness of old run logs | Store failure artifacts that don't expire, or cache failure output in the issue |

## Root Cause Analysis (for the specific test)

**Test:** `DependentResourceWaitsForOpenAIModelResourceWithHealthCheckToBeHealthy`

**Root cause:** The `OpenAIModelResource` (child) inherits its parent `OpenAIResource`'s `resource_check` health check annotation via `TryGetAnnotationsIncludingAncestorsOfType`. This health check (`OpenAIHealthCheck`) makes an HTTP GET to `https://status.openai.com/api/v2/status.json` with a 5-second timeout. When this external call fails or times out in CI, the model resource stays unhealthy, and the test's `WaitForResourceHealthyAsync` blocks until the 180-second CTS expires.

**Evidence:**
- The non-quarantined sibling test (`DependentResourceWaitsForOpenAIResourceWithHealthCheckToBeHealthy`) explicitly removes `resource_check` (lines 80-81 of `OpenAIFunctionalTests.cs`)
- Failures are Linux-only (7%), consistent with CI network variability
- Stack trace shows `OperationCanceledException` in `WaitForResourceHealthyAsync` → timeout

**Fix applied:** Remove the parent's `resource_check` health check annotation in the quarantined test, matching the sibling test's pattern.

## Proposed Skill Changes (Summary)

1. **Prioritize log analysis** — Download and analyze existing failure logs before attempting reproduction
2. **Add a code-analysis fast path** — When root cause is clear from static analysis, allow skipping reproduction
3. **Offload CI management to sub-agents** — Use task tool for push/poll/read to save main context
4. **Improve quarantine issue format** — Include job IDs, failure snippets, and test-level tracking
5. **Recognize external dependencies** — When a test depends on external services, adjust reproduction strategy
