# Investigation Findings: Test Verification Script

## Issue Discovered

### Problem
The `run-test-100-times.sh` script kills itself during cleanup.

**Root Cause:**
```bash
# Line 122 in clean_environment()
pkill -9 -f "dotnet test"
```

When the script is invoked as:
```bash
./run-test-100-times.sh -- dotnet test tests/Aspire.Hosting.Tests/...
```

The script's bash process has "dotnet test" in its command line arguments. The `pkill -f` searches the full command line, so it matches and kills the script itself!

### Evidence
```
$ ./run-test-100-times.sh -n 3 -- dotnet test ...
========================================
Test Verification Run — 3 iterations
Mode: STOP ON FIRST FAILURE
========================================
...
Killed  (exit code 137)
```

Exit code 137 = killed by SIGKILL (signal 9)

### Why This Happens
1. Script starts: `bash run-test-100-times.sh -- dotnet test ...`
2. Process command line contains "dotnet test"
3. `clean_environment()` runs `pkill -9 -f "dotnet test"`
4. pkill finds script's bash process (matches "dotnet test" in args)
5. Script gets SIGKILL and dies

## Solution Options

### Option 1: More Specific pkill Pattern
Instead of `pkill -f "dotnet test"`, use a pattern that won't match the script itself:

```bash
# Kill dotnet test processes, but not our script
pkill -9 -f "dotnet.*/dotnet test" 2>/dev/null || true
# Or be more specific
pkill -9 -f "exec dotnet test" 2>/dev/null || true
```

### Option 2: Exclude Current Process
```bash
# Get our PID and exclude it
SCRIPT_PID=$$
pgrep -f "dotnet test" | grep -v "^$SCRIPT_PID$" | xargs -r kill -9 2>/dev/null || true
```

### Option 3: Use Process Tree
Only kill actual test execution processes, not shells:

```bash
# Kill only dotnet processes running tests, not shells
ps aux | grep '[d]otnet.*test.*Aspire.Hosting.Tests' | awk '{print $2}' | xargs -r kill -9 2>/dev/null || true
```

### Option 4: Don't Kill "dotnet test"
Since we're running `--no-build`, the "dotnet test" is just a launcher. The actual test process is `dotnet-tests` which is already being killed.

```bash
# Remove this line:
# pkill -9 -f "dotnet test"

# Keep these:
pkill -9 -f "dotnet-tests" 2>/dev/null || true
pkill -9 -f "$TEST_ASSEMBLY_NAME" 2>/dev/null || true
```

## Recommended Fix

**Option 4** is safest and simplest. The actual test runner process is `dotnet-tests` (already being killed). The `dotnet test` launcher exits when tests complete, so we don't need to kill it.

### Implementation
```bash
clean_environment() {
    # Kill dcp / dcpctrl processes
    pgrep -lf "dcp" 2>/dev/null | grep -E "dcp(\.exe|ctl)" | awk '{system("kill -9 "$1)}' 2>/dev/null || true

    # Kill dotnet-tests processes (the actual test runner)
    pkill -9 -f "dotnet-tests" 2>/dev/null || true
    
    # Don't kill "dotnet test" as it may match our script's command line
    # The dotnet test launcher exits when tests complete

    # Kill processes matching the test assembly name
    if [[ -n "$TEST_ASSEMBLY_NAME" ]]; then
        pkill -9 -f "$TEST_ASSEMBLY_NAME" 2>/dev/null || true
    fi
    
    # ... rest of cleanup ...
}
```

## Testing the Fix

After removing `pkill -9 -f "dotnet test"`, the script should:
1. Run all iterations without killing itself
2. Properly clean up test processes between runs
3. Report results correctly

### Verification
```bash
# Should complete without "Killed" message
./run-test-100-times.sh -n 3 -- dotnet test tests/Aspire.Hosting.Tests/... 
```

## Impact

Without this fix:
- ❌ Script kills itself during cleanup
- ❌ Only runs environment logging, no actual tests
- ❌ Exits with code 137 (killed)
- ❌ No useful results

With this fix:
- ✅ Script runs all iterations
- ✅ Properly cleans between runs
- ✅ Reports results correctly
- ✅ Exits with appropriate code

## Status

**Issue:** Identified and understood  
**Fix:** Remove problematic pkill line  
**Next:** Apply fix and re-test
