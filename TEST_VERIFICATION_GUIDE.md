# Test Verification Guide

## Purpose
This document explains how to verify the fix for flaky test issue #9673 by running the test 100 times.

## Background
- **Issue**: #9673 - Test `TestPortOnEndpointAnnotationAndAllocatedEndpointAnnotationMatchForReplicatedServices` fails intermittently
- **Failure Rate**: ~23.5% (23-24 failures per 100 runs)
- **Root Cause**: Race condition - waiting for "Application started." log before HTTP endpoint is fully ready
- **Fix**: Replace `WaitForTextAsync` with `WaitForHealthyAsync` to properly wait for resource health

## Verification Script

### Script: `run-test-100-times.sh`

The verification script runs the flaky test 100 times with proper cleanup between runs.

**Features:**
- Runs test 100 iterations
- Cleans resources between each run (kills processes, cleans DCP resources)
- Tracks pass/fail/timeout statistics
- Generates detailed logs for each iteration
- Creates summary report with statistics
- Colored output for easy reading
- Exit code indicates overall success/failure

### Running the Verification

#### Prerequisites
- .NET SDK 8.0+
- DCP (Developer Control Plane) installed
- Test project built: `dotnet build tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj`

#### Execute
```bash
# Make script executable (if not already)
chmod +x run-test-100-times.sh

# Run verification
./run-test-100-times.sh
```

#### Output
The script provides:
- Real-time progress for each iteration
- Summary every 10 iterations
- Final statistics report
- Exit code (0 = success, 1 = failures detected)

#### Results Location
Results are saved to: `/tmp/test-results-YYYYMMDD-HHMMSS/`

**Files created:**
- `test-run.log` - Complete run log
- `summary.txt` - Summary report with statistics
- `iteration-N.log` - Log for each iteration
- `failure-N.log` - Detailed logs for failed iterations
- `timeout-N.log` - Detailed logs for timeout iterations

## Expected Results

### Before Fix
With the original code (using `WaitForTextAsync`):
- **Pass Rate**: ~76.5%
- **Fail Rate**: ~23.5%
- **Expected Failures**: 23-24 out of 100 runs
- **Failure Pattern**: Intermittent `TaskCanceledException` in fixture initialization

### After Fix
With the new code (using `WaitForHealthyAsync`):
- **Pass Rate**: 100%
- **Fail Rate**: 0%
- **Expected Failures**: 0 out of 100 runs
- **Reason**: No race condition - resources are truly ready before HTTP requests

## Understanding the Fix

### Problem
```csharp
// OLD CODE - Race condition
await App.WaitForTextAsync("Application started.", "servicea", cancellationToken);
using var clientA = App.CreateHttpClientWithResilience(...);
await clientA.GetStringAsync("/", cancellationToken); // ❌ Can fail if endpoint not ready
```

The log message "Application started." doesn't guarantee the HTTP endpoint is fully bound and ready.

### Solution
```csharp
// NEW CODE - Deterministic
await App.WaitForHealthyAsync(TestProgram.ServiceABuilder, cancellationToken);
using var clientA = App.CreateHttpClientWithResilience(...);
await clientA.GetStringAsync("/", cancellationToken); // ✅ Endpoint guaranteed ready
```

`WaitForHealthyAsync` waits for:
1. Resource to reach "Running" state
2. Resource ready event to complete
3. Endpoint fully initialized and accepting connections

## Interpreting Results

### Success (Expected)
```
========================================
Test Results Summary
========================================
Total Iterations: 100
Passed:  100 (100.0%)
Failed:  0 (0.0%)
Errors:  0 (0.0%)
========================================
Status: SUCCESS - All tests passed!
```

This confirms the fix eliminates the race condition.

### Partial Failures (Unexpected)
If 1-5 failures occur out of 100:
- May indicate environmental issues (CI load, resource constraints)
- Review failure logs to identify pattern
- Compare to baseline (23.5%) - still a significant improvement

### High Failure Rate (Requires Investigation)
If >10 failures occur out of 100:
- Fix may not fully address the issue
- Review failure logs for error patterns
- May need additional investigation

## Troubleshooting

### Test Won't Run
**Symptom**: Script fails immediately
**Cause**: DCP not installed or test project not built
**Solution**:
1. Ensure DCP is installed: `~/.aspire/` directory exists
2. Build test project: `dotnet build tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj`
3. Run restore: `./restore.sh`

### Timeouts
**Symptom**: Tests timeout (180 seconds)
**Cause**: Resources taking too long to start
**Solution**:
- Check system resources (CPU, memory)
- Review timeout logs in results directory
- May need to increase timeout in script

### Process Cleanup Issues
**Symptom**: "Address already in use" errors
**Cause**: Previous test processes still running
**Solution**:
- Script automatically cleans processes
- Manually kill if needed: `pkill -9 -f "Aspire.Hosting.Tests"`

## CI Integration

### GitHub Actions
The verification script can be integrated into CI:

```yaml
- name: Verify flaky test fix
  run: |
    ./run-test-100-times.sh
  timeout-minutes: 300  # 5 hours for 100 runs

- name: Upload test results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: test-verification-results
    path: /tmp/test-results-*/
```

### Azure DevOps
```yaml
- script: ./run-test-100-times.sh
  displayName: 'Verify Flaky Test Fix'
  timeoutInMinutes: 300

- task: PublishTestResults@2
  condition: always()
  inputs:
    testResultsFiles: '/tmp/test-results-*/*.log'
```

## Additional Testing

### Related Tests
The fix also affects these quarantined tests (same fixture):
- `TestProjectStartsAndStopsCleanly` (#9672)
- `TestPortOnEndpointAnnotationAndAllocatedEndpointAnnotationMatch` (#9671)

These should also be verified once unquarantined.

### Load Testing
For comprehensive verification:
1. Run script multiple times (5+ times = 500+ test runs)
2. Run on different OS (Windows, Linux, macOS)
3. Run under load conditions
4. Monitor resource usage during runs

## Conclusion

The verification script provides empirical evidence that the fix eliminates the race condition. A successful run (100/100 passes) demonstrates that:

1. ✅ Race condition is eliminated
2. ✅ Resources are properly initialized before use
3. ✅ HTTP endpoints are ready when accessed
4. ✅ Test is now deterministic and reliable

Expected outcome: **100/100 passes** confirming the fix works correctly.
