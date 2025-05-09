# Helix

The Helix CI job builds `tests/helix/send-to-helix-ci.proj`, which in turns builds the `Test` target on `tests/helix/send-to-helix-inner.proj`. This inner project uses the Helix SDK to construct `@(HelixWorkItem)`s, and send them to Helix to run.

- `tests/helix/send-to-helix-basictests.targets` - this prepares all the tests that don't need special preparation
- `tests/helix/send-to-helix-endtoend-tests.targets` - this is for tests that require a SDK+workload installed

## Install SDK+workload from artifacts

1. `.\build.cmd -pack`
2. `dotnet build tests\workloads.proj`

.. which results in `artifacts\bin\dotnet-tests` which has a SDK (version from `global.json`) with the `aspire` workload installed using packs from `artifacts/packages`.

## Controlling test runs on CI

- Tests on pull-requests run in GitHub Actions. Individual test projects can be opted-out by setting appropriate MSBuild properties:
  - `<RunOnGithubActionsWindows>false</RunOnGithubActionsWindows>` and/or
  - `<RunOnGithubActionsLinux>false</RunOnGithubActionsLinux>`.

- Tests for rolling builds run on the build machine and Helix.
Individual test projects can be opted-out by setting appropriate MSBuild properties:
  - `<RunOnAzdoCIWindows>false</RunOnAzdoCIWindows>` and/or
  - `<RunOnAzdoCILinux>false</RunOnAzdoCILinux>` and/or
  - `<RunOnAzdoHelixWindows>false</RunOnAzdoHelixWindows>` and/or
  - `<RunOnAzdoHelixLinux>false</RunOnAzdoHelixLinux>`.
