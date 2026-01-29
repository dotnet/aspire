# Manual Testing Guide for Aspire Doctor Command

## Overview
This document describes how to manually test the new `aspire doctor` command integration with the VS Code extension.

## Prerequisites
1. Build the Aspire CLI: `./build.sh` (or `./build.cmd` on Windows)
2. Build the VS Code extension:
   ```bash
   cd extension
   npm install
   npm run compile
   ```
3. Open VS Code to the extension development host (F5 from extension directory)

## Test Scenarios

### Scenario 1: Basic Doctor Command Execution
**Steps:**
1. Open Command Palette (Ctrl+Shift+P / Cmd+Shift+P)
2. Type "Aspire: Run diagnostics"
3. Select the command

**Expected Results:**
- Progress notification appears: "Aspire Doctor - Running diagnostics..."
- "Aspire Diagnostics" output channel opens automatically
- Diagnostic results are displayed with formatted categories:
  - .NET SDK
  - Container Runtime
  - Environment (if applicable)
- Each check shows status icon (✓, ⚠, or ✗)
- Fix suggestions are displayed for warnings/failures
- Documentation links are included where applicable
- Summary notification appears with format: "Aspire Doctor: X passed, Y warnings, Z failed"

**Screenshot Location:**
(Screenshot should show the Output Channel with formatted results)

### Scenario 2: All Checks Pass
**Setup:**
- Ensure .NET SDK is installed
- Ensure Docker is running (if applicable)
- Ensure dev certificates are trusted

**Expected Results:**
- All checks show ✓ (pass) icon
- Summary shows: "Aspire Doctor: X passed, 0 warnings, 0 failed"
- Information-level notification (blue icon)

### Scenario 3: Some Warnings
**Setup:**
- Untrust dev certificates (or similar non-critical issue)

**Expected Results:**
- Some checks show ⚠ (warning) icon
- Fix suggestions are displayed
- Summary shows warnings count > 0
- Warning-level notification (yellow icon)
- Output includes link to detailed prerequisites

### Scenario 4: Critical Failures
**Setup:**
- Stop Docker (if testing container checks)
- OR remove .NET SDK (if testing SDK checks)

**Expected Results:**
- Failed checks show ✗ (fail) icon
- Fix suggestions are displayed
- Summary shows failures count > 0
- Error-level notification (red icon)
- Output includes link to detailed prerequisites

### Scenario 5: JSON Output Format (CLI Only)
**Steps:**
```bash
cd /path/to/aspire
dotnet run --project src/Aspire.Cli/Aspire.Cli.csproj -- doctor --format Json
```

**Expected Results:**
- Clean JSON output (no status messages)
- Structure matches:
  ```json
  {
    "checks": [
      {
        "category": "sdk",
        "name": "dotnet-sdk",
        "status": "pass|warning|fail",
        "message": "...",
        "fix": "...",  // optional
        "link": "...",  // optional
        "details": "..."  // optional
      }
    ],
    "summary": {
      "passed": 0,
      "warnings": 0,
      "failed": 0
    }
  }
  ```

### Scenario 6: Human-Readable Output (CLI Only)
**Steps:**
```bash
cd /path/to/aspire
dotnet run --project src/Aspire.Cli/Aspire.Cli.csproj -- doctor
```

**Expected Results:**
- Formatted table output with:
  - Category headers
  - Status icons (✓, ⚠, ✗)
  - Messages
  - Fix suggestions (indented)
  - Documentation links (indented)
  - Summary line at the end

## Verification Checklist

- [ ] Command appears in Command Palette as "Aspire: Run diagnostics"
- [ ] Output Channel opens automatically
- [ ] Output Channel name is "Aspire Diagnostics"
- [ ] Results are grouped by category
- [ ] Categories are in correct order (SDK, Container, Environment)
- [ ] Status icons display correctly (✓, ⚠, ✗)
- [ ] Messages are clear and readable
- [ ] Fix suggestions are displayed when available
- [ ] Documentation links are displayed when available
- [ ] Summary notification appears with correct severity
- [ ] Summary notification shows accurate counts
- [ ] Extension mode automatically uses JSON format
- [ ] CLI JSON format works independently
- [ ] CLI human-readable format works independently

## Known Limitations
1. This is a read-only diagnostic tool - it does not automatically fix issues
2. Some checks may require elevated permissions (e.g., Docker daemon)
3. Extension mode testing requires VS Code Extension Host

## Sample Output

### CLI Human-Readable Output
```
Aspire Environment Check
========================

.NET SDK
  ✓  .NET 10.0.102 installed (x64)
  ⚠  HTTPS development certificate is not trusted
        Certificate ABC123... exists in the personal store but was not found in the trusted root store.
        Run: dotnet dev-certs https --trust
        See: https://aka.ms/aspire-prerequisites#dev-certs

Container Runtime
  ⚠  Docker Engine detected (version 28.0.4). Aspire's container tunnel is required to allow containers to reach applications running on the host
        Set environment variable: ASPIRE_ENABLE_CONTAINER_TUNNEL=true
        See: https://aka.ms/aspire-prerequisites#docker-engine

Summary: 1 passed, 2 warnings, 0 failed
For detailed prerequisites: https://aka.ms/aspire-prerequisites
```

### CLI JSON Output
```json
{
  "checks": [
    {
      "category": "sdk",
      "name": "dotnet-sdk",
      "status": "pass",
      "message": ".NET 10.0.102 installed (x64)"
    },
    {
      "category": "sdk",
      "name": "dev-certs",
      "status": "warning",
      "message": "HTTPS development certificate is not trusted",
      "fix": "Run: dotnet dev-certs https --trust",
      "link": "https://aka.ms/aspire-prerequisites#dev-certs",
      "details": "Certificate ABC123... exists in the personal store but was not found in the trusted root store."
    }
  ],
  "summary": {
    "passed": 1,
    "warnings": 1,
    "failed": 0
  }
}
```

## Notes
- The doctor command is designed to be non-intrusive and informational
- All checks should complete quickly (< 5 seconds total)
- The extension automatically formats the output for better readability
- Fix suggestions should be actionable and clear
