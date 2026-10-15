# CI Pipeline Optimizations

This document explains the CI pipeline optimizations in the current test workflow, why the jobs are structured the way they are, and what to verify when changing that structure.

## Goals

The test pipeline is optimized for three things:

1. **Short time to first useful test signal**
2. **Short total wall-clock time**
3. **Clear dependency boundaries between shared artifacts and test jobs**

Those goals are the reason the workflow prefers several smaller, dependency-aware jobs over a single large serialized build-and-test lane.

## Current structure

The main test workflow in `.github/workflows/tests.yml` is organized into these phases:

1. `setup_for_tests`
   - Enumerates tests and produces the expanded `all_tests` matrix
   - Splits the matrix into dependency buckets with `eng/scripts/split-test-matrix-by-deps.ps1`

2. `build_packages`
   - Produces the shared Aspire package feed used by package-dependent tests
   - Uses `-p:SkipBundleDeps=true` so it does not duplicate the RID-specific DCP/Dashboard work handled by the CLI archive jobs

3. `build_cli_archive_linux`, `build_cli_archive_windows`, `build_cli_archive_macos`
   - Build native CLI archives per RID
   - Prepare and package RID-specific bundle components with the shared `eng/Bundle.proj` phases
   - Upload `built-nugets-for-{rid}` artifacts for downstream tests
   - Upload `cli-native-archives-{rid}` artifacts for archive consumers such as polyglot validation

4. Test execution lanes
   - `tests_no_nugets`
   - `tests_requires_nugets_linux`
   - `tests_requires_nugets_windows`
   - `tests_requires_nugets_macos`
   - `tests_requires_cli_archive`
   - `polyglot_validation`
   - `extension_tests_win`

5. `results`
   - Aggregates the final workflow status after all required lanes finish

## Why the jobs are split this way

### 1. Package build and CLI archive build run in parallel

`build_packages` and the CLI archive jobs intentionally run side by side.

Without that split, CLI archive creation would sit behind the shared package build even though most of the archive work is independent. The archive workflow now builds the RID-specific DCP and Dashboard packages it needs locally, so there is no reason to serialize it behind `build_packages`.

This reduces time-to-artifact for:

- CLI archive consumers
- polyglot validation
- any tests that need RID-specific DCP or Dashboard packages

### 2. CLI archive builds are per OS

The workflow uses one CLI archive job per platform instead of one combined matrix dependency.

That allows downstream jobs to wait only for the archive that matches their runner and RID:

- Linux tests wait for `build_cli_archive_linux`
- Windows tests wait for `build_cli_archive_windows`
- macOS tests wait for `build_cli_archive_macos`

If these stayed grouped behind one logical dependency, the slowest platform would delay every dependent test lane.

### 3. `requires_nugets` tests are split by OS

Tests that need built packages also need the correct RID-specific DCP/Dashboard packages. Because those packages are produced by the CLI archive job for the same OS, the workflow splits the `requires_nugets` bucket into Linux, Windows, and macOS lanes.

That keeps the dependency graph aligned with artifact production:

- the Linux lane does not wait for Windows or macOS artifacts
- the Windows lane does not wait for Linux or macOS artifacts
- the macOS lane does not wait for Linux or Windows artifacts

### 4. `tests_requires_cli_archive` depends only on the Linux CLI archive lane

The current `requires_cli_archive` consumers run on Linux, so the workflow depends on `build_cli_archive_linux` instead of every CLI archive job. It still waits for `build_packages`, but among the CLI archive jobs it only needs the Linux lane.

This is intentional. Requiring all CLI archive builds would add wait time with no benefit for the current consumers. If future archive-dependent tests are added for Windows or macOS, this dependency should be revisited.

### 5. `tests_no_nugets` starts as soon as possible

The `no_nugets` buckets are the fastest way to get broad CI feedback because they do not depend on package or archive production. Starting them immediately reduces the time before developers see failures in unit tests and other self-contained lanes.

### 6. `results` stays centralized

Even though execution is more parallel, the workflow still uses a single `results` job so GitHub reports one final, easy-to-understand outcome for the overall test pipeline.

## Additional optimizations in the current workflow

### 1. Linux critical-path builds use the 8-core runner

`build_cli_archive_linux` and `build_packages` both run on `8-core-ubuntu-latest`. The Linux CLI archive is on the critical path for linux-only consumers such as `tests_requires_cli_archive` and `polyglot_validation`, and `build_packages` is shared by every package-dependent lane.

