# Instructions for GitHub and VisualStudio Copilot
### https://github.blog/changelog/2025-01-21-custom-repository-instructions-are-now-available-for-copilot-on-github-com-public-preview/

## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version C#, currently C# 13 features.
* Never change global.json unless explicitly asked to.
* Don't update files under `*/api/*.cs` (e.g. src/Aspire.Hosting/api/Aspire.Hosting.cs) as they are generated.

## Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.
* Place private class declarations at the bottom of the file.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Building

* Follow the instructions in the repo to build.
* If temporarily introducing warnings during a refactoring, you can add the flag `/p:TreatWarningsAsErrors=false` to your build command to prevent the build from failing. However before you finish your work, you must strive to fix any warnings as well.

In building and testing, never use `dotnet` without extension. Use `dotnet.sh` on Unix, `dotnet.cmd` on Windows.

### Testing

* We use xUnit SDK v3 with Microsoft.Testing.Platform (https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
* Do not emit "Act", "Arrange" or "Assert" comments.
* We do not use any mocking framework at the moment.
* Copy existing style in nearby files for test method names and capitalization.

## Running tests

(1) Build from the root with `build.sh`.
(2) If that produces errors, fix those errors and build again. Repeat until the build is successful.
(3) To then run tests, use a command similar to this `dotnet.sh test tests/Aspire.Seq.Tests/Aspire.Seq.Tests.csproj` (using the path to whatever projects are applicable to the change).

Note that tests for a project can be executed without first building from the root.

(4) To run just certain tests, it's important to include the filter after `--`, for example `dotnet.sh test tests/Aspire.Hosting.Testing.Tests/Aspire.Hosting.Testing.Tests.csproj --no-build --logger "console;verbosity=detailed" -- --filter "TestingBuilderHasAllPropertiesFromRealBuilder"`

## Quarantined tests

- Tests that are flaky and don't fail deterministically are marked with the `QuarantinedTest` attribute.
- Such tests are not run as part of the regular tests workflow (`tests.yml`).
    - Instead they are run in the `Outerloop` workflow (`tests-outerloop.yml`).
- A github issue url is used with the attribute

Example: `[QuarantinedTest("..issue url..")]`

## Snapshot Testing with Verify

* We use the Verify library (Verify.XunitV3) for snapshot testing in several test projects.
* Snapshot files are stored in `Snapshots` directories within test projects.
* When tests that use snapshot testing are updated and generate new output, the snapshots need to be accepted.
* Use `dotnet verify accept -y` to accept all pending snapshot changes after running tests.
* The verify tool is available globally as part of the copilot setup.

## Editing resources

The `*.Designer.cs` files are in the repo, but are intended to match same named `*.resx` files. If you add/remove/change resources in a resx, make the matching changes in the `*.Designer.cs` file that matches that resx.
