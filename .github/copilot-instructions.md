# Instructions for GitHub and VisualStudio Copilot
### https://github.blog/changelog/2025-01-21-custom-repository-instructions-are-now-available-for-copilot-on-github-com-public-preview/

## Repository Overview

**Aspire** provides tools, templates, and packages for building observable, production-ready distributed applications. At its core is an app model that defines services, resources, and connections in a code-first approach.

### Key Components
- **Aspire.Hosting**: Application host orchestration and resource management
- **Aspire.Dashboard**: Web-based dashboard for monitoring and debugging
- **Service Discovery**: Infrastructure for service-to-service communication
- **Integrations**: 40+ packages for databases (SQL Server, PostgreSQL, Redis, MongoDB), message queues (RabbitMQ, Kafka), cloud services (Azure), and more
- **CLI Tools**: Command-line interface for project creation and management
- **Project Templates**: Starter templates for new Aspire applications

### Technology Stack
- .NET 10.0 RC (specified in global.json)
- C# 13 preview features
- xUnit SDK v3 with Microsoft.Testing.Platform for testing
- Microsoft.DotNet.Arcade.Sdk for build infrastructure
- Native AOT compilation for CLI tools
- Multi-platform support (Windows, Linux, macOS, containers)

## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version C#, currently C# 13 features.
* Never change global.json unless explicitly asked to.
* Never change package.json or package-lock.json files unless explicitly asked to.
* Never change NuGet.config files unless explicitly asked to.
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

**Always run restore first to set up the local SDK.** Run `./restore.sh` (Linux/macOS) or `./restore.cmd` (Windows) first to install the local SDK. After restore, you can use standard `dotnet` commands, which will automatically use the local SDK when available due to the paths configuration in global.json.

#### Prerequisites
1. **Restore First**: Always run `./restore.sh` (Linux/macOS) or `./restore.cmd` (Windows) to set up the local .NET SDK (~30 seconds)

#### Build Commands
- **Full Build**: `./build.sh` (Linux/macOS) or `./build.cmd` (Windows) - defaults to restore + build (~3-5 minutes)
- **Build Only**: `./build.sh --build` (assumes restore already done)
- **Skip Native Build**: Add `/p:SkipNativeBuild=true` to avoid slow native AOT compilation (~1-2 minutes saved)
- **Clean Build**: `./build.sh --rebuild`
- **Package Generation**: `./build.sh --pack` to create NuGet packages

#### Build Troubleshooting
- If temporarily introducing warnings during refactoring, add `/p:TreatWarningsAsErrors=false` to prevent build failure
- **Important**: All warnings should be addressed before committing any final changes
- Template engine warnings about "Missing generatorVersions" are expected and not errors
- If build fails with SDK errors, run `./restore.sh` again to ensure correct .NET 10 RC is installed
- Build artifacts go to `./artifacts/` directory

#### Visual Studio / VS Code Setup
- **VS Code**: Run `./build.sh` first, then use `./start-code.sh` to launch with correct environment
- **Visual Studio**: Run `./build.cmd` first, then use `./startvs.cmd` to launch with local SDK environment

### Testing

