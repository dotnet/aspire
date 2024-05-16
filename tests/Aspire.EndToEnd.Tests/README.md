# Aspire.EndToEnd.Tests

## TL;DR or How do I use this?

### Steps to prepare for `outside-of-repo` runs

1. [Install the sdk+workload](../Aspire.Workload.Tests/README.md#install-the-sdkworkload)

### Using it from VS

- For switching to `outside-of-repo` add `<TestsRunningOutsideOfRepo>true</TestsRunningOutsideOfRepo>` to `tests/Aspire.EndToEnd.Tests/Directory.Build.props` *before* any imports.
    - tests cannot be run at this point as they will fail complaining about `artifacts/bin/dotnet-latest` being missing
    - Install the sdk+workload following the steps above
    - Run/debug the tests normally now, and they will be using the sdk
    - Also note that in this case the testproject is run from the bindir for `Aspire.EndToEnd.Tests`, so a path like `artifacts/bin/Aspire.EndToEnd.Tests/Debug/net8.0/testassets/testproject/`

### Using it from command line

- When running the tests you can either:
    - set `<TestsRunningOutsideOfRepo>true</TestsRunningOutsideOfRepo>` to `tests/Aspire.EndToEnd.Tests/Directory.props` before any imports
    - or set the environment variable `TestsRunningOutsideOfRepo=true`

## (details) What is the goal here?

1. We want to run some EndToEnd tests on CI, which can `dotnet run` an aspire project,
and allow individual tests to interact with the services.
This requires:

    - Ability to build, and run an aspire project - IOW, a sdk with the `aspire` workload installed.
    - `docker`

2. Also, allow using `TestProject.*` in `tests/testproject`, in two modes:
- `in-repo` test run which directly reference aspire projects, and repo targets
- `outside-of-repo` test runs which uses a SDK+workload based on local build output

## Solution:

### SDK+workload

[Sdk+workload](../Aspire.Workload.Tests/README.md#solution-sdkworkload)

### TestProject

- This can switch between the two test run modes using the msbuild property `$(TestsRunningOutsideOfRepo)`
    - when running `in-repo` the test project directly references hosting targets, and aspire projects via `ProjectReference`
    - when running `outside-of-repo` the `ProjectReferences` and imports are replaced with `PackageReferences` to the Aspire nugets
- Default is to run `in-repo`

### Helix

- The tests are built on the build machine
- The testproject, and the sdk+workload is sent to helix
  - where the tests run using `dotnet` from the sdk+workload path
- Since `docker` is needed to helix, this is enabled only for `Linux` in this PR. Blocked on https://github.com/dotnet/dnceng/issues/2067 for windows support.
