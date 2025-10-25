---
name: Quarantine Test
description: Quarantine flaky or failing tests by adding the QuarantinedTest attribute using the QuarantineTools. Use when tests are failing intermittently or need to be excluded from regular test runs.
allowed-tools: Read, Grep, Bash
---

# Quarantine Test

## Purpose

This skill quarantines flaky or failing tests by using the QuarantineTools utility to add the `[QuarantinedTest]` attribute. Quarantined tests are excluded from regular test runs and instead run in a separate quarantine workflow.

## When to Use

Invoke this skill when:
- A test is failing intermittently (flaky)
- A test doesn't fail deterministically
- User requests to "quarantine a test"
- A test needs to be temporarily excluded from CI while being fixed
- You need to mark a test as quarantined with an associated GitHub issue

## Important Context

- Quarantined tests are marked with `[QuarantinedTest("issue-url")]` attribute
- They are NOT run in the regular `tests.yml` workflow
- They ARE run in the separate `tests-quarantine.yml` workflow every 6 hours
- A GitHub issue URL is REQUIRED when quarantining tests
- The QuarantineTools utility handles adding the attribute and managing using directives automatically

## Instructions

### Step 1: Identify the Test to Quarantine

1. Get the fully-qualified test method name in format: `Namespace.ClassName.TestMethodName`
2. If user provides partial name, use Grep to find the complete qualified name:
   ```bash
   grep -rn "void TestMethodName" tests/
   ```
3. Extract the namespace and class name from the file to build the fully-qualified name

Example: `Aspire.Hosting.Tests.DistributedApplicationTests.TestMethodName`

### Step 2: Get or Create GitHub Issue

1. Check if the user provided a GitHub issue URL
2. If not, ask: "What is the GitHub issue URL for tracking this flaky test?"
3. The URL must be a valid http/https URL (e.g., `https://github.com/dotnet/aspire/issues/1234`)
4. If no issue exists, suggest creating one first to track the test failure

### Step 3: Run QuarantineTools

Execute the QuarantineTools with the quarantine flag, test name(s), and issue URL:

```bash
dotnet run --project tools/QuarantineTools/QuarantineTools.csproj -- --quarantine "Namespace.ClassName.TestMethodName" --url "https://github.com/org/repo/issues/1234"
```

**Multiple tests** can be quarantined at once:
```bash
dotnet run --project tools/QuarantineTools/QuarantineTools.csproj -- --quarantine "Namespace.Class.Test1" "Namespace.Class.Test2" --url "https://github.com/org/repo/issues/1234"
```

**Command line flags:**
- `-q` or `--quarantine`: Quarantine mode (add attribute)
- `-i` or `--url`: GitHub issue URL (required for quarantine)
- Tests: Fully-qualified method names (space-separated)

### Step 4: Verify the Changes

1. The tool will output which files were modified:
   ```
   Updated 1 file(s):
    - tests/ProjectName.Tests/TestFile.cs
   ```

2. Read the modified file to confirm the attribute was added correctly:
   ```bash
   grep -A 2 -B 2 "QuarantinedTest" tests/ProjectName.Tests/TestFile.cs
   ```

3. Verify that:
   - The `[QuarantinedTest("issue-url")]` attribute appears above the test method
   - The `using Aspire.TestUtilities;` directive was added to the file (if not already present)

### Step 5: Build and Run Tests to Confirm

1. Build the test project to ensure no compilation errors:
   ```bash
   dotnet build tests/ProjectName.Tests/ProjectName.Tests.csproj
   ```

2. Run the test project with quarantine filter to verify the test is now quarantined:
   ```bash
   dotnet test tests/ProjectName.Tests/ProjectName.Tests.csproj --no-build -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
   ```

3. Confirm the quarantined test is NOT executed in the regular test run

4. Optionally, verify the test CAN be run with the quarantine filter:
   ```bash
   dotnet test tests/ProjectName.Tests/ProjectName.Tests.csproj --no-build -- --filter-trait "quarantined=true"
   ```

### Step 6: Report Results

Provide a clear summary:
- Which test(s) were quarantined
- The GitHub issue URL used
- Which file(s) were modified
- Confirmation that the test builds and is properly excluded from regular runs
- Remind the user to commit the changes

## Examples

### Example 1: Quarantine a single flaky test

User: "Quarantine the TestDistributedApplicationLifecycle test, it's flaky. Issue: https://github.com/dotnet/aspire/issues/5678"

