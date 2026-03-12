# Auto-rerun transient CI failures

This document describes the current behavior contract for `.github/workflows/auto-rerun-transient-ci-failures.yml`.

## Overview

The workflow analyzes failed `CI` pull request runs, identifies retry-safe transient infrastructure failures, requests reruns for the matched jobs through GitHub's job-rerun API, and comments on the open pull request with the rerun details.

GitHub's job-rerun API also reruns downstream dependent jobs, so the workflow targets only the matched jobs when issuing rerun requests, but the resulting rerun attempt can include dependents that GitHub schedules automatically.

It is intentionally conservative:

- it does not rerun every failed job in a run
- it treats mixed deterministic failures plus transient post-step noise as non-retryable by default
- it keeps `workflow_dispatch` in dry-run mode for historical inspection and matcher tuning

## Matcher behavior

- Retry jobs with no failed steps only when their annotations contain an explicit job-level infrastructure signature.
- Retry jobs whose failed step is on the retry-safe allowlist only when their annotations also contain a transient infrastructure signature.
- Ignore aggregator jobs such as `Final Results` and `Tests / Final Test Results`.
- Skip jobs whose failed steps are outside the retry-safe allowlist, even if their annotations contain generic failure text.
- Keep the mixed-failure veto: if an ignored step such as `Run tests*` failed, do not rerun the job based only on unrelated transient post-step noise.
- Allow a narrow override when an ignored failed step is paired with a high-confidence job-level infrastructure annotation such as runner loss or action-download failure.
- Allow a narrow override for Windows jobs whose failures are limited to post-test cleanup or upload steps when the annotations report process initialization failure `-1073741502` (`0xC0000142`).
- Allow a narrow log-based override for non-test-execution failures when the job log shows high-confidence infrastructure network failures against approved `dnceng` public feeds, `builds.dotnet.microsoft.com`, `api.github.com`, or `github.com`.

## Safety rails

- `workflow_dispatch` remains dry-run only. It exists for historical inspection and matcher tuning, not for issuing reruns.
- Automatic rerun requires at least one retryable job.
- Automatic rerun is suppressed when matched jobs exceed the configured cap.
- Before issuing reruns, the workflow confirms that at least one associated pull request is still open.
- The workflow targets only the matched jobs when issuing rerun requests rather than rerunning the entire source run, although GitHub's job-rerun API also reruns dependent jobs automatically.
- The workflow summary links to the analyzed workflow run and, when reruns are requested, to both the failed attempt and the rerun attempt.
- After successful rerun requests, the workflow comments on the open associated pull request with links to the failed attempt, the rerun attempt, per-job failed-attempt links, and retry reasons.

## Tests

The automated tests for this workflow live in `tests/Infrastructure.Tests/WorkflowScripts/AutoRerunTransientCiFailuresTests.cs`.

Those tests are intentionally behavior-focused rather than regex-focused:

- they use representative fixtures for each supported behavior
- they keep representative job and step fixtures anchored to the current CI workflow names so matcher coverage does not drift from the implementation
- they cover the mixed-failure veto and ignored-step override explicitly
- they keep only a minimal set of YAML contract checks for safety rails such as `workflow_dispatch` dry-run mode, first-attempt-only automatic reruns, and gating the rerun job on `rerun_eligible`
