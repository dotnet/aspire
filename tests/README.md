# Helix

The helix CI job builds `tests/helix/send-to-helix-ci.proj`, which in turns builds the `Test` target on `tests/helix/send-to-helix-inner.proj`. This inner project uses the Helix SDK to construct `@(HelixWorkItem)`s, and send them to helix to run.

- `tests/helix/send-to-helix-basictests.targets` - this prepares all the tests that don't need special preparation
- `tests/helix/send-to-helix-endtoend-tests.targets` - this is for tests that require a sdk+workload installed

## Install sdk+workload from artifacts

1. `.\build.cmd -pack`
2. `dotnet build tests\workloads.proj`

.. which results in `artifacts\bin\dotnet-tests` which has a sdk (version from `global.json`) with the `aspire` workload installed using packs from `artifacts/packages`.
