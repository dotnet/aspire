# Windows ARM64 storage benchmark

This experiment compares Aspire's real native CLI archive build on the existing
runner disk, local NVMe formatted NTFS, and local NVMe formatted as a ReFS Dev
Drive. The manually dispatched path in
`.github/workflows/build-cli-native-archives.yml` runs only in `radical/aspire`.
Normal reusable-workflow calls do not provision storage.

## Run the pilot

Push the benchmark branch to the fork, then dispatch it. The run records its
immutable source commit:

```shell
gh workflow run build-cli-native-archives.yml --repo radical/aspire --ref <benchmark-branch>
gh run list --repo radical/aspire --workflow build-cli-native-archives.yml --branch <benchmark-branch> --limit 1
gh run watch <run-id> --repo radical/aspire --exit-status
```

The default matrix runs one sample each of `baseline`, `ntfs`, and `devdrive`
on `windows-11-vs2026-arm`. Every variant uses the same source commit, Release
configuration, build flags, and existing package verification. The benchmark
runner and RID are fixed; JSON cannot select a self-hosted machine. Provisioning
also requires `runner.environment` to be `github-hosted`. CLI and native
Dashboard smoke tests run against each variant's own archives. There is no
signing, publishing, dependency-cache restore, or explicit Defender policy or
exclusion changes. Dev Drive can change effective scanning behavior through its
default trust/performance mode.

The helper is fetched from this run's source SHA before checkout. It refuses
occupied D:, a populated or linked workspace, system/boot disks, non-RAW disks,
and inventories without exactly one eligible NVMe disk. A requested optimization
that cannot run fails with `not-exercised` evidence; it must not count as a
successful optimized sample. Provisioning errors fail the job.

Workspace relocation also moves workspace-relative NuGet packages, build output,
and Arcade's `artifacts/tmp`. `TEMP` and `TMP` move to NVMe. Runner-managed
`RUNNER_TEMP` is deliberately unchanged. This measures these path changes together,
not each path's individual contribution. The baseline has unchanged storage
placement, but includes the same extra evidence and smoke steps as the variants.

## Repeat and compare

After all three pilot builds and artifact checks pass, dispatch four further
rounds with samples 2 through 5. Supply `benchmarkTargets` using the same three objects as
the default matrix, updating `sample` and `benchmarkSuffix` together. For example:

```json
[
  {"storage":"baseline","sample":2,"benchmarkSuffix":"-baseline-2"},
  {"storage":"ntfs","sample":2,"benchmarkSuffix":"-ntfs-2"},
  {"storage":"devdrive","sample":2,"benchmarkSuffix":"-devdrive-2"}
]
```

The three jobs are concurrent, contemporaneous matched rounds; matrix array order
does not control execution order. Record actual start/end times. Each sample needs
a fresh runner. Keep the exact harness SHA fixed for all five rounds. A harness
fix requires a new matched set; preserve failed attempts separately. Keep runner
image and resolved tool versions comparable; record hardware variation rather
than claiming labels pin hardware. Report every sample, median and range, and
wins per matched round.
Do not claim meaningful tail-latency statistics from five samples.

For acceptance, each treatment uses its first five eligible contemporaneous pairs
with baseline, ordered by dispatch. Within a pair, require the same source SHA,
runner image/version, CPU model/count and RAM, OS, resolved SDK/MSVC toolchain,
Node/PowerShell/Visual Studio versions, and recorded Defender state/exclusions.
Image/tool drift excludes the pair from acceptance, regardless of its speed;
retain it as exploratory evidence. Compare treatment and baseline medians over
those same five pairs. Preserve image strata when images change between rounds.
Allow at most three replacement rounds beyond the initial five; if that cannot
produce five comparable pairs, the corresponding treatment is inconclusive.

Use the Actions jobs API for whole-job and per-step timestamps, including setup,
checkout, builds, verification, and artifact upload. Report queue time separately.
These timestamps do not measure pre-job VM allocation time. Save raw jobs by run ID
and attempt using `/actions/runs/<run-id>/attempts/<attempt>/jobs`. Record CPU
identity and resolved .NET, Node, PowerShell, Visual Studio and native toolset
versions; use binlogs to identify the native toolchain actually selected.
Download `windows-storage-*` for disk, path, image, Defender, and provisioning
evidence, and `cli-native-logs-*` for the existing build logs and binlogs:

```shell
gh api --paginate repos/radical/aspire/actions/runs/<run-id>/attempts/<attempt>/jobs
gh run download <run-id> --repo radical/aspire --pattern 'windows-storage-*' --dir <evidence-directory>
```

Artifact suffixes identify mode and sample. Preserve those suffixes when selecting
archives for follow-up validation. A green build does not establish consumer
compatibility. After the build comparison, install/extract the winning CLI from
the exact run/mode/sample and validate generated AppHost creation, build, start,
resource readiness and stop on `windows-11-arm`. Use the existing
`eng/scripts/cli-starter-validation.ps1 -ValidationRoot` parameter to place
generated apps on the selected disk. Persist consumer results before recommending
rollout. Existing Windows ARM64 tray UI smoke remains disabled and unvalidated.

Dispatch the consumer job separately after selecting an artifact:

```shell
gh workflow run build-cli-native-archives.yml --repo radical/aspire --ref <benchmark-branch> \
  -f validationRunId=<successful-run-id> -f validationMode=<mode> -f validationSample=<sample>
```

This skips native builds, provisions the selected storage mode on `windows-11-arm`,
downloads that exact archive and its treatment evidence, and checks the installed
CLI version against its archive name. These `-ci` builds omit commit metadata from
`--version`; source provenance is verified through the artifact run and matching
checkout reports, not by inventing an overridden CLI identity.
The existing starter scenarios exercise creation, startup,
resource readiness and shutdown for C# and TypeScript AppHosts. They use published
daily templates and dependencies, not a same-commit managed-package build; record
this boundary alongside the exact native artifact provenance.

## Decision

Adopt NVMe only with at least 10% lower median total execution time, including
provisioning, and a win in at least four of five matched rounds. All required
checks and consumer validation must pass. Compare NTFS and Dev Drive independently;
prefer NTFS if Dev Drive adds no repeatable material benefit. Report inconclusive
or unsuccessful results as such, rather than selecting an apparent winner.

The final report must identify the proposed change, each mode's median and range
in seconds, absolute and percentage net savings, per-round wins, provisioning
cost, source SHA, run/job links, and remaining limitations. Prepare an upstream
draft PR from the fork with those measured results. A harness-only PR must be
explicitly experimental and must not claim a production speedup.

Do not reformat the live x64 D: workspace. x64 experiments, production rollout,
production starter-validation wiring, and Azure pipeline changes are outside this
benchmark.

For the external motivation and measured runner layouts, see
[the t3code experiment](https://github.com/pingdotgg/t3code/pull/22454),
[the published filesystem probe](https://github.com/marcpems/windows-runner-filesystems),
and [Microsoft's Dev Drive documentation](https://learn.microsoft.com/windows/dev-drive/).
