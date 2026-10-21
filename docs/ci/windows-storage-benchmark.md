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
Allow at most three replacement pairs per treatment beyond the initial five;
if that cannot produce five comparable pairs, that treatment is inconclusive.

The RAM control uses the nominal 16 GiB class plus a maximum 1 MiB within-pair
raw-memory difference. This numerical tolerance was clarified after collection
began: the initial collector incorrectly treated a 40 KiB raw-memory difference
as a different RAM class. Preserve the raw values and original exclusions, and
disclose any reclassification. Other matching controls and performance thresholds
are unchanged; the source of that small raw-memory difference is not established.

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
Generated apps must be outside the checkout: its central package versions and
`Directory.Build.*` files are not part of a consumer application. The artifact
installer records an external `TEMP`-relative validation root; starter diagnostics
are uploaded separately from storage/provenance evidence.

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

## Measured Aspire results

**Local NVMe with Dev Drive reduced median native-build job time by 212 seconds
(17.9%) and won all five eligible comparisons.** This supports a scoped Windows
ARM64 native-build optimization, not a general Windows performance guarantee.
This PR is experimental: normal reusable-workflow calls retain existing storage.

All 23 native jobs completed successfully at the frozen source commit
`012cda4fd7cff02842f47844a6daa9bc625cdaf9`. Each retained the three real build
phases, package verifiers, TerminalHost smoke, and own-artifact CLI/Dashboard smoke.
All links below refer to attempt 1. Times are whole-job seconds, including
provisioning, evidence, checks and uploads, but excluding scheduling.

| Treatment | Eligible pairs | Matched C: baseline median (range), s | Treatment median (range), s | Net median saving | Wins | Timing verdict |
|---|---:|---:|---:|---:|---:|---|
| D: NVMe ReFS Dev Drive | 5 | 1187 (1098-1195) | 975 (936-1073) | 212 s / 17.9% | 5/5 | Meets the timing rule |
| D: NVMe NTFS | 4 | 1189.5 (1116-1380) | 989.5 (977-1011) | 200 s / 16.8% | 4/4 | Inconclusive: fewer than five pairs |

Each row uses its own contemporaneous baseline cohort; these are not a direct
Dev Drive-versus-NTFS comparison. Three bounded NTFS replacement pairs still left
only four eligible pairs. There were no further retries to obtain a preferred
result. In particular, these results do not establish that ReFS outperforms NTFS.

### Every observation and its eligibility

Each time links to its actual job. An excluded pair remains correctness and
provisioning evidence; it is not used for either acceptance threshold.

