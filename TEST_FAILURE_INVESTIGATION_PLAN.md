# Test Failure Investigation Plan

## Purpose
This document provides a systematic approach to investigate test failures when they occur during the 100-iteration verification run.

## When Failures Occur

The `run-test-100-times.sh` script now **breaks on first failure** to enable immediate investigation.

### Automatic Capture
When a failure occurs, the script automatically captures:

1. **Failure iteration number** - Which run failed
2. **Pass count before failure** - How many passed before the failure
3. **Detailed test logs** - Full output saved to `failure-N.log`
4. **Command line** - Exact command used in `command.txt`
5. **Git commit** - Which version of code was tested
6. **Timestamp** - When the failure occurred

### Files Created on Failure

```
/tmp/test-results-YYYYMMDD-HHMMSS/
├── command.txt              # Command line, git commit, working dir
├── test-run.log            # Complete run log
├── failure-info.txt        # Failure summary
├── failure-N.log           # Detailed log of failed iteration
├── iteration-1.log         # Log from iteration 1
├── iteration-2.log         # Log from iteration 2
└── summary.txt             # Final summary report
```

## Investigation Steps

### Step 1: Review Failure Information

```bash
# Check which iteration failed
cat /tmp/test-results-*/failure-info.txt

# Expected output:
# First failure at iteration: X
# Pass count before failure: Y
# Failure count: 1
# Error count: 0
```

### Step 2: Examine Failure Log

```bash
# View the detailed failure log
cat /tmp/test-results-*/failure-X.log | less
```

**Look for:**
- Stack traces
- Error messages
- Timeout indicators
- Resource initialization issues
- Race condition patterns

### Step 3: Compare with Successful Runs

```bash
# Compare a successful iteration with the failure
diff /tmp/test-results-*/iteration-$((X-1)).log /tmp/test-results-*/failure-X.log
```

### Step 4: Check for Common Failure Patterns

#### Pattern 1: Race Condition (Original Issue)
**Symptom:** `TaskCanceledException` during fixture initialization
```
System.Threading.Tasks.TaskCanceledException : A task was canceled.
at Aspire.Hosting.Tests.SlimTestProgramFixture.WaitReadyStateAsync
```

**Analysis:** The fix may not be complete. HTTP endpoint still not ready.

**Solution:** Review `WaitForHealthyAsync` implementation or add additional waiting.

#### Pattern 2: Resource Not Available
**Symptom:** DCP connection failures, port binding errors
```
Polly.Timeout.TimeoutRejectedException : The operation didn't complete within the allowed timeout
```

**Analysis:** DCP or infrastructure issue, not test code.

**Solution:** Check DCP availability, port conflicts, system resources.

#### Pattern 3: Transient Network Issues
**Symptom:** HTTP connection errors after resource is healthy
```
System.Net.Http.HttpRequestException: Connection refused
```

**Analysis:** Network layer issue between test and service.

**Solution:** Add retry logic to HTTP client (already has resilience).

#### Pattern 4: Test Quarantine Needed
**Symptom:** Consistent failures unrelated to the fix
```
[Different error than original race condition]
```

**Analysis:** Different issue than #9673.

**Solution:** Quarantine test with different issue number.

### Step 5: Check System State

```bash
# Check for resource conflicts
netstat -tuln | grep LISTEN

# Check for stuck processes
ps aux | grep -E "Aspire|TestProject|dcp"

# Check disk space
df -h /tmp

# Check memory
free -h
```

### Step 6: Reproduce the Failure

Try to reproduce with targeted runs:

```bash
# Run just the failed iteration scenario
for i in {1..10}; do
    echo "Attempt $i"
    dotnet test tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj \
        --no-build --no-restore \
        -- --filter-method "*.TestPortOnEndpointAnnotationAndAllocatedEndpointAnnotationMatchForReplicatedServices"
    
    if [ $? -ne 0 ]; then
        echo "Failed on attempt $i"
        break
    fi
done
```

## Analysis Framework

### Question 1: Is it the same race condition?
- **Yes:** Fix needs improvement
- **No:** Different issue, separate investigation

