# Dogfooding Aspire 13.0

This document provides instructions for dogfooding Aspire 13.0 ahead of its release.

## Overview

Aspire 13.0 is a synchronous release that will ship alongside .NET 10 and .NET servicing releases. Because it depends on versions that are currently only available through internal feeds, we provide two dogfooding options:

## Choose Your Option

### [Option 1: Full Staging Build (Internal Feeds Required)](dogfood-option1.md)

Test the actual bits that will be shipped, including internal .NET dependencies. This requires authentication to internal feeds.

**Choose this if:**
- You have access to internal .NET feeds
- You want to test the exact release candidate
- You're comfortable setting up authenticated NuGet feeds

👉 **[Follow Option 1 Instructions](dogfood-option1.md)**

---

### [Option 2: Public Build (Public Feeds Only)](dogfood-option2.md)

Test the latest Aspire bits with publicly available .NET dependencies. This doesn't test the exact shipping configuration but is easier to set up.

**Choose this if:**
- You prefer a simpler setup without authentication
- You don't have access to internal feeds
- You still want to test the latest Aspire functionality

👉 **[Follow Option 2 Instructions](dogfood-option2.md)**

---

## Prerequisites

- Windows, Linux, or macOS
- PowerShell (for some setup scripts)
- .NET SDK installed

---

## Reporting Issues

If you encounter issues during dogfooding:

1. Search existing issues: https://github.com/dotnet/aspire/issues
2. File a new issue with:
   - Which option you chose (Option 1 or Option 2)
   - Your OS and .NET SDK version
   - Complete error messages and logs
   - Steps to reproduce

---

## Additional Resources

- [Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [.NET 10 Release Information](https://github.com/dotnet/core/tree/main/release-notes/10.0)
- [Contributing Guide](./docs/contributing.md)

