# Instructions for GitHub and VisualStudio Copilot
### https://github.blog/changelog/2025-01-21-custom-repository-instructions-are-now-available-for-copilot-on-github-com-public-preview/

## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version C#, currently C# 13 features.
* Never change global.json unless explicitly asked to.

## Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Testing

* We use xUnit SDK v3 with Microsoft.Testing.Platform (https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
* Do not emit "Act", "Arrange" or "Assert" comments.
* We do not use any mocking framework at the moment.
* Copy existing style in nearby files for test method names and capitalization.
* Flaky tests can be quarantined with the `QuarantinedTest` attribute, with a corresponding github issue. These tests are then skipped in the regular tests run. Instead they are run in the Outerloop tests workflow.

## Running tests

(1) Build from the root with `build.sh`.
(2) If that produces errors, fix those errors and build again. Repeat until the build is successful.
(3) To then run tests, use a command similar to this `dotnet test tests/Aspire.Seq.Tests/Aspire.Seq.Tests.csproj` (using the path to whatever projects are applicable to the change).
