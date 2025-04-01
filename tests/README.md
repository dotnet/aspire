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
  - `<SupportsGithubActionsWindows>false</SupportsGithubActionsWindows>` and/or
  - `<SupportsGithubActionsLinux>false</SupportsGithubActionsLinux>`.

- Tests for rolling builds run on the build machine and Helix.
Individual test projects can be opted-out by setting appropriate MSBuild properties:
  - `<SupportsAzdo>false</SupportsAzdo>` and/or
  - `<SupportsHelixWindows>false</SupportsHelixWindows>` and/or
  - `<SupportsHelixLinux>false</SupportsHelixLinux>`.
