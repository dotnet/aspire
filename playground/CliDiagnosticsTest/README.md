# CLI Diagnostics Test Project

This test project demonstrates the improved CLI error diagnostics with FileLoggerProvider.

## Test Scenarios

### 1. Build Failure (`BuildFailure`)
Tests the scenario where a project fails to build due to compilation errors.

**Expected behavior:**
- Clean error message on console by default
- Full build output, command, and exit code captured in `~/.aspire/cli/diagnostics/{timestamp}/aspire.log`
- Log file path displayed on error

**To test:**
```bash
cd BuildFailure
aspire run
# Should show clean error with log file path

aspire run --log-level Debug
# Should show more detailed diagnostics
```

### 2. AppHost Exception (`AppHostException`)
Tests the scenario where the AppHost throws an exception during startup.

**Expected behavior:**
- Clean error message on console by default
- Full exception details with stack trace in log file
- Log file path displayed on error

**To test:**
```bash
cd AppHostException
aspire run
# Should show clean error with log file path

aspire run --log-level Debug
# Should show full exception and stack trace
```

### 3. Unexpected CLI Error (`UnexpectedError`)
Tests the scenario where an unexpected error occurs in the CLI itself.

**Expected behavior:**
- Clean error message on console by default
- Full exception details, environment snapshot, and error.txt in diagnostics bundle
- Log file path displayed on error

**To test:**
```bash
cd UnexpectedError
aspire run
# Should show clean error with log file path

aspire run --log-level Debug
# Should show full exception and stack trace
```

## Verification

After running each test, check:
1. The log file exists at the displayed path
2. The log file contains all relevant information:
   - Build commands and output (for build failures)
   - Exception messages and stack traces
   - Environment information
3. The diagnostics bundle contains:
   - `aspire.log` - Full session log
   - `error.txt` - Human-readable error summary (when errors occur)
   - `environment.json` - Environment snapshot (when errors occur)
