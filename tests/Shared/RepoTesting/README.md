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
        - Aspire package versions are dynamically computed by calling `GetPackageName` on all `src/**/*.csproj`
          projects. This correctly handles packages with `SuppressFinalPackageVersion=true` (which keep a
          prerelease suffix even during version stabilization) as well as PR version suffixes.
    - This also adds properties named in `@(PropertyForHelixRun)` from the repo, like `$(DefaultTargetFramework)`.

## Adding new Aspire package versions

New Aspire packages added under `src/` are automatically discovered and included in
`Directory.Packages.Versions.props` via the `GetPackageName` MSBuild target. No manual updates
to `Directory.Packages.Helix.props` are required for Aspire packages.

## Adding non-Aspire package versions

Add any non-Aspire package versions needed by test projects (e.g. test framework adapters) in
`tests/Shared/RepoTesting/Directory.Packages.Helix.props`.
