# Running tests outside-of-repo

This provides support for running tests outside of the repo, for example on a helix agent.

- For this you need the source of the tests, and any dependencies.
- Instead of direct `ProjectReferences` to the various Aspire hosting, and component projects use `@(AspireProjectOrPackageReference)`.
    - These are converted to `ProjectReference` when `$(TestsRunningOutsideOfRepo) != true`.
    - But converted to `PackageReference` when `$(TestsRunningOutsideOfRepo) == true`.

- To allow building such test projects, the build is isolated and patched to build outside the
repo by adding appropriate `Directory.Build.{props,targets}`, and `Directory.Packages.props`
    - and using a custom `nuget.config` which resolves the Aspire packages from the locally built packages
    - and a `Directory.Packages.Versions.props` is generated with PackageVersions taken from the repo
    - This also adds properties named in `@(PropertyForHelixRun)` from the repo, like `$(DefaultTargetFramework)`.

## Adding new package versions

Add any new package versions used by test projects in `tests/Shared/RepoTesting/Directory.Packages.Helix.props`.