Actions:
1. Find the fully-qualified name using Grep:
   ```bash
   grep -rn "void TestDistributedApplicationLifecycle" tests/
   ```
   Result: `tests/Aspire.Hosting.Tests/DistributedApplicationTests.cs`

2. Determine qualified name: `Aspire.Hosting.Tests.DistributedApplicationTests.TestDistributedApplicationLifecycle`

3. Run QuarantineTools:
   ```bash
   dotnet run --project tools/QuarantineTools/QuarantineTools.csproj -- -q "Aspire.Hosting.Tests.DistributedApplicationTests.TestDistributedApplicationLifecycle" -i "https://github.com/dotnet/aspire/issues/5678"
   ```

4. Verify output shows file was updated

5. Build and test:
   ```bash
   dotnet build tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj
   dotnet test tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj --no-build -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
   ```

6. Report: "Quarantined test `TestDistributedApplicationLifecycle` with issue #5678. The test is now excluded from regular CI runs and will run in the quarantine workflow."

### Example 2: Quarantine multiple related tests

User: "These three tests in RedisTests are all flaky, quarantine them: TestRedisConnection, TestRedisCache, TestRedisCommands. Issue: https://github.com/dotnet/aspire/issues/9999"

Actions:
1. Find fully-qualified names (assuming they're in `Aspire.Components.Tests.RedisTests`)

2. Run QuarantineTools with multiple tests:
   ```bash
   dotnet run --project tools/QuarantineTools/QuarantineTools.csproj -- -q "Aspire.Components.Tests.RedisTests.TestRedisConnection" "Aspire.Components.Tests.RedisTests.TestRedisCache" "Aspire.Components.Tests.RedisTests.TestRedisCommands" -i "https://github.com/dotnet/aspire/issues/9999"
   ```

3. Verify all three were modified

4. Build and test the project

5. Report: "Quarantined 3 tests from RedisTests with issue #9999."

### Example 3: User provides short test name

User: "Quarantine CanStartDashboard - it keeps timing out"

Actions:
1. Ask for GitHub issue: "What is the GitHub issue URL for tracking this flaky test?"

2. User provides: "https://github.com/dotnet/aspire/issues/4321"

3. Find the test:
   ```bash
   grep -rn "void CanStartDashboard" tests/
   ```
   Found in: `tests/Aspire.Dashboard.Tests/DashboardTests.cs`

4. Read the file to determine namespace: `Aspire.Dashboard.Tests`

5. Build fully-qualified name: `Aspire.Dashboard.Tests.DashboardTests.CanStartDashboard`

6. Run QuarantineTools:
   ```bash
   dotnet run --project tools/QuarantineTools/QuarantineTools.csproj -- -q "Aspire.Dashboard.Tests.DashboardTests.CanStartDashboard" -i "https://github.com/dotnet/aspire/issues/4321"
   ```

7. Build and verify

8. Report success

## Common Issues and Troubleshooting

### Issue: "No method found matching"
- **Cause**: The fully-qualified name is incorrect
- **Solution**: Use Grep to find the exact namespace, class name, and method name

### Issue: "The test is already quarantined"
- **Cause**: The attribute already exists on the method
- **Solution**: Verify by reading the test file; no action needed

### Issue: Tool reports "Quarantining requires a valid http(s) URL"
- **Cause**: The issue URL is missing or malformed
- **Solution**: Ensure the URL starts with `http://` or `https://`

### Issue: Build fails after quarantining
- **Cause**: The QuarantineTools may have encountered a syntax issue (rare)
- **Solution**: Read the modified file and check for syntax errors; the tool should handle this correctly

## Important Notes

1. **Always build after quarantining** to ensure the changes are valid
2. **Run tests to confirm** the quarantined test is properly excluded
3. **Don't forget to commit** the modified test files
4. **Track with GitHub issues** - every quarantined test should have an associated issue
5. **The QuarantineTools handles**:
   - Adding the `[QuarantinedTest("url")]` attribute
   - Adding `using Aspire.TestUtilities;` if needed
   - Preserving file formatting and indentation
   - Supporting nested classes and various namespace styles

## Unquarantining Tests

To unquarantine a test (remove the attribute), use:
```bash
dotnet run --project tools/QuarantineTools/QuarantineTools.csproj -- -u "Namespace.ClassName.TestMethodName"
```

The tool will:
- Remove the `[QuarantinedTest]` attribute
- Remove the `using Aspire.TestUtilities;` directive if no other tests in the file use it
