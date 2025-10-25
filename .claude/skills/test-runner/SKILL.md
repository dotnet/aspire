---
name: Aspire Test Runner
description: Run tests for the Aspire project correctly, excluding quarantined and outerloop tests, with proper build verification. Use when running tests, debugging test failures, or validating changes.
allowed-tools: Read, Grep, Glob, Bash
---

# Aspire Test Runner

## Purpose

This skill ensures tests are run correctly in the Aspire repository, following the project's specific requirements for test execution, including proper exclusion of quarantined and outerloop tests.

## When to Use

Invoke this skill when:
- Running tests for a specific project or test class
- Debugging test failures
- Validating code changes
- Verifying builds after modifications
- User requests to "run tests" or "test my changes"

## Critical Requirements

**ALWAYS exclude quarantined and outerloop tests** in automated environments:
- Quarantined tests are marked with `[QuarantinedTest]` and are known to be flaky
- Outerloop tests are marked with `[OuterloopTest]` and are long-running or resource-intensive
- These tests run separately in dedicated CI workflows

## Instructions

### Step 1: Identify Test Target

1. If the user specifies a test project, use that path
2. If the user mentions specific test methods or classes, identify the containing test project
3. Use Glob to find test projects if needed:
   ```bash
   find tests -name "*.Tests.csproj" -type f
   ```

### Step 2: Build Verification (if needed)

**Important**: Only build if:
- There have been code changes since the last build
- The user hasn't just run a successful build
- You're unsure if the code is up to date

If building is needed:
```bash
# Quick build with skip native (saves 1-2 minutes)
./build.sh --build /p:SkipNativeBuild=true
```

### Step 3: Run Tests with Proper Filters

**Default test run** (all tests in a project):
```bash
dotnet test tests/{ProjectName}.Tests/{ProjectName}.Tests.csproj --no-build -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

**Specific test method**:
```bash
dotnet test tests/{ProjectName}.Tests/{ProjectName}.Tests.csproj --no-build -- --filter-method "*.{MethodName}" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

**Specific test class**:
```bash
dotnet test tests/{ProjectName}.Tests/{ProjectName}.Tests.csproj --no-build -- --filter-class "*.{ClassName}" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

**Multiple test methods**:
```bash
dotnet test tests/{ProjectName}.Tests/{ProjectName}.Tests.csproj --no-build -- --filter-method "*.Method1" --filter-method "*.Method2" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

### Step 4: Handle Test Results

**If tests pass**:
- Report success to the user
- Mention the number of tests that passed

**If tests fail**:
1. Analyze the failure output
2. Identify which tests failed and why
3. Check if failures are related to recent code changes
4. Suggest fixes or next steps
5. Do NOT mark the task as complete if tests are failing

**If snapshot tests fail**:
1. Tests using Verify library will show snapshot differences
2. After verifying the new output is correct, run:
   ```bash
   dotnet verify accept -y
   ```
3. Re-run the tests to confirm they pass

### Step 5: Report Results

Provide a clear summary:
- Number of tests run
- Pass/fail status
- Any warnings or issues
- Next steps if failures occurred

## Examples

### Example 1: Run all tests for a specific project

User: "Run tests for Aspire.Hosting.Testing"

Actions:
1. Identify test project: `tests/Aspire.Hosting.Testing.Tests/Aspire.Hosting.Testing.Tests.csproj`
2. Run with proper filters:
   ```bash
   dotnet test tests/Aspire.Hosting.Testing.Tests/Aspire.Hosting.Testing.Tests.csproj --no-build -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
   ```
3. Report results

### Example 2: Run a specific test method

User: "Run the TestingBuilderHasAllPropertiesFromRealBuilder test"

Actions:
1. Find the test using Grep:
   ```bash
   grep -r "TestingBuilderHasAllPropertiesFromRealBuilder" tests/
   ```
2. Identify project: `Aspire.Hosting.Testing.Tests`
3. Run specific test:
   ```bash
   dotnet test tests/Aspire.Hosting.Testing.Tests/Aspire.Hosting.Testing.Tests.csproj --no-build -- --filter-method "*.TestingBuilderHasAllPropertiesFromRealBuilder" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
   ```
4. Report results

### Example 3: Run tests after making changes

User: "I just modified the hosting code, run tests to verify"

Actions:
1. Identify affected test projects (e.g., `Aspire.Hosting.Tests`)
2. Build first since code was modified:
   ```bash
   ./build.sh --build /p:SkipNativeBuild=true
   ```
3. Run tests:
   ```bash
   dotnet test tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
   ```
   Note: No `--no-build` flag since we need to pick up the changes
4. Report results

## Common Pitfalls to Avoid

1. **Never omit the quarantine and outerloop filters** - this will run flaky tests
2. **Don't use `--no-build`** if code has changed - the changes won't be tested
3. **Don't run the full test suite** - it takes 30+ minutes, use targeted testing
4. **Don't ignore snapshot test failures** - they indicate output changes that need review
5. **Don't forget the `--` separator** before filter arguments

## Valid Test Filter Switches

- `--filter-class` / `--filter-not-class`: Filter by class name
- `--filter-method` / `--filter-not-method`: Filter by method name
- `--filter-namespace` / `--filter-not-namespace`: Filter by namespace
- `--filter-trait` / `--filter-not-trait`: Filter by trait (category, platform, etc.)

Switches can be repeated to filter multiple values. Class and method filters expect fully qualified names, unless using a prefix like `*.ClassName`.