| Run | C: baseline, s | NVMe NTFS, s | Dev Drive, s | Eligible comparisons |
|---|---:|---:|---:|---|
| [Pilot](https://github.com/radical/aspire/actions/runs/39534476590) | [1116](https://github.com/radical/aspire/actions/runs/39534476590/job/119007825213) | [990](https://github.com/radical/aspire/actions/runs/39534476590/job/119007825118) | [1001](https://github.com/radical/aspire/actions/runs/39534476590/job/119007824846) | NTFS only; Dev Drive image mismatch |
| [Round 2](https://github.com/radical/aspire/actions/runs/39536298696) | [1167](https://github.com/radical/aspire/actions/runs/39536298696/job/119013623366) | [1032](https://github.com/radical/aspire/actions/runs/39536298696/job/119013623294) | [936](https://github.com/radical/aspire/actions/runs/39536298696/job/119013623094) | Dev Drive only; NTFS image mismatch |
| [Round 3](https://github.com/radical/aspire/actions/runs/39536300916) | [1187](https://github.com/radical/aspire/actions/runs/39536300916/job/119013629658) | [1011](https://github.com/radical/aspire/actions/runs/39536300916/job/119013629913) | [1073](https://github.com/radical/aspire/actions/runs/39536300916/job/119013629920) | Both |
| [Round 4](https://github.com/radical/aspire/actions/runs/39536303120) | [1195](https://github.com/radical/aspire/actions/runs/39536303120/job/119013636155) | [970](https://github.com/radical/aspire/actions/runs/39536303120/job/119013636472) | [975](https://github.com/radical/aspire/actions/runs/39536303120/job/119013636439) | Dev Drive only; NTFS image mismatch |
| [Round 5](https://github.com/radical/aspire/actions/runs/39536305382) | [1098](https://github.com/radical/aspire/actions/runs/39536305382/job/119013644421) | [992](https://github.com/radical/aspire/actions/runs/39536305382/job/119013644188) | [956](https://github.com/radical/aspire/actions/runs/39536305382/job/119013644551) | Dev Drive only; NTFS image mismatch |
| [Dev Drive replacement](https://github.com/radical/aspire/actions/runs/39538159673) | [1191](https://github.com/radical/aspire/actions/runs/39538159673/job/119019530786) | - | [990](https://github.com/radical/aspire/actions/runs/39538159673/job/119019530665) | Dev Drive |
| [NTFS replacement 1](https://github.com/radical/aspire/actions/runs/39539741093) | [1192](https://github.com/radical/aspire/actions/runs/39539741093/job/119024514097) | [977](https://github.com/radical/aspire/actions/runs/39539741093/job/119024514034) | - | NTFS |
| [NTFS replacement 2](https://github.com/radical/aspire/actions/runs/39539743035) | [1159](https://github.com/radical/aspire/actions/runs/39539743035/job/119024519359) | [972](https://github.com/radical/aspire/actions/runs/39539743035/job/119024519158) | - | Neither; NTFS image mismatch |
| [NTFS replacement 3](https://github.com/radical/aspire/actions/runs/39539745092) | [1380](https://github.com/radical/aspire/actions/runs/39539745092/job/119024525014) | [989](https://github.com/radical/aspire/actions/runs/39539745092/job/119024524656) | - | NTFS |

The eligible Dev Drive pair savings, in dispatch order, were 231, 114, 220, 142,
and 201 seconds. NTFS pair savings were 126, 176, 215, and 391 seconds, without
dropping the slow 1380-second baseline as an outlier.
Dispatch-to-job-start elapsed time was 3-4 seconds for these paired jobs. That
interval is not a measurement of VM allocation alone.

### Image and tool controls

`storage.json` and `build.json` record the actual environment for every job;
`BuildCli.binlog` identifies the selected native linker, not just installed tools.
Eligible pairs matched on all declared controls. Different images were retained
as separate strata, never matched across rounds to repair an excluded pair.

| Runner image version | Visual Studio | Build PowerShell | Defender product |
|---|---|---|---|
| 20261013.183.4 | 18.10.12224.181 | 7.6.6 | 4.18.25080.5 |
| 20261018.189.1 | 18.11.12306.390 | 7.6.7 | 4.18.26090.9 |

All eligible pairs used Cobalt 100 CPUs, four logical processors, nominal 16 GiB,
Windows 11 `10.0.26200`, Node `v22.23.3`, repository SDK
`11.0.100-rc.2.26504.105`, and the actual ARM64-host/ARM64-target linker
`MSVC/14.51.36231/bin/Hostarm64/arm64/link.exe`. Defender real-time protection
was enabled, with both `C:\` and `D:\` exclusions recorded on all samples.
Dev Drive reported a trusted developer volume with WdFilter attached.

The pilot baseline and NTFS jobs used `20261018.189.1`; its 1001-second Dev Drive
job used `20261013.183.4`. In round 2, round 4, and NTFS replacement 2, the baseline
used `20261018.189.1` while NTFS used `20261013.183.4`. Round 5 had the reverse
NTFS mismatch. These image, Visual Studio, PowerShell and Defender mismatches
remain disqualifying even though the SDK and actual linker matched.

| Eligible image stratum | Dev Drive pairs | Baseline / Dev Drive medians, s | NTFS pairs | Baseline / NTFS medians, s |
|---|---:|---:|---:|---:|
| 20261013.183.4 | 3 | 1187 / 990 | 2 | 1189.5 / 994 |
| 20261018.189.1 | 2 | 1181 / 955.5 | 2 | 1248 / 989.5 |

The post-collection RAM clarification matters: the pilot NTFS pair and Dev Drive
round 2, round 5, and replacement pairs were originally excluded by the
collector's exact-byte check. The pilot baseline/NTFS raw values were
17169428480/17169387520 bytes; those three Dev Drive baseline/treatment pairs
were 17169387520/17169428480 bytes. Each difference is 40960 bytes, within the
clarified 1 MiB limit and the same nominal 16 GiB class. Without that clarification
Dev Drive would have only two eligible pairs, not five. The numerical tolerance
was not preregistered, and the cause of the small raw-memory difference is unknown.

### Where the time changed

These are per-step medians over each treatment's eligible cohort. They must not
be summed to reconstruct the whole-job median.

| Step | Dev Drive cohort baseline / treatment, s | NTFS cohort baseline / treatment, s |
|---|---:|---:|
| Full preparation step | 44 / 50 | 43.5 / 46 |
| Checkout | 30 / 13 | 32 / 14 |
| RID-specific packages | 451 / 321 | 454 / 342.5 |
| Bundle payload | 294 / 257 | 294 / 257.5 |
| CLI packages | 277 / 252 | 279 / 250 |

The preparation helper itself took median 7.20/14.89 seconds for baseline/Dev
Drive and 7.88/13.68 seconds for baseline/NTFS. This includes inventory,
provisioning and treatment verification, not isolated format time. The full
preparation step also fetches the helper and starts PowerShell. Both costs are
already included in net whole-job savings.

### Exact-artifact consumer gate

**Passed on `windows-11-arm`: extraction/install, C# and TypeScript creation,
package addition, build/start, expected resource readiness, and stop.**
[The successful consumer job](https://github.com/radical/aspire/actions/runs/39542241841/job/119032382957)
took 341 seconds; it is a correctness check, not an additional performance sample.

The installed artifact was `cli-native-archives-win-arm64-devdrive-4` from
run `39536303120`, containing `aspire-cli-win-arm64-17.0.0-ci.zip`. Artifact source
was `012cda4fd7cff02842f47844a6daa9bc625cdaf9`; consumer harness source was
`a52b1a37e4fc00ee433d4ebdaff0d4715b355786`. The consumer ran Windows 11 ARM64,
image `20261013.183.4`, with SDK `11.0.100-rc.2.26504.105` installed,
Node `22.23.3`, and CLI `17.0.0-ci`. Apps ran outside the checkout at
`D:\aspire-benchmark\tmp\storage-benchmark-consumer` on the verified Dev Drive.
Published daily templates/dependencies resolved to `17.0.0-preview.1.26520.8`;
they are not same-commit managed packages.

The exact dispatch and validation entry point were:

```shell
gh workflow run build-cli-native-archives.yml --repo radical/aspire --ref windows-ci-performance \
  -f validationRunId=39536303120 -f validationMode=devdrive -f validationSample=4
```

```powershell
./eng/scripts/cli-starter-validation.ps1 -Channel daily -MaxStartupSeconds 180 -ResourceReadyTimeoutSeconds 300 -ValidationRoot $env:BENCHMARK_VALIDATION_ROOT
```

The helper exercised `aspire new`, `aspire add Aspire.Hosting.PostgreSQL`,
`aspire run --detach`, stop, `aspire start`, `aspire resources`, `aspire wait`,
and final stop. Successful runs include the AppHost build. TypeScript reported
`aspire run started in 19 seconds` and `aspire start started in 20.08 seconds`;
C# reported 10 seconds and 6.08 seconds respectively. Resource waits returned
exit code 0 for TypeScript `app`/`frontend` and C# `apiservice`. Detached run/start
returned valid AppHost process/path metadata, and cleanup completed.

Negative evidence is retained in
[consumer run 39541491282](https://github.com/radical/aspire/actions/runs/39541491282).
Its TypeScript lifecycle passed, but C# package addition failed with:

```text
Projects that use central package version management should not define the version on the PackageReference items but on the PackageVersion items: Aspire.Hosting.AppHost;Aspire.Hosting.Dotnet
```

That consumer fixture was inside the checkout and inherited
`Directory.Packages.props`, which enables central package management. The
successful external-root run used the same native artifact without changing
its generated package references. This is evidence for fixture isolation, not
a product fix or a storage-induced regression. Storage/provenance evidence and
per-template diagnostics are uploaded for the consumer run.

### Harness validation

The following focused command passed all 147 tests, including storage refusal,
treatment verification, failure-report preservation, unsuccessful-artifact-run
rejection, and trigger-map routing:

```shell
MSBUILDTERMINALLOGGER=false dotnet test \
  --project tests/Infrastructure.Tests/Infrastructure.Tests.csproj \
  --no-launch-profile -- \
  --filter-class '*.WindowsStorageBenchmarkTests' \
  --filter-class '*.TestTriggerMapTests' \
  --filter-not-trait 'quarantined=true' \
  --filter-not-trait 'outerloop=true'
```

The workflow and whitespace checks also passed:

```shell
actionlint -shellcheck= -pyflakes= .github/workflows/build-cli-native-archives.yml
git --no-pager diff --check
```

### Recommendation and limits

The candidate change is to place the Windows ARM64 native-build workspace,
workspace-relative packages/output, and `TEMP`/`TMP` on the verified unused
NVMe disk. Leave `RUNNER_TEMP` and the tool cache alone. The Dev Drive variant
has passed the declared timing rule; NTFS remains promising but inconclusive.
The exact-artifact consumer gate also passed. Filesystem choice is not settled
by these measurements.

The measured 17.9% is for equally instrumented benchmark jobs, not a promise of
the same saving in an uninstrumented production workflow. It measures the whole
path-placement bundle, not physical disk, ReFS, or Defender effects separately.
No explicit Defender policy or exclusions were changed; Dev Drive's default trust
and performance mode may change effective scanning.

`windows-11-arm` consumer compatibility is a separate completion gate, not a
timing comparison. Other Windows workloads, x64, Azure pipelines, and Windows ARM
tray UI have not been performance-validated here. In particular, do not format an
existing x64 D: workspace to apply this experiment. A production change needs its
own scoped approval; this harness does not enable it.