* We use xUnit SDK v3 with Microsoft.Testing.Platform (https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
* Do not emit "Act", "Arrange" or "Assert" comments.
* We do not use any mocking framework at the moment.
* Copy existing style in nearby files for test method names and capitalization.
* Do not leave newly-added tests commented out. All added tests should be building and passing.
* Do not use Directory.SetCurrentDirectory in tests as it can cause side effects when tests execute concurrently.

## Running tests

(1) Build from the root with `./build.sh` (~3-5 minutes).
(2) If that produces errors, fix those errors and build again. Repeat until the build is successful.
(3) To run tests for a specific project: `dotnet test tests/ProjectName.Tests/ProjectName.Tests.csproj --no-build -- --filter-not-trait "quarantined=true"`

Note that tests for a project can be executed without first building from the root.

(4) To run specific tests, include the filter after `--`:
```bash
dotnet test tests/Aspire.Hosting.Testing.Tests/Aspire.Hosting.Testing.Tests.csproj -- --filter-method "TestingBuilderHasAllPropertiesFromRealBuilder" --filter-not-trait "quarantined=true"
```

**Important**: Avoid passing `--no-build` unless you have just built in the same session and there have been no code changes since. In automation or while iterating on code, omit `--no-build` so changes are compiled and picked up by the test run.

### CRITICAL: Excluding Quarantined Tests

When running tests in automated environments (including Copilot agent), **always exclude quarantined tests** to avoid false negatives:

```bash
# Correct - excludes quarantined tests (use this in automation)
dotnet test tests/Project.Tests/Project.Tests.csproj -- --filter-not-trait "quarantined=true"

# For specific test filters, combine with quarantine exclusion
dotnet test tests/Project.Tests/Project.Tests.csproj -- --filter-method "TestName" --filter-not-trait "quarantined=true"
```

Never run all tests without the quarantine filter in automated environments, as this will include flaky tests that are known to fail intermittently.

Valid test filter switches include: --filter-class, --filter-not-class, --filter-method, --filter-not-method, --filter-namespace, --filter-not-namespace, --filter-not-trait, --filter-trait

### Test Verification Commands
- **Single Test Project**: Typical runtime ~10-60 seconds per test project
- **Full Test Suite**: Can take 30+ minutes, use targeted testing instead

## Project Layout and Architecture

### Directory Structure
- **`/src`**: Main source code for all Aspire packages
  - `Aspire.Hosting/`: Core hosting and orchestration infrastructure
  - `Aspire.Dashboard/`: Web dashboard UI (Blazor application)
  - `Components/`: 40+ integration packages for databases, messaging, cloud services
  - `Aspire.Cli/`: Command-line interface tools
  - `Microsoft.Extensions.ServiceDiscovery/`: Service discovery infrastructure
- **`/tests`**: Comprehensive test suites mirroring src structure
- **`/playground`**: Sample applications including TestShop for verification
- **`/docs`**: Documentation including contributing guides and area ownership
- **`/eng`**: Build scripts, tools, and engineering infrastructure
- **`/.github`**: CI/CD workflows, issue templates, and GitHub automation
- **`/extension`**: VS Code extension source code

### Key Configuration Files
- **`global.json`**: Pins .NET SDK version (10.0.100-rc.1.25411.109) - never modify without explicit request
- **`.editorconfig`**: Code formatting rules, null annotations, diagnostic configurations
- **`Directory.Build.props`**: Shared MSBuild properties across all projects
- **`Directory.Packages.props`**: Centralized package version management
- **`Aspire.slnx`**: Main solution file (XML-based solution format)

### Continuous Integration
- **`tests.yml`**: Main test workflow running across Windows/Linux/macOS
- **`tests-outerloop.yml`**: Runs quarantined tests separately every 2 hours
- **`ci.yml`**: Main CI workflow triggered on PRs and pushes to main/release branches
- **Build validation**: Includes package generation, API compatibility checks, template validation

### Dependencies and Hidden Requirements
- **Local .NET SDK**: Automatically uses local SDK when available after running restore due to paths configuration in global.json
- **Package References**: Centrally managed via Directory.Packages.props
- **API Surface**: Public APIs tracked in `src/*/api/*.cs` files (auto-generated, don't edit)

### Common Validation Steps
1. **Build Verification**: `./build.sh` should complete without errors
2. **Package Generation**: `./build.sh --pack` verifies all packages can be created
3. **Specific Tests**: Target individual test projects related to your changes

## Quarantined tests

- Tests that are flaky and don't fail deterministically are marked with the `QuarantinedTest` attribute.
- Such tests are not run as part of the regular tests workflow (`tests.yml`).
    - Instead they are run in the `Outerloop` workflow (`tests-outerloop.yml`).
- A github issue url is used with the attribute

Example: `[QuarantinedTest("..issue url..")]`

- To quarantine or unquarantine tests, use the tool in `tools/QuarantineTools/QuarantineTools.csproj`.

## Snapshot Testing with Verify

* We use the Verify library (Verify.XunitV3) for snapshot testing in several test projects.
* Snapshot files are stored in `Snapshots` directories within test projects.
* When tests that use snapshot testing are updated and generate new output, the snapshots need to be accepted.
* Use `dotnet verify accept -y` to accept all pending snapshot changes after running tests.
* The verify tool is available globally as part of the copilot setup.

## Editing resources

The `*.Designer.cs` files are in the repo, but are intended to match same named `*.resx` files. If you add/remove/change resources in a resx, make the matching changes in the `*.Designer.cs` file that matches that resx.

## Markdown files

* Markdown files should not have multiple consecutive blank lines.
* Code blocks should be formatted with triple backticks (```) and include the language identifier for syntax highlighting.
* JSON code blocks should be indented properly.

## Trust These Instructions

These instructions are comprehensive and tested. Only search for additional information if:
1. The instructions appear outdated or incorrect
2. You encounter specific errors not covered here
3. You need details about new features not yet documented

For most development tasks, following these instructions should be sufficient to build, test, and validate changes successfully.
