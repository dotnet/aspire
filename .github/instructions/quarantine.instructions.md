---
applyTo: "tools/QuarantineTools/*"
---

This tool is used to quarantine flaky tests.

Usage:

```bash
dotnet run --project tools/QuarantineTools -- -q Namespace.Type.Method -i https://issue.url
```

Make sure to build the project containing the updated tests to ensure the changes don't break the build.