That makes Linux the best place to spend extra build capacity: finishing the shared package build and Linux archive earlier directly reduces the time before downstream validation lanes can start.

### 2. `build_packages` skips RID-specific bundle dependencies

`build_packages` uses `SkipBundleDeps=true`, while the CLI archive jobs prepare
and package their RID-specific components through `eng/Bundle.proj`.

Those two settings work together:

- the shared package build avoids work that would otherwise be duplicated
- each CLI archive job becomes responsible for the RID-specific dependencies it actually needs
- package build and archive build can proceed in parallel without racing to produce the same artifacts

### 3. The VS Code extension is built in its dedicated test lane

The extension work now lives in `extension_tests_win` instead of also being folded into `build_packages`.

That keeps `build_packages` focused on the shared package feed, and because `extension_tests_win` has no dependency on the package or archive jobs it can run independently instead of lengthening those critical paths.

## Supporting workflow details

### `build-packages.yml`

`build_packages` uses:

```bash
./build.sh -restore -build -ci -pack -bl -p:InstallBrowsersForPlaywright=false -p:SkipTestProjects=true -p:SkipPlaygroundProjects=true -p:SkipBundleDeps=true
```

`SkipBundleDeps=true` matters because the RID-specific bundle dependencies are produced by the per-RID bundle package jobs, not by the shared package build.

### Native package handoff

`tests.yml` calls `build-native-bundle-packages.yml` separately for Linux x64,
Windows x64, and macOS ARM64, the RIDs with package-only test buckets. Each
package job:

1. Runs `PublishBundleComponents` to publish Managed, Dashboard, TerminalHost,
   and the Windows/macOS tray once for the target RID.
2. Verifies Dashboard warnings from that publish's `PublishNativeDashboard.log`.
3. Runs `PackageBundlePayload` with `PackDcpPackage=true` to produce the
   Dashboard/DCP packages and bundle archive without publishing components again.
4. Smoke-tests TerminalHost on matching host architectures.
5. Uploads `platform-nugets-for-{rid}`, `native-dashboard-{rid}`, and the
   compressed `native-bundle-payload-{rid}` handoff.

Package-only test buckets and matching native Dashboard validation depend on
these package jobs, not final CLI jobs. A reusable-workflow call is a single dependency barrier:
uploading packages earlier within the old archive call would not release tests.

The three matching `build-cli-native-archives.yml` calls consume their RID's
handoff with `bundlePrepared: true`. Each downloads the payload without extracting/recreating it,
preserving embedded bytes and executable modes, and builds the CLI with the
existing version/channel inputs. Tool/npm verification and Windows tray smoke
remain after CLI compilation. The established final `built-nugets-for-{rid}` and
`cli-native-archives-{rid}` artifacts remain complete for dogfood, tool installs,
Starter, installers, CLI E2E, polyglot, and extension consumers.

The other three RIDs and direct callers, including specialized test workflows,
omit `bundlePrepared` and retain their original single-worker publish/package/CLI
path. They incur no handoff transfer or additional allocation. Missing handoffs
on the split path fail the CLI job rather than silently republish components.
Official Azure DevOps orchestration is unchanged.

The split adds a runner allocation and compressed-payload transfer for each of
the three split RIDs.
Platform packages also transfer to the CLI job to preserve the existing final
NuGet artifact contract. New binary artifact uploads disable redundant ZIP
compression. Measure queue/transfer time and end-to-end CI duration, particularly
on macOS; producer head-start alone is not proof of wall-time improvement.

### Shared bundle phases and the official signing seam

`eng/Bundle.proj` owns both phases, used by GitHub and
`eng/pipelines/templates/build_sign_native.yml`:

```bash
./dotnet.sh msbuild eng/Bundle.proj /t:PublishBundleComponents /p:Configuration=Release /p:TargetRid=linux-x64 /p:ContinuousIntegrationBuild=true
# Official builds sign the prepared components here.
./dotnet.sh msbuild eng/Bundle.proj /t:PackageBundlePayload /p:Configuration=Release /p:TargetRid=linux-x64 /p:ContinuousIntegrationBuild=true /p:BundleVersion=ci-bundlepayload
```

Both phases require exactly one supported RID and enforce Release in CI.
Child processes explicitly receive configuration, RID, CI identity, version
suffix, official build ID, release-version policy, and CLI channel. Orchestration
switches are not propagated into component builds.

