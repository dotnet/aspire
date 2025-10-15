# Oracle Container Fixture Improvements

## Summary
Enhanced the `OracleContainerFixture` to address hang issues during container initialization by adding timeout handling, diagnostic logging, and better error messages.

## Changes Made

### 1. Added 3-Minute Timeout ⏱️
- Previously: No timeout, could hang indefinitely (up to 1 hour based on Testcontainers default)
- Now: Fails fast after 3 minutes with actionable error message
- Benefits:
  - CI pipeline fails faster instead of waiting 7+ minutes for hang dump
  - Developers get quicker feedback during local development
  - Reduces wasted CI resources

### 2. Diagnostic Logging 📝
Added logging at key points:
- ✅ Container initialization start
- ✅ Container image being used
- ✅ Successful startup confirmation
- ✅ Timeout/failure notifications
- ✅ Container logs retrieval on failure

### 3. Container Log Capture 🔍
On timeout, the fixture now:
- Attempts to retrieve container stdout/stderr logs
- Logs them using `IMessageSink` for visibility in test output
- Handles failures gracefully if logs can't be retrieved

### 4. Enhanced Error Messages 💬
Improved exception includes:
- Clear description of what failed
- Expected log message that never appeared: `"Completed: ALTER DATABASE OPEN"`
- Possible root causes:
  - Docker resource constraints
  - Networking issues
  - Container image problems
- Guidance to check container logs

## Code Comparison

### Before (Problematic)
```csharp
Container = new OracleBuilder()
    .WithPortBinding(1521, true)
    .WithHostname("localhost")
    .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/gvenzl/oracle-xe:21.3.0-slim-faststart")
    .WithWaitStrategy(Wait
        .ForUnixContainer()
        .UntilMessageIsLogged("Completed: ALTER DATABASE OPEN")
    ).Build();

await Container.StartAsync(); // ❌ No timeout, no diagnostics
```

### After (Improved)
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3)); // ✅ 3-min timeout

try
{
    _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Starting Oracle container...")); // ✅ Logging
    await Container.StartAsync(cts.Token); // ✅ Timeout enforced
    _diagnosticMessageSink.OnMessage(new DiagnosticMessage("Oracle container started successfully"));
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    // ✅ Capture container logs
    var (stdout, stderr) = await Container.GetLogsAsync(ct: CancellationToken.None);
    _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Container logs:\n{stdout}"));

    // ✅ Throw helpful exception
    throw new InvalidOperationException(
        "Oracle container failed to start within the 3-minute timeout. " +
        "The container did not log the expected startup completion message...",
        new TimeoutException("Container startup timed out after 3 minutes"));
}
```

## Expected Outcomes

### When Container Starts Successfully
```text
Starting Oracle container initialization...
Starting Oracle container with image: aspire-test-cr.azurecr.io/gvenzl/oracle-xe:21.3.0-slim-faststart
Oracle container started successfully
```

### When Container Fails to Start (Timeout)
```text
Starting Oracle container initialization...
Starting Oracle container with image: aspire-test-cr.azurecr.io/gvenzl/oracle-xe:21.3.0-slim-faststart
Oracle container failed to start within 3 minutes timeout
Container stdout logs:
<container output here>
Container stderr logs:
<any errors here>

InvalidOperationException: Oracle container failed to start within the 3-minute timeout.
The container did not log the expected startup completion message: 'Completed: ALTER DATABASE OPEN'.
This may indicate Docker resource constraints, networking issues, or problems with the container image.
Check the container logs above for more details.
```

## Testing Recommendations

### 1. Verify Timeout Works
Run the test and manually stop Docker daemon or container to verify:
- Timeout triggers after 3 minutes
- Error message is clear and actionable
- Container logs are captured

### 2. Verify Success Path
Run tests normally to ensure:
- Diagnostic messages appear in test output
- Container starts successfully
- Tests pass as before

### 3. Monitor CI Runs
After deploying:
- Watch for reduced hang times (should fail at ~3 minutes instead of 7+)
- Check that error messages provide useful debugging information
- Verify container logs are visible in CI output

## Future Improvements

### Short-term
- Consider adding retry logic with exponential backoff
- Add pre-flight Docker health check

### Medium-term
- Explore alternative wait strategies (port checks, health endpoints)
- Consider connection-based readiness check instead of log message

### Long-term
- Evaluate if different Oracle container image would be more reliable
- Consider quarantining if issue persists
- Provide mock/in-memory alternative for faster testing

## Related Issue
See `oracle-hang-issue.md` for detailed analysis of the original hang problem.
