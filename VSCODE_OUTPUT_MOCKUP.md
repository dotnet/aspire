# VS Code Extension Output - Mock Screenshot

## Command Palette
```
> Aspire: Run diagnostics
```

## Progress Notification
```
┌─────────────────────────────────────────────┐
│ 🔄 Aspire Doctor                           │
│    Running diagnostics...                   │
└─────────────────────────────────────────────┘
```

## Output Channel: "Aspire Diagnostics"
```
Aspire Environment Diagnostics
==================================================

.NET SDK
--------------------------------------------------

  ✓  .NET 10.0.102 installed (x64)

  ⚠  HTTPS development certificate is not trusted
        Certificate 80CC62F46B5884DA48327D84F3CB39A2138E906A exists in the personal store but was not found in the trusted root store.
        Run: dotnet dev-certs https --trust
        See: https://aka.ms/aspire-prerequisites#dev-certs


Container Runtime
--------------------------------------------------

  ⚠  Docker Engine detected (version 28.0.4). Aspire's container tunnel is required to allow containers to reach applications running on the host
        Set environment variable: ASPIRE_ENABLE_CONTAINER_TUNNEL=true
        See: https://aka.ms/aspire-prerequisites#docker-engine


Summary
--------------------------------------------------
✓ Passed: 1
⚠ Warnings: 2
✗ Failed: 0

For detailed prerequisites, see: https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling
```

## Summary Notification (Warning)
```
┌─────────────────────────────────────────────┐
│ ⚠ Aspire Doctor: 1 passed, 2 warnings,     │
│   0 failed                                  │
└─────────────────────────────────────────────┘
```

## Notification Variations

### All Passed (Information)
```
┌─────────────────────────────────────────────┐
│ ℹ Aspire Doctor: 5 passed, 0 warnings,     │
│   0 failed                                  │
└─────────────────────────────────────────────┘
```

### Critical Failures (Error)
```
┌─────────────────────────────────────────────┐
│ ✗ Aspire Doctor: 2 passed, 1 warnings,     │
│   2 failed                                  │
└─────────────────────────────────────────────┘
```

## How It Works

1. User opens Command Palette (Ctrl+Shift+P / Cmd+Shift+P)
2. User types "Aspire: Run diagnostics" or "doctor"
3. Extension executes: `aspire doctor --format Json`
4. CLI returns structured JSON with check results
5. Extension parses JSON and formats for Output Channel
6. Extension shows summary notification based on severity:
   - Information (ℹ): All checks passed
   - Warning (⚠): Some warnings but no failures
   - Error (✗): One or more critical failures

## Benefits

- **Searchable**: Output Channel content is searchable and copyable
- **Persistent**: Results remain visible for reference
- **Non-intrusive**: Notification can be dismissed while output remains
- **Actionable**: Fix suggestions and links are clearly displayed
- **Categorized**: Results grouped by category for easy scanning