On cold CI agents without `NUGET_PACKAGES`, child restores use the repository
`.packages` cache that Arcade selects for package-target imports. Otherwise,
NuGet's user-cache default can leave build targets such as Grpc.Tools unimported
even after restore succeeds. CreateLayout receives that same effective cache
through `NUGET_PACKAGES`, so DCP discovery does not depend on a warm user cache.
Caller-supplied `NUGET_PACKAGES` remains respected.

Packaging validates the expected RID/configuration publish files before doing
work. Missing files fail rather than trigger a fallback publish.
Dashboard packages, standalone ZIPs, and the bundle consume the same prepared
Dashboard, excluding symbols. The default standalone `Build` retains its skip
options and creates the bundle without adding Dashboard/DCP packages.

Official signing remains between phases: macOS entitlements, symbol cleanup,
Arcade signing, whole-app tray signing/notarization, signed tray restoration,
and executable permissions are still pipeline-owned. `PackageBundlePayload`
has no dependency on publishing, so these changes survive packaging.
Official DCP packages remain managed-build-owned; only GitHub sets
`PackDcpPackage=true`. Source indexing and canonical CLI archive/tool/npm
packaging are unchanged.

Matching build phases does not imply byte equality between unsigned CI and
signed official outputs, or between different release identities/toolchains.
The previous GitHub second Dashboard publish could lose CI/version properties;
the shared preparation now keeps one explicit build identity for all surfaces.

### Native CLI packaging

The CLI archive jobs also produce RID-specific native CLI artifacts used by tests and downstream packaging. For details about native CLI archive signing, dotnet-tool package construction, and why signed builds consume the signed archive payload, see [Native CLI Packaging](native-cli-packaging.md).

### `run-tests.yml`

When a test lane sets `requiresNugets: true`, `run-tests.yml`:

1. Downloads the shared `built-nugets` artifact
2. Computes the lane RID from `runner.os`
3. Downloads `platform-nugets-for-{rid}` when the caller sets
   `usePlatformPackages: true`, or the complete `built-nugets-for-{rid}` by default
4. Merges those RID-specific packages into the local package feed before running tests

That is why the dependency graph must preserve the RID relationship between each
test lane and its package/archive producer. Final results still depend directly
on all three early package producers and all six CLI producers, so a late CLI failure
cannot be hidden by package tests that already passed.

## What to validate when changing this area

When changing the CI structure, verify:

1. **Artifact production still matches artifact consumption**
   - `build_packages` still publishes the shared package feed
   - each CLI archive lane still publishes the correct `built-nugets-for-{rid}` artifact

2. **Each test lane waits only for the work it actually needs**
   - avoid adding cross-OS dependencies unless a consumer truly needs them

3. **Linux-only consumers stay on the shortest path**
   - especially `tests_requires_cli_archive` and `polyglot_validation`

4. **Matrix bucket sizes stay under GitHub's limits**
   - `split-test-matrix-by-deps.ps1` enforces the 256-job hard limit

5. **Documentation stays in sync**
   - update this file and `docs/ci/TestingOnCI.md` whenever the dependency graph changes

## Source-index build

The standalone daily pipeline in `eng/pipelines/azure-pipelines-source-index.yml`
builds the full repository for source indexing, including managed Dashboard,
CLI, tests and playground projects. Its build command disables two operations
that do not contribute managed source coverage:

- `CanPublishDashboardNativeAot=false` disables native Dashboard publishing,
  NuGet packaging and ZIP staging in `eng/dashboardpack/Common.projitems`.
  The managed Dashboard project remains selected by `eng/Build.props`.
- `InstallBrowsersForPlaywright=false` disables browser provisioning in
  `tests/Shared/Playwright/Playwright.targets`, not compilation of the tests.

These are global MSBuild properties passed only by the source-index pipeline;
other builds retain their defaults. Native CLI work remains enabled.
`SkipNativeBuild` is deliberately not used because it also removes native CLI
projects. `SkipDashboardPublish` is not a replacement: it repackages existing
native outputs and fails when those outputs are absent.

When changing these controls, compare the before/after compiler source inputs
in the build binlogs and the solution generated by the pipeline's `BinLogToSln`
step. A successful build alone does not establish indexing coverage. Preserve
the full managed project selection, including tests and playground, and verify
source-index conversion/upload and CodeQL finalization on an active-scan run.
The job timeout and CodeQL configuration are unchanged.
