# Opt-in CI shadow experiments

The shadow workflow compares one fixed workload without changing production CI,
required checks, or platform coverage. It runs only in `microsoft/aspire`.
Do not use fork timings as production-runner evidence.

## Activation and source identity

`.github/workflows/ci-shadow.yml` accepts a manual dispatch for an upstream PR
number or an explicit full source SHA. A PR is resolved once to its head
repository and SHA. Supplying both the number and SHA rejects a changed head.
Every arm and repetition uses that exact SHA, not a moving branch or merge ref.
The harness revision is recorded separately.

GitHub requires a `workflow_dispatch` workflow to exist on the default branch
before it can be dispatched. Before that registration, an upstream **draft PR**
whose title contains **`[ci-shadow]`** opts in through `pull_request`, but only
when it changes the shadow workflows, scripts, tests, or this document.
Opening, reopening, synchronizing, editing, or converting that PR to draft
can start a trial. An ordinary PR does not schedule the experiment.

The workflow never uses `pull_request_target`, writes to the repository,
publishes packages, posts comments, or uses an environment or repository secret.
Checkout credentials are not persisted. Source execution receives no `GH_TOKEN`;
only planning and the separate reporting job use read-only API credentials.
Normal first-time-contributor approval and Actions policy still apply.
Read-only permissions are not a sandbox: source code still executes on disposable
hosted runners and can forge its own logs. Inspect evidence before trusting it.

After default-branch registration, dispatch from the reviewed harness branch:

```bash
gh workflow run ci-shadow.yml --repo microsoft/aspire --ref <harness-branch> \
  -f pull_request=<number> -f source_sha=<expected-full-head-sha> -f repetitions=3
```

For a source commit without an upstream PR:

```bash
gh workflow run ci-shadow.yml --repo microsoft/aspire --ref <harness-branch> \
  -f source_repository=microsoft/aspire -f source_sha=<full-source-sha> -f repetitions=3
```

`source_repository` also supports an explicit fork source commit; execution still
runs upstream. Verify the workflow/ref and source SHA before dispatching.
No workflow is dispatched automatically by these instructions.

## First experiment: reuse managed test output

The fixed project is
`tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj`. Each arm executes exactly
`Aspire.Hosting.Tests.StableConnectionStringBuilderTests` and
`Aspire.Hosting.Tests.ResourceBuilderLifetimeTests`, one class per shard.
No Docker/native package producers are needed for these classes.

```text
baseline: two fresh jobs, each builds the project -> runs one class
candidate: one fresh build -> upload immutable payload
                             -> two fresh jobs download -> bootstrap -> run one class
```

Both arms use the project's existing `8-core-ubuntu-latest` production runner
label, identical CI build flags, direct compiled-DLL MTP execution, and quarantine,
outerloop, and known-failure exclusions. No runner is enlarged and no dependency
cache is added. The evaluated `MtpBaseArgs` are captured from the source project;
empty results still fail even if those arguments ignore MTP exit code 8.

The producer archives the unchanged
`artifacts/bin/Aspire.Hosting.Tests/Debug/net10.0` output and records its file
inventory. It does not enable `PrepareForHelix`, which would change xUnit settings.
Consumers retain a pinned source checkout for repository-root lookups but never
invoke MSBuild or project restore. They bootstrap the source-pinned SDK through
`dotnet.sh`, then install the producer's exact compatible framework patch versions.
That bootstrap cost is part of the experiment, not hidden.

One to three repetitions are allowed; three is the default. Repetitions run
sequentially to avoid competing with each other. Each repetition starts fresh
runners and independent output directories; artifacts are namespaced by
repetition, arm, and class. This is three repetitions within one workflow,
not three independent workflow runs.

## Evidence and interpretation

Artifacts expire after three days. Download evidence promptly:

```bash
gh run watch <run-id> --repo microsoft/aspire --exit-status
gh run download <run-id> --repo microsoft/aspire --dir <fresh-evidence-directory>
```

`shadow-plan` records the source, harness, selected classes, and repetition count.
Each `shadow-<N>-evidence-*` artifact contains phase commands, exit codes,
timestamps, image/CPU metadata, logs, and TRX results where applicable.
Producer artifacts include the payload file/size inventory; separate binlog
artifacts preserve build diagnostics. A failed workload preserves its measurement
before propagating the error.

The per-repetition report fails on missing or empty TRX, skipped or failed tests,
wrong classes, duplicate identities, provenance mismatches, or unequal
class/method/display-name/outcome inventories. Random execution/test GUIDs are
not parity keys. Missing or unsuccessful producer/consumer jobs also fail.

`shadow-<N>-comparison` contains original-attempt REST run/job/step timestamps
and the verified comparison. It reports:

- **Lane execution span:** first baseline shard start to last baseline completion,
  versus producer start to last payload consumer completion. The latter includes
  the artifact/upload/queue/download barrier, bootstrap, and post-job work.
- **Workflow-created to lane completion:** also includes the initial scheduling
  delay; it is not PR push-to-finish or required-check readiness.
- **Runner-seconds:** sum of lane job execution durations, a compute metric,
  not a wall-clock saving. Planning/reporting overhead remains visible separately
  in the raw timeline.

Take median, minimum, and maximum across all three successful, parity-verified
repetitions. Compare image versions and CPU counts, retain failures separately,
and do not substitute a rerun's timestamps for attempt 1.
Two baseline builds already run in parallel: sharing one build may reduce
runner-seconds while **increasing** wall time. No speedup is claimed until the
upstream experiment has run and its artifact closure and exact test inventory
have been inspected.

This bounded managed-output trial does not measure the entire pipeline,
change native producers, omit Windows/ARM coverage, or establish ten-minute CI.
Other platform/selector/native hypotheses need their own isolated experiments.
