# Native CLI Packaging

This document explains how CI produces Aspire CLI native archives and dotnet-tool packages, and why official signed builds treat the native CLI archive as the canonical signed payload.

## Produced artifacts

The native CLI packaging flow produces two related artifact families:

1. Native CLI archives named `aspire-cli-<rid>-<version>.zip` or `aspire-cli-<rid>-<version>.tar.gz`.
2. Dotnet-tool NuGet packages:
   - RID-specific packages such as `Aspire.Cli.osx-arm64.<version>.nupkg`.
   - The `Aspire.Cli.<version>.nupkg` pointer package used by `dotnet tool install`.

The archive contains the native `aspire` executable plus runtime files that must stay beside it for direct installation and installer feeds such as Homebrew, WinGet, and Nix. The RID-specific tool package contains the same native CLI executable under the dotnet tool layout.

## GitHub Actions PR builds

The GitHub Actions test workflow builds native CLI archives per OS/RID and uploads them as `cli-native-archives-*` artifacts. Package-dependent test lanes download the RID-specific packages produced by the matching archive job.

These PR artifacts are useful for validation and dogfooding, but they are not the final official signed release artifacts.

### Experimental native-only build roots

The reusable native archive workflow accepts `nativeCliRootOnly` (default
`false`). It passes `SkipNativeCliBootstrapBuild=true` only to the CLI packaging
invocation. `eng/Build.props` retains the outer CLI restore, then removes the
non-RID CLI root immediately before Arcade's `ExecuteBuildTargets`. The RID
clipack project still restores and publishes the CLI and produces the archive,
dotnet-tool packages, npm packages, and native symbols.

Do not remove the CLI from the outer restore. On a clean workspace, the inner
MSBuild restore can create the NativeAOT package imports after the CLI project
evaluation has already been cached. Publishing with that evaluation can succeed
without running ILC and produce a managed apphost instead of the native CLI.
The existing tool-package size check rejects that result.