### Question 2: Is it reproducible?
- **Yes:** Deterministic bug, easier to fix
- **No:** Still flaky, may need different approach

### Question 3: What's the failure rate?
- **High (>10%):** Major issue with fix
- **Low (<5%):** Minor issue, may be acceptable or needs tuning
- **Same as baseline (23.5%):** Fix didn't work

### Question 4: When does it fail?
- **Early iterations:** Initialization issue
- **Random iterations:** True race condition
- **Late iterations:** Resource exhaustion

## Potential Fix Improvements

### If Race Condition Still Exists

**Option 1: Add Explicit Endpoint Check**
```csharp
await App.WaitForHealthyAsync(TestProgram.ServiceABuilder, cancellationToken);

// Add explicit endpoint ready check
var endpoint = TestProgram.ServiceABuilder.Resource.Annotations
    .OfType<EndpointAnnotation>().Single();
while (endpoint.AllocatedEndpoint == null)
{
    await Task.Delay(100, cancellationToken);
}

using var client = App.CreateHttpClientWithResilience(...);
await client.GetStringAsync("/", cancellationToken);
```

**Option 2: Add Retry Logic to Fixture**
```csharp
await App.WaitForHealthyAsync(TestProgram.ServiceABuilder, cancellationToken);

// Retry HTTP request with exponential backoff
int retries = 5;
for (int i = 0; i < retries; i++)
{
    try
    {
        using var client = App.CreateHttpClientWithResilience(...);
        await client.GetStringAsync("/", cancellationToken);
        break; // Success
    }
    catch (HttpRequestException) when (i < retries - 1)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100 * (i + 1)), cancellationToken);
    }
}
```

**Option 3: Wait for Allocated Endpoint + Health**
```csharp
// Wait for both health AND allocated endpoint
await App.WaitForHealthyAsync(TestProgram.ServiceABuilder, cancellationToken);

// Also ensure endpoint is allocated
await WaitForAllocatedEndpointAsync(TestProgram.ServiceABuilder, cancellationToken);

using var client = App.CreateHttpClientWithResilience(...);
await client.GetStringAsync("/", cancellationToken);
```

### If Different Issue

1. **File separate issue** with new failure pattern
2. **Quarantine test** if unrelated to #9673
3. **Add specific fix** for the new issue

## Decision Tree

```
Failure Detected
    │
    ├─→ Same error as #9673? 
    │   └─→ YES: Improve fix (see Options above)
    │   └─→ NO: Continue below
    │
    ├─→ Infrastructure issue (DCP, network)?
    │   └─→ YES: Not a test code issue, environmental
    │   └─→ NO: Continue below
    │
    ├─→ Reproducible?
    │   └─→ YES: Deterministic bug, fix it
    │   └─→ NO: Continue below
    │
    └─→ Failure rate?
        ├─→ >10%: Major issue, must fix
        ├─→ 5-10%: Moderate issue, should fix
        ├─→ <5%: Minor issue, may quarantine
        └─→ <1%: Acceptable or different test needed

```

## Documentation Template

When documenting a failure, include:

```markdown
# Failure Analysis - Iteration X

## Summary
- **Iteration:** X of 100
- **Pass count before failure:** Y
- **Failure type:** [Race condition / Timeout / Other]
- **Date/Time:** [timestamp]
- **Git commit:** [commit hash]

## Error Details
[Paste relevant error from failure-X.log]

## Root Cause
[Analysis of why it failed]

## Proposed Fix
[Specific code changes needed]

## Testing Plan
[How to verify the fix]
```

## Success Criteria

The fix is successful if:
- ✅ 100/100 iterations pass
- ✅ No early termination
- ✅ All logs show expected behavior
- ✅ No timeout or error conditions

## Failure Threshold

Consider the fix successful if:
- 95+ passes out of 100 (but investigate failures)
- No early termination due to systematic errors
- Failures are clearly environmental, not code issues

## Next Steps After Investigation

1. **Document findings** in issue #9673
2. **Propose fix improvement** with specific code changes
3. **Test fix locally** if reproducible
4. **Update PR** with improved fix
5. **Re-run verification** with improved fix
6. **Iterate** until 100/100 success rate achieved