In the first-attempt macOS ARM64 binlog from
[run 39316168683](https://github.com/microsoft/aspire/actions/runs/39316168683/attempts/1),
the outer non-RID C# compile took 58.56 seconds and the RID publish C# compile
took 59.33 seconds, almost completely concurrently. There was one subsequent
220.73-second ILC execution. These are task intervals, not additive wall-time
savings. The experiment removes duplicate managed compilation, not native
compilation or any artifact family.

Keep this switch off until controlled upstream PR runs establish the benefit.
Compare at least three clean runs per setting on the same runner types and
source, recording median/min/max for CLI packaging and the whole native job.
Require exactly one CLI `CoreCompile` Csc execution plus ILC in the candidate
binlog, unchanged package inventories, and passing existing tool/npm, bundle,
and native smoke checks. Local unbundled packaging alone does not validate the
embedded runtime or the full six-RID matrix.

The dedicated `native-cli-root-experiment.yml` workflow runs only on upstream
draft PRs whose title contains `[ci-native-root-experiment]` and whose changes
match its limited path filter. It runs three clean macOS ARM64 jobs per setting
on the same PR source, at most two concurrently. The optional
`artifactNamePrefix` input isolates all four artifact families by setting and
repetition; its empty default preserves existing artifact names. Normal PR
CI does not enable the build optimization.

This opt-in adds six complete native jobs, approximately 140 macOS
runner-minutes at the observed baseline, in addition to normal PR CI. Title
edits and new pushes can trigger another trial; body-only edits do not.
The concurrency group cancels superseded experiments.

#### Upstream measurement: inconclusive, keep disabled

[Run 39344699152, attempt 1](https://github.com/microsoft/aspire/actions/runs/39344699152/attempts/1)
tested head `9aaefd64ac808d9a487f3b3f83bad9d50306dbb0` with three fresh
macOS ARM64 jobs per setting. All six native jobs passed. The global
two-job concurrency limit does not synchronize repetitions; candidate starts
trailed the corresponding controls by 15, 57, and 276 seconds, from REST job
timestamps. These are six independent jobs, not three synchronized pairs.

All six used runner image `macos-26-arm64` version `20261013.0424.1`,
macOS `26.6.2` build `25G83`, and .NET SDK `11.0.100-rc.2.26504.105`.
CLI binlogs identify `Xcode_26.6.app` and `MacOSX26.5.sdk` in the native
toolchain paths. No image or observed toolchain-path mismatch was found.
Hardware CPU counts and compiler patch versions were not measured;
`MSBuildNodeCount=3` is a build setting, not a hardware CPU measurement.

Durations below are seconds from attempt-specific REST step/job timestamps.
RID package and bundle work run before the experimental CLI packaging phase.

| Repetition | Setting | RID packages | Bundle payload | CLI packages | Whole native job |
| --- | --- | ---: | ---: | ---: | ---: |
| 1 | Control | 472 | 314 | 281 | 1140 |
| 1 | Candidate | 416 | 347 | 277 | 1108 |
| 2 | Control | 533 | 425 | 309 | 1330 |
| 2 | Candidate | 340 | 393 | 228 | 1015 |
| 3 | Control | 587 | 551 | 350 | 1578 |
| 3 | Candidate | 462 | 400 | 293 | 1226 |

| Metric | Control median (min-max), N=3 | Candidate median (min-max), N=3 |
| --- | ---: | ---: |
| CLI packages | 309 (281-350) | 277 (228-293) |
| Whole native job | 1330 (1140-1578) | 1108 (1015-1226) |

**No material wall-time improvement is established.** The CLI median is
32 seconds lower, but the within-setting ranges are 69 and 65 seconds.
The 222-second whole-job median difference cannot be attributed to the
switch: substantial differences occur before it is applied. For repetition
2, 225 of the 315 seconds of whole-job difference occur in RID/bundle steps.
Artifact-upload totals are 28/26, 23/20, and 34/22 seconds for
control/candidate, respectively; they are included in whole-job durations.

The mechanism and macOS output correctness were verified: every control
`BuildCli.binlog` has two main CLI `CoreCompile` Csc tasks, every candidate
has one, and all six retain one CLI ILC execution. Satellite-assembly Csc
tasks are excluded. All six pass tool/npm binary-to-archive checks, Dashboard
warning verification, bundle/tray verification, and TerminalHost smoke.
All eight uploaded package/archive filenames and their member inventories
match across all six jobs; CLI archive executables are executable Mach-O
files. Cross-job binary equality is not asserted. Other RIDs remain untested.

The six native jobs consumed 123.28 runner-minutes; the workflow elapsed
64 minutes 30 seconds including preparation, queueing, and finalization.
Normal PR CI is additional. An earlier invalid setup run,
[39342766702](https://github.com/microsoft/aspire/actions/runs/39342766702/attempts/1),
was stopped after both first jobs failed before CLI packaging. Its full-SHA
package version triggered NuGet `NU5123` at a 201-character installed path;
the normal eight-character PR suffix reduces that example to 169 characters.
Those failures are excluded from performance comparisons and the 123.28 figure.

Keep the switch default-off; do not roll it out or repeat this expensive
experiment without a new decision. Removing the activation title marker
prevents results-only pushes from launching another native trial.

## Internal signed builds

The internal Azure DevOps `build_sign_native` stage is responsible for producing signed native CLI artifacts. Windows and macOS native jobs run with code signing enabled so each OS/arch-specific native executable can be signed or notarized on the appropriate platform.

MicroBuild treats each CLI archive as a signing container. It extracts signable nested files such as `aspire.exe`, `aspire`, and native libraries to `artifacts/tmp/<Configuration>/ContainerSigning`, signs them, and writes the signed streams back into the archive. It does not update Arcade's original `PublishToDisk` staging directory.

Because of that, the archive is the canonical payload after signing. Consumers that need the final native CLI binary should read it from the signed archive, not from `artifacts/obj/.../output` or another intermediate staging directory.

## Dotnet-tool package signing constraints

The native `aspire` binary has to be signed/notarized on the OS/arch-specific native jobs. That means the macOS native job can produce a signed/notarized `aspire` binary and archive, but it cannot produce a signed `.nupkg` because NuGet package signing only happens on Windows.

If the macOS job produced a tool `.nupkg` from an unsigned binary and the main Windows job signed that package later, NuGet signing would fail because the `aspire` binary inside the package is not signed.

The tool packaging flow therefore extracts the signed `aspire` binary from the signed native archive and packs that into the RID-specific tool `.nupkg`. This produces an unsigned `.nupkg` that contains the signed `aspire` CLI; the `.nupkg` itself is signed later by the main Windows job.

Linux native jobs do not sign ELF binaries in `build_sign_native`. For Linux, extracting from the native archive gives the same unsigned ELF payload, and the resulting NuGet packages are signed later as packages by the main Windows build.

## Nix flake packaging

The root `flake.nix` packages the stable Aspire CLI from the versioned GitHub release archive URLs and hashes tracked in `eng/nix/versions.json`. It is a binary package, not a Nix source build of this repository. This keeps the Nix package aligned with the same canonical signed native archive consumed by the other installers.

For stable releases, `release-publish-nuget`'s `UpdateNixPackageJob` dispatches `.github/workflows/update-nix-cli-flake.yml`. The job reads the `aspire-cli-*.tar.gz.sha512` checksums from the signed source build's `BlobArtifacts` and passes them to the workflow as inputs, so the manifest is built from the build — not from the GitHub release. This lets it run while the release is still a **draft** (immutable releases don't serve draft assets from the public download URL). The workflow runs:

```sh
eng/nix/update-versions.sh --version <VERSION> --sha512 osx-arm64=<hex> --sha512 osx-x64=<hex> --sha512 linux-arm64=<hex> --sha512 linux-x64=<hex>
```

The workflow commits the Nix manifest change to the `update-baseline-<VERSION>` branch created by `release-github-tasks.yml`, then creates or updates the baseline PR. Merging that PR is the in-repo Nix "ship" step: it publishes the flake metadata that points at the versioned release download URLs (which go live when the release manager publishes the draft). When the `--sha512` inputs are supplied the updater uses them directly; run manually without them (against an already-published release) it downloads the official `.sha512` assets instead. Either way it writes Nix-compatible SRI hashes. Do not point the manifest at mutable `aka.ms` channel URLs; Nix fixed-output fetches require stable versioned URLs.

The Nix derivation writes `{"source":"nix"}` to `.aspire-install.json` next to the packaged native binary. `BundleService` treats this route as read-only and extracts the embedded bundle payload into the user-owned Aspire home instead of the Nix store.
