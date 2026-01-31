# Aspire Bundle - Self-Contained Distribution

> **Status:** Draft Specification  
> **Last Updated:** January 2026

This document specifies the **Aspire Bundle**, a self-contained distribution package that provides the Aspire CLI along with all runtime components (Dashboard, DCP) needed to run any Aspire application.

## Table of Contents

1. [Overview](#overview)
2. [Problem Statement](#problem-statement)
3. [Goals and Non-Goals](#goals-and-non-goals)
4. [Architecture](#architecture)
5. [Bundle Layout](#bundle-layout)
6. [Component Discovery](#component-discovery)
7. [NuGet Operations](#nuget-operations)
8. [Certificate Management](#certificate-management)
9. [AppHost Server](#apphost-server)
10. [CLI Integration](#cli-integration)
11. [Configuration](#configuration)
12. [Size and Distribution](#size-and-distribution)
13. [Security Considerations](#security-considerations)
14. [Build Process](#build-process)

---

## Overview

The Aspire Bundle is a platform-specific archive containing the Aspire CLI and all runtime components:

- **Aspire CLI** (native AOT executable)
- **.NET Runtime** (for running managed components)
- **Pre-built AppHost Server** (for polyglot app hosts)
- **Aspire Dashboard** (no longer distributed via NuGet)
- **Developer Control Plane (DCP)** (no longer distributed via NuGet)
- **NuGet Helper Tool** (for package search and restore without SDK)
- **Dev-Certs Tool** (for HTTPS certificate management without SDK)

**Key change**: DCP and Dashboard are now bundled with the CLI installation, not downloaded as NuGet packages. This applies to **all** Aspire applications, including .NET ones. This:

- Eliminates large NuGet package downloads on first run
- Ensures version consistency between CLI and runtime components
- Simplifies the Aspire.Hosting SDK (no more MSBuild magic for DCP/Dashboard)
- Makes offline scenarios work reliably

Users download a single archive (~200 MB compressed, ~577 MB on disk), extract it, and have everything needed to run any Aspire application.

---

## Problem Statement

Currently, Aspire has two distribution challenges:

### For Polyglot App Hosts
The polyglot app host requires a globally-installed .NET SDK for:
1. **Dynamic Project Build**: The AppHost Server project is generated and built at runtime
2. **Package Operations**: `aspire add` uses `dotnet package search`
3. **Component Resolution**: DCP and Dashboard come from NuGet

### For All Applications
DCP and Dashboard distribution via NuGet packages causes:
1. **Large first-run downloads**: ~100+ MB of NuGet packages
2. **Version skew**: Dashboard/DCP version can mismatch CLI version
3. **Complex MSBuild targets**: Magic in Aspire.Hosting.AppHost SDK
4. **Offline difficulties**: Needs NuGet cache or internet access

---

## Goals and Non-Goals

### Goals

- **Zero .NET SDK dependency** for polyglot app host scenarios
- **Single download** containing all required runtime components
- **Unified DCP/Dashboard distribution** - bundled with CLI, not via NuGet
- **Offline capable** once the bundle is installed
- **Same functionality** as current approach, simpler distribution
- **Backward compatible** with existing SDK-based workflows

### Non-Goals

- Replacing the .NET SDK for .NET app host development
- Supporting `aspire new` for .NET project templates (requires SDK)
- Auto-updating the bundle (manual download for now)

---

## Architecture

### Component Interaction

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ASPIRE BUNDLE                                   │
│                          aspire-{version}-{platform}                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐     spawns      ┌───────────────────────────────────┐    │
│  │   aspire     │ ───────────────▶│          .NET RUNTIME             │    │
│  │ (Native AOT) │                 │                                   │    │
│  │              │                 │  • Runs AppHost Server            │    │
│  │  Commands:   │                 │  • Runs NuGet Helper Tool         │    │
│  │  • run       │                 │  • Hosts Dashboard                │    │
│  │  • add       │                 └───────────────────────────────────┘    │
│  │  • new       │                              │                           │
│  │  • publish   │                              ▼                           │
│  └──────┬───────┘              ┌───────────────────────────────────┐       │
│         │                      │        APPHOST SERVER             │       │
│         │  JSON-RPC            │                                   │       │
│         │◀────────────────────▶│  • Aspire.Hosting.* assemblies    │       │
│         │   (socket)           │  • RemoteHostServer endpoint      │       │
│         │                      │  • Dynamic integration loading    │       │
│         │                      └───────────────┬───────────────────┘       │
│         │                                      │                           │
│         │              ┌───────────────────────┼───────────────────┐       │
│         │              ▼                       ▼                   ▼       │
│         │     ┌─────────────┐         ┌─────────────┐     ┌────────────┐  │
│         │     │  DASHBOARD  │         │     DCP     │     │INTEGRATIONS│  │
│         │     │             │         │             │     │            │  │
│         │     │ dashboard/  │         │ dcp/        │     │~/.aspire/  │  │
│         │     └─────────────┘         └─────────────┘     │ packages/  │  │
│         │                                                  └────────────┘  │
│         │                                                        ▲         │
│         │     ┌─────────────────────────────────────────┐        │         │
│         │     │         USER'S APPHOST                  │────────┘         │
│         │     │    (TypeScript / Python / etc.)         │                  │
│         │     │                                         │                  │
│         │     │    apphost.ts / app.py                  │                  │
│         │     └─────────────────────────────────────────┘                  │
│         │                                                                  │
└─────────┴──────────────────────────────────────────────────────────────────┘
```

### Execution Flow

When a user runs `aspire run` with a TypeScript app host:

1. **CLI reads project configuration** from `.aspire/settings.json`
2. **CLI discovers bundle layout** using priority-based resolution
3. **CLI downloads missing integrations** using the NuGet Helper Tool
4. **CLI generates `appsettings.json`** for the AppHost Server with integration list
5. **CLI starts AppHost Server** using the bundled .NET runtime
6. **CLI starts guest app host** (TypeScript) which connects via JSON-RPC
7. **AppHost Server orchestrates** containers, Dashboard, and DCP

---

## Bundle Layout

### Directory Structure

```text
aspire-{version}-{platform}/
│
├── aspire[.exe]                        # Native AOT CLI (~25 MB)
│
├── layout.json                         # Bundle metadata
│
├── runtime/                            # .NET 10 Runtime (~106 MB)
│   ├── dotnet[.exe]                    # Muxer executable
│   ├── LICENSE.txt
│   ├── host/
│   │   └── fxr/{version}/
│   │       └── hostfxr.{dll|so|dylib}
│   └── shared/
│       ├── Microsoft.NETCore.App/{version}/
│       │   └── *.dll
│       └── Microsoft.AspNetCore.App/{version}/
│           └── *.dll
│
├── aspire-server/                     # Pre-built AppHost Server (~19 MB)
│   ├── aspire-server[.exe]             # Single-file executable
│   └── appsettings.json                # Default config
│
├── dashboard/                          # Aspire Dashboard (~42 MB)
│   ├── aspire-dashboard[.exe]          # Single-file executable
│   ├── wwwroot/
│   └── ...
│
├── dcp/                                # Developer Control Plane (~127 MB)
│   ├── dcp[.exe]                       # Native executable
│   └── ...
│
└── tools/                              # Helper tools (~5 MB)
    ├── aspire-nuget/                   # NuGet operations
    │   ├── aspire-nuget[.exe]          # Single-file executable
    │   └── ...
    │
    └── dev-certs/                      # HTTPS certificate tool
        ├── dotnet-dev-certs.dll
        ├── dotnet-dev-certs.deps.json
        └── dotnet-dev-certs.runtimeconfig.json
```

**Total Bundle Size:**
- **Unzipped:** ~323 MB
- **Zipped:** ~113 MB

### layout.json Schema

```json
{
  "version": "13.2.0",
  "platform": "linux-x64",
  "runtimeVersion": "10.0.0",
  "components": {
    "cli": "aspire",
    "runtime": "runtime",
    "apphostServer": "aspire-server",
    "dashboard": "dashboard",
    "dcp": "dcp",
    "nugetHelper": "tools/aspire-nuget",
    "devCerts": "tools/dev-certs"
  },
  "builtInIntegrations": []
}
```

---

## Component Discovery

The CLI and `Aspire.Hosting` both need to discover DCP, Dashboard, and .NET runtime locations. During the transition period, different versions of CLI and Aspire.Hosting may be used together, so both components implement discovery with graceful fallback.

### Discovery Priority

Both CLI and Aspire.Hosting follow this priority order for DCP and Dashboard:

1. **Environment variables** (`ASPIRE_DCP_PATH`, `ASPIRE_DASHBOARD_PATH`, `ASPIRE_RUNTIME_PATH`) - highest priority
2. **Disk discovery** - check for bundle layout next to the executable
3. **Assembly metadata** - NuGet package paths embedded at build time (Aspire.Hosting only)

For .NET runtime resolution (used when launching Dashboard):

1. **Environment variable** (`ASPIRE_RUNTIME_PATH`) - set by CLI for guest apphosts
2. **Disk discovery** - check for `runtime/` directory next to the app
3. **PATH fallback** - use `dotnet` from system PATH

### Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPIRE_LAYOUT_PATH` | Root of the bundle | `/opt/aspire` |
| `ASPIRE_DCP_PATH` | DCP binaries location | `/opt/aspire/dcp` |
| `ASPIRE_DASHBOARD_PATH` | Dashboard executable path | `/opt/aspire/dashboard/aspire-dashboard` |
| `ASPIRE_RUNTIME_PATH` | Bundled .NET runtime directory (guest apphosts only) | `/opt/aspire/runtime` |
| `ASPIRE_INTEGRATION_LIBS_PATH` | Path to integration DLLs for aspire-server assembly resolution | `/home/user/.aspire/libs` |
| `ASPIRE_USE_GLOBAL_DOTNET` | Force SDK mode | `true` |
| `ASPIRE_REPO_ROOT` | Dev mode (Aspire repo path, DEBUG builds only) | `/home/user/aspire` |

**Note:** `ASPIRE_RUNTIME_PATH` is only set for guest (polyglot) apphosts. .NET apphosts use the globally installed `dotnet`.

**Note:** `ASPIRE_INTEGRATION_LIBS_PATH` is set by the CLI when running guest apphosts that require additional hosting integration packages (e.g., `Aspire.Hosting.Redis`). The aspire-server uses this path to resolve integration assemblies at runtime.

### Transition Compatibility

During the transition from NuGet-based to bundle-based distribution, these version combinations must work:

#### Scenario 1: New CLI + New Aspire.Hosting

```text
Bundle CLI ────► runs ────► .NET AppHost (new Aspire.Hosting)
     │                           │
     │ sets ASPIRE_DCP_PATH      │ reads ASPIRE_DCP_PATH
     │ sets ASPIRE_DASHBOARD_PATH│ reads ASPIRE_DASHBOARD_PATH
     │                           │
     └──────────────────────────►│ Uses bundled DCP/Dashboard ✓
```

**Behavior**: CLI detects bundle, sets environment variables. Aspire.Hosting reads them first.

#### Scenario 2: New CLI + Old Aspire.Hosting

```text
Bundle CLI ────► runs ────► .NET AppHost (old Aspire.Hosting)
     │                           │
     │ sets ASPIRE_DCP_PATH      │ ignores (doesn't check env vars)
     │ sets ASPIRE_DASHBOARD_PATH│ 
     │                           │
     │                           │ Uses NuGet package paths ✓
```

**Behavior**: CLI sets env vars, but old Aspire.Hosting doesn't read them. Falls back to assembly metadata (NuGet packages). Works correctly.

#### Scenario 3: Old CLI + New Aspire.Hosting

```text
Old CLI ────► runs ────► .NET AppHost (new Aspire.Hosting)
     │                        │
     │ (no env vars set)      │ checks env vars → empty
     │                        │ does disk discovery → not found
     │                        │ uses assembly metadata (NuGet) ✓
```

**Behavior**: No env vars set. New Aspire.Hosting tries disk discovery, doesn't find bundle, falls back to NuGet packages.

#### Scenario 4: No CLI (direct `dotnet run`)

```text
dotnet run ────► .NET AppHost (any Aspire.Hosting)
                      │
                      │ checks env vars → empty
                      │ does disk discovery → not found (unless bundle installed)
                      │ uses assembly metadata (NuGet) ✓
```

**Behavior**: Standard SDK workflow. Uses NuGet packages as always.

#### Scenario 5: Bundle installed system-wide

```text
dotnet run ────► .NET AppHost (new Aspire.Hosting)
                      │
                      │ checks env vars → empty
                      │ does disk discovery → finds /opt/aspire/dcp ✓
                      │ uses bundled DCP/Dashboard ✓
```

**Behavior**: Even without CLI, if bundle is installed to a well-known location and AppHost is run from there, disk discovery finds it.

### Why Both Need Discovery

| Component | When it discovers | What it does |
|-----------|------------------|--------------|
| **CLI** | Before launching AppHost | Sets `ASPIRE_DCP_PATH`, `ASPIRE_DASHBOARD_PATH`, and `ASPIRE_RUNTIME_PATH` (guest only) env vars |
| **Aspire.Hosting** | At AppHost startup | Reads env vars OR does its own disk discovery OR uses NuGet |

This dual-discovery approach ensures:
- **Forward compatibility**: New CLI works with old Aspire.Hosting
- **Backward compatibility**: Old CLI works with new Aspire.Hosting
- **Standalone operation**: Aspire.Hosting works even without CLI

---

## NuGet Operations

The bundle includes a managed NuGet Helper Tool that provides package search and restore functionality without requiring the .NET SDK.

### NuGet Helper Commands

```bash
# Search for packages
{runtime}/dotnet {tools}/aspire-nuget/aspire-nuget.dll search \
  --query "Aspire.Hosting" \
  --prerelease \
  --take 50 \
  --source https://api.nuget.org/v3/index.json \
  --format json

# Restore packages
{runtime}/dotnet {tools}/aspire-nuget/aspire-nuget.dll restore \
  --package "Aspire.Hosting.Redis" \
  --version "13.2.0" \
  --framework net10.0 \
  --output ~/.aspire/packages

# Create flat layout from restored packages
{runtime}/dotnet {tools}/aspire-nuget/aspire-nuget.dll layout \
  --assets ~/.aspire/packages/obj/project.assets.json \
  --output ~/.aspire/packages/libs \
  --framework net10.0
```

### Search Output Format

```json
{
  "packages": [
    {
      "id": "Aspire.Hosting.Redis",
      "version": "13.2.0",
      "allVersions": ["13.1.0", "13.2.0"],
      "description": "Redis hosting integration for .NET Aspire",
      "authors": ["Microsoft"],
      "source": "nuget.org",
      "deprecated": false
    }
  ],
  "totalHits": 42
}
```

### Package Cache Structure

```text
~/.aspire/packages/
├── aspire.hosting.redis/
│   └── 13.2.0/
│       ├── aspire.hosting.redis.13.2.0.nupkg
│       └── lib/
│           └── net10.0/
│               └── Aspire.Hosting.Redis.dll
├── aspire.hosting.valkey/
│   └── 13.2.0/
│       └── ...
└── libs/                               # Flat layout for probing
    ├── Aspire.Hosting.Redis.dll
    ├── Aspire.Hosting.Valkey.dll
    └── ...
```

---

## Certificate Management

The bundle includes the `dotnet-dev-certs` tool for HTTPS certificate management. This enables polyglot apphosts to configure HTTPS certificates without requiring a globally-installed .NET SDK.

### Dev-Certs Tool Usage

```bash
# Check certificate trust status (machine-readable output)
{runtime}/dotnet {tools}/dev-certs/dotnet-dev-certs.dll https --check --trust

# Trust the development certificate (requires elevation on some platforms)
{runtime}/dotnet {tools}/dev-certs/dotnet-dev-certs.dll https --trust
```

### Certificate Tool Abstraction

The CLI uses an `ICertificateToolRunner` abstraction to support both bundle and SDK modes:

| Mode | Implementation | Usage |
|------|----------------|-------|
| Bundle | `BundleCertificateToolRunner` | Uses bundled runtime + dev-certs.dll |
| SDK | `SdkCertificateToolRunner` | Uses `dotnet dev-certs` from global SDK |

The appropriate implementation is selected via DI based on whether a bundle layout is detected:

```csharp
services.AddSingleton<ICertificateToolRunner>(sp =>
{
    var layout = sp.GetService<LayoutConfiguration>();
    var devCertsPath = layout?.GetDevCertsDllPath();
    
    if (devCertsPath is not null && File.Exists(devCertsPath))
    {
        return new BundleCertificateToolRunner(layout!);
    }
    
    return new SdkCertificateToolRunner(sp.GetRequiredService<IDotNetCliRunner>());
});
```

---

## AppHost Server

### Pre-built vs Dynamic Mode

The bundle includes a pre-built AppHost Server with core hosting only (no integrations). All integrations are downloaded on-demand:

| Condition | Mode | Description |
|-----------|------|-------------|
| Bundle detected | **Pre-built + Dynamic Loading** | Use pre-built server, download integrations as needed |
| No bundle detected | **Dynamic** | Generate and build project (requires SDK) |

### Integration Download Flow

When a project references integrations (e.g., `Aspire.Hosting.Redis`):

1. CLI reads `.aspire/settings.json` for package list
2. CLI checks local cache (`~/.aspire/packages/`)
3. Missing packages are downloaded via NuGet Helper
4. Packages are extracted to flat layout for assembly loading
5. AppHost Server loads integration assemblies at startup

### Pre-built Mode Execution

```bash
# CLI spawns the pre-built AppHost Server
{aspire-server}/aspire-server \
  --project {user-project-path} \
  --socket {socket-path}
```

### Dynamic Integration Loading

When the user's project requires integrations not included in the bundle:

1. CLI downloads missing packages using NuGet Helper to a project-specific cache
2. AppHost Server receives the paths to restored assemblies via command line arguments
3. Assemblies are loaded using the standard .NET assembly loading mechanism

---

## CLI Integration

### Transparent Mode Detection

The CLI automatically detects whether to use bundle or SDK mode based on its execution context:

1. **Bundle mode**: CLI is running from within a bundle layout (detected via relative paths)
2. **SDK mode**: CLI is installed via `dotnet tool` or running standalone

No user configuration or flags are required - the experience is identical regardless of installation method.

### Self-Update Command

When running from a bundle, `aspire update --self` updates the bundle to the latest version:

```bash
# Update the bundle to the latest version
aspire update --self

# Check for updates without installing
aspire update --self --check
```

The update process:
1. Queries GitHub releases API for latest version
2. Downloads the appropriate platform-specific archive
3. Extracts to a temporary location
4. Replaces the current bundle (preserving user config)
5. Restarts the CLI if needed

When running via `dotnet tool`, `aspire update --self` displays instructions to use `dotnet tool update`.

### Mode Detection Algorithm

```csharp
bool ShouldUseBundleMode()
{
    // Check if explicitly disabled via environment variable
    var useSdk = Environment.GetEnvironmentVariable("ASPIRE_USE_GLOBAL_DOTNET");
    if (string.Equals(useSdk, "true", StringComparison.OrdinalIgnoreCase))
        return false;

    // Auto-detect: check if CLI is running from within a bundle layout
    var layoutPath = DiscoverRelativeLayout();
    if (layoutPath != null && ValidateLayout(layoutPath))
        return true;

    // Fall back to SDK mode
    return false;
}
```

### Environment Variable Override

For advanced scenarios (testing, debugging), a single environment variable can force SDK mode:

| Variable | Description |
|----------|-------------|
| `ASPIRE_USE_GLOBAL_DOTNET=true` | Force SDK mode even when running from bundle |

This is not documented for end users - it's for internal testing and edge cases only.

---

## Installation

### One-Line Install Scripts

**Linux/macOS (bash):**
```bash
curl -fsSL https://aka.ms/install-aspire.sh | bash
```

**Windows (PowerShell):**
```powershell
irm https://aka.ms/install-aspire.ps1 | iex
```

### Script Behavior

The install scripts:
1. Detect the current platform (OS + architecture)
2. Query GitHub releases for the latest bundle version
3. Download the appropriate archive
4. Extract to the default location:
   - Linux/macOS: `~/.aspire/bundle/`
   - Windows: `%USERPROFILE%\.aspire\bundle\`
5. Add to PATH (with user confirmation)
6. Verify installation with `aspire --version`

### Side-by-Side with Existing CLI

The bundle installs to a **separate subdirectory** from the existing CLI to allow both to coexist:

```text
~/.aspire/
├── bin/                    # Existing CLI (SDK-based, from get-aspire-cli-pr.sh)
│   └── aspire              #   - Requires .NET SDK for some operations
│                           #   - Uses NuGet packages for DCP/Dashboard
│
├── bundle/                 # Bundle CLI (self-contained, from get-aspire-cli-bundle-pr.sh)
│   ├── aspire              #   - Native AOT CLI executable
│   ├── layout.json         #   - Bundle configuration
│   ├── runtime/            #   - Bundled .NET runtime
│   │   └── dotnet
│   ├── dashboard/          #   - Pre-built Dashboard
│   │   └── aspire-dashboard
│   ├── dcp/                #   - Developer Control Plane
│   │   └── dcp
│   ├── aspire-server/     #   - Pre-built AppHost Server (polyglot)
│   │   └── aspire-server
│   └── tools/
│       └── aspire-nuget/   #   - NuGet operations without SDK
│           └── aspire-nuget
│
├── hives/                  # NuGet package hives (shared, preserved)
│   └── pr-{number}/
│       └── packages/
│
└── globalsettings.json     # Global CLI settings (shared, preserved)
```

**Key behaviors:**
- Installing the bundle only modifies `~/.aspire/bundle/` - other directories are untouched
- Existing CLI at `~/.aspire/bin/aspire` continues to work
- NuGet hives and settings are preserved across installations
- Users can switch between CLI and bundle by adjusting PATH priority

### Script Options

**Linux/macOS:**
```bash
# Install specific version
curl -fsSL https://aka.ms/install-aspire.sh | bash -s -- --version 13.2.0

# Install to custom location
curl -fsSL https://aka.ms/install-aspire.sh | bash -s -- --install-dir /opt/aspire

# Skip PATH modification
curl -fsSL https://aka.ms/install-aspire.sh | bash -s -- --no-path
```

**Windows:**
```powershell
# Install specific version
irm https://aka.ms/install-aspire.ps1 | iex -Args '--version', '13.2.0'

# Install to custom location  
irm https://aka.ms/install-aspire.ps1 | iex -Args '--install-dir', 'C:\aspire'
```

### Default Installation Locations

| Component | Linux/macOS | Windows |
|-----------|-------------|---------|
| Bundle CLI | `~/.aspire/bundle/aspire` | `%USERPROFILE%\.aspire\bundle\aspire.exe` |
| Existing CLI | `~/.aspire/bin/aspire` | `%USERPROFILE%\.aspire\bin\aspire.exe` |
| NuGet Hives | `~/.aspire/hives/` | `%USERPROFILE%\.aspire\hives\` |
| Settings | `~/.aspire/globalsettings.json` | `%USERPROFILE%\.aspire\globalsettings.json` |

### PR Build Installation

For testing PR builds before they are merged:

**Bundle from PR (self-contained):**
```bash
# Linux/macOS
./eng/scripts/get-aspire-cli-bundle-pr.sh 1234

# Windows
.\eng\scripts\get-aspire-cli-bundle-pr.ps1 -PRNumber 1234
```

**Existing CLI from PR (requires SDK):**
```bash
# Linux/macOS
./eng/scripts/get-aspire-cli-pr.sh 1234

# Windows
.\eng\scripts\get-aspire-cli-pr.ps1 -PRNumber 1234
```

---

## Configuration

Configuration is primarily done through environment variables. No user-editable configuration files are required.

### Environment Variable Precedence

```text
ASPIRE_* env vars > relative path auto-detect > assembly metadata (NuGet packages)
```

### Integration Cache

Downloaded integration packages are cached in:
- Linux/macOS: `~/.aspire/packages/`
- Windows: `%LOCALAPPDATA%\Aspire\packages\`

---

## Size and Distribution

### Size Estimates (Windows x64)

| Component | On Disk | Zipped |
|-----------|---------|--------|
| DCP (platform-specific) | ~286 MB | ~100 MB |
| .NET 10 Runtime (incl. ASP.NET Core) | ~200 MB | ~70 MB |
| Dashboard (framework-dependent) | ~43 MB | ~15 MB |
| CLI (native AOT) | ~22 MB | ~10 MB |
| AppHost Server (core only) | ~21 MB | ~8 MB |
| NuGet Helper (aspire-nuget) | ~5 MB | ~2 MB |
| Dev-certs Tool | ~0.1 MB | ~0.05 MB |
| **Total** | **~577 MB** | **~204 MB** |

*AppHost Server includes core hosting only - all integrations are downloaded on-demand.*
*Dashboard is framework-dependent (not self-contained), sharing the bundled .NET runtime.*
*Sizes vary by platform. Linux tends to be smaller than Windows.*

### Distribution Formats

| Platform | Format | Filename |
|----------|--------|----------|
| Windows x64 | ZIP | `aspire-13.2.0-win-x64.zip` |
| Linux x64 | tar.gz | `aspire-13.2.0-linux-x64.tar.gz` |
| Linux ARM64 | tar.gz | `aspire-13.2.0-linux-arm64.tar.gz` |
| macOS x64 | tar.gz | `aspire-13.2.0-osx-x64.tar.gz` |
| macOS ARM64 | tar.gz | `aspire-13.2.0-osx-arm64.tar.gz` |

### Download Locations

- **GitHub Releases**: `https://github.com/dotnet/aspire/releases`
- **aspire.dev**: Direct download links on documentation site

---

## Development Mode

When developing Aspire itself, the bundle mode is **not** used even if `ASPIRE_REPO_ROOT` is set. This ensures developers can:

1. Make changes to `Aspire.Hosting.*` assemblies
2. Use project references instead of pre-built binaries
3. See their changes reflected immediately without rebuilding a bundle

### How It Works

The layout discovery system detects development mode and creates a "dev layout" with `Version = "dev"`. When the CLI detects a dev layout, it falls back to the standard SDK-based flow:

```csharp
if (layout.IsDevLayout)
{
    // Continue using SDK mode with project references
    return false;
}
```

### Environment Variables for Development

| Variable | Description |
|----------|-------------|
| `ASPIRE_REPO_ROOT` | Path to local Aspire repo (triggers dev layout detection) |
| `ASPIRE_USE_GLOBAL_DOTNET=true` | Force SDK mode, skip bundle detection entirely |

### Testing Bundle Infrastructure

To test bundle infrastructure during development without affecting the normal dev workflow:

1. Build the aspire-server standalone: `dotnet build src/Aspire.Hosting.RemoteHost`
2. Create a test bundle layout manually with the built artifacts
3. Set `ASPIRE_LAYOUT_PATH` to point to your test layout
4. The dev layout detection only activates when `ASPIRE_REPO_ROOT` is set

---

## Security Considerations

### Package Signing

| Platform | Mechanism |
|----------|-----------|
| Windows | Authenticode signature on CLI executable |
| macOS | Notarization + code signing |
| Linux | GPG signature file (`.asc`) |

### Checksum Verification

Each release includes SHA256 checksums:

```text
aspire-13.2.0-linux-x64.tar.gz.sha256
aspire-13.2.0-win-x64.zip.sha256
```

### Runtime Isolation

The bundled .NET runtime is isolated from any globally-installed .NET:

- `DOTNET_ROOT` is set to the bundle's runtime directory
- `DOTNET_MULTILEVEL_LOOKUP=0` disables global probing
- No modification to system PATH or environment

### NuGet Security

- Package downloads use HTTPS only
- Package signatures are verified when available
- Authenticated feeds require explicit credential configuration

---

## Backward Compatibility

A core design principle of the bundle feature is **complete backward compatibility**. Users with existing workflows must not experience any breaking changes.

### Compatibility Requirements

1. **Existing SDK-based workflows continue to work unchanged**
   - If the .NET SDK is installed globally, all existing commands work identically
   - No new CLI flags required to use existing functionality
   - `aspire new`, `aspire add`, `aspire run` behave the same as before

2. **Dotnet tool installation remains supported**
   - `dotnet tool install -g Aspire.Cli` continues to work
   - `aspire update --self` shows `dotnet tool update` instructions when running as a tool

3. **Bundle mode is transparent**
   - No user action required to switch between bundle and SDK mode
   - CLI auto-detects which mode to use based on installation location
   - All commands produce the same user-visible output regardless of mode

### Detection Logic

The CLI determines its execution mode using this priority order:

```text
1. ASPIRE_USE_GLOBAL_DOTNET=true → Force SDK mode (for testing/debugging)
2. ASPIRE_REPO_ROOT is set       → Dev mode (use SDK with project refs)
3. Valid bundle layout found     → Bundle mode
4. .NET SDK available globally   → SDK mode
5. Neither available             → Error with installation instructions
```

### API Compatibility

Changes to internal CLI classes maintain backward compatibility through:

1. **New dependencies are optional or have sensible defaults**
   ```csharp
   // IBundleDownloader is nullable - if not registered, bundle update is skipped
   private readonly IBundleDownloader? _bundleDownloader;
   
   // ILayoutDiscovery always returns null if no layout found - SDK mode continues
   var layout = _layoutDiscovery.DiscoverLayout();
   if (layout is null) { /* fall back to SDK mode */ }
   ```

2. **DI registration is additive**
   - New services are registered alongside existing ones
   - Tests using full DI container continue to work
   - Tests mocking specific services are unaffected

3. **Graceful degradation**
   - If bundle components are missing, fall back to SDK
   - If NuGetHelper is unavailable, fall back to `dotnet` commands
   - Error messages guide users to resolution

### Test Compatibility

Tests continue to work because:

1. **Integration tests use the full DI container**
   - New services (`ILayoutDiscovery`, `IBundleDownloader`) are registered
   - Tests discover no layout → SDK mode is used → existing behavior

2. **Unit tests mock at service boundaries**
   - Tests mocking `IProjectLocator`, `IPackagingService` etc. are unaffected
   - New services can be mocked independently if needed

3. **Test helpers register all services**
   - `CliTestHelper.CreateServiceCollection()` uses the same registration as production
   - No test-specific configuration needed for backward compatibility

### Environment Variable Summary

| Variable | Purpose | Effect |
|----------|---------|--------|
| `ASPIRE_USE_GLOBAL_DOTNET=true` | Force SDK mode | Bypasses bundle detection entirely |
| `ASPIRE_REPO_ROOT` | Development mode | Uses SDK with project references |
| `ASPIRE_LAYOUT_PATH` | Bundle location | Overrides auto-detection |
| `ASPIRE_DCP_PATH` | DCP override | Works in both modes |
| `ASPIRE_DASHBOARD_PATH` | Dashboard override | Works in both modes |
| `ASPIRE_RUNTIME_PATH` | .NET runtime override | For guest apphosts only |

### Migration Path

Users migrating from SDK-based installation to bundle:

1. **No migration required** - existing projects work with bundle CLI
2. **Package references unchanged** - same NuGet packages, same versions
3. **Configuration preserved** - `~/.aspire/` settings continue to work
4. **Can switch back anytime** - reinstall via `dotnet tool` to return to SDK mode

### Version Compatibility

A key design principle is that the CLI and AppHost can be updated independently:

#### CLI Updated, AppHost Unchanged

When using a newer CLI with an older AppHost (NuGet packages):

1. **Protocol stability** - The JSON-RPC protocol between CLI and AppHost is versioned
2. **Feature detection** - CLI queries AppHost capabilities before using new features
3. **Graceful fallback** - Unknown features are skipped, core functionality preserved
4. **Package resolution** - NuGet packages from older Aspire versions continue to work

```text
CLI v10.0 ────► AppHost (Aspire.Hosting v9.x)
    │
    └── Uses SDK mode to build project with v9.x packages
        Works identically to v9.x CLI
```

#### AppHost Updated, CLI Unchanged

When using an older CLI with newer AppHost packages:

1. **Forward compatibility** - Older CLI can run newer AppHost projects
2. **New features unavailable** - Features requiring CLI support won't work
3. **Clear error messages** - When incompatibility detected, show upgrade guidance
4. **Core functionality works** - `aspire run`, `aspire add` continue to function

```text
CLI v9.x ────► AppHost (Aspire.Hosting v10.x)
    │
    └── Builds and runs project
        New v10 features that need CLI support are unavailable
        User sees: "Upgrade CLI for new features: aspire update --self"
```

#### Bundle Updated, SDK-based AppHost

When using bundle CLI with SDK-installed Aspire packages:

1. **Mode detection** - Bundle CLI detects SDK is available
2. **SDK mode activation** - Uses `dotnet build` for AppHost, not pre-built server
3. **Identical behavior** - Works exactly like dotnet-tool-installed CLI
4. **No conflicts** - Bundle runtime isolated from global .NET

```text
Bundle CLI ────► AppHost (via SDK)
    │
    └── Detects .NET SDK is installed
        Falls back to SDK mode
        Uses dotnet build, not pre-built server
```

#### Version Mismatch Handling

```csharp
// CLI checks AppHost protocol version
var serverVersion = await appHost.GetProtocolVersionAsync();
if (serverVersion < MinSupportedVersion)
{
    // Show upgrade message but continue if possible
    InteractionService.DisplayMessage("warning", 
        $"AppHost uses protocol v{serverVersion}, CLI expects v{MinSupportedVersion}+. " +
        "Some features may not work. Consider updating packages.");
}
```

---

## Future Considerations

### Out of Scope for Initial Release

- **Auto-update mechanism**: Users manually download new versions
- **Minimal bundle variant**: Full bundle only, no on-demand component download
- **Template creation**: `aspire new` for .NET templates still requires SDK

### Potential Enhancements

1. **Modular bundles**: Base + optional integration packs
2. **CDN distribution**: Faster downloads via global CDN
3. **Update command**: `aspire update --self` for bundle updates (implemented)
4. **Bundle compression**: Support for zstd in ZIP format (better ratios)
5. **Single-file runtime bundle**: Consolidate runtime folder into single file (see below)

### Single-File Runtime Bundle (Future Option)

The current bundle layout includes a `runtime/` folder (~106 MB) containing the .NET runtime:

```text
runtime/
├── dotnet.exe                 # Host/muxer
├── host/fxr/{version}/        # hostfxr
└── shared/
    ├── Microsoft.NETCore.App/{version}/
    └── Microsoft.AspNetCore.App/{version}/
```

A future enhancement could consolidate this into a **single-file runtime binary** using the `Microsoft.NET.HostModel.Bundle` API. This would:

1. Create a single `dotnet-aspire` executable containing:
   - The apphost (native executable stub)
   - hostfxr and hostpolicy (statically linked or bundled)
   - Microsoft.NETCore.App framework assemblies
   - Microsoft.AspNetCore.App framework assemblies

2. Use .NET's bundle format which:
   - Memory-maps managed assemblies directly from the bundle (no extraction)
   - Extracts only native libraries to a temp directory when needed
   - Caches extracted files across runs

#### Proposed Implementation

**Step 1: Create a minimal host application**

```csharp
// tools/AspireRuntimeHost/Program.cs
// Minimal app that forwards execution to the target DLL
public class Program
{
    public static int Main(string[] args)
    {
        // The actual DLL to run is passed as first argument
        // This app just provides the runtime context
        return 0;
    }
}
```

**Step 2: Publish as single-file with shared frameworks**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  </PropertyGroup>
</Project>
```

**Step 3: Use the Bundler API programmatically**

```csharp
using Microsoft.NET.HostModel.Bundle;

var bundler = new Bundler(
    hostName: "dotnet-aspire",
    outputDir: outputPath,
    options: new BundleOptions(
        targetOS: targetOS,
        targetArch: targetArch,
        enableCompression: true));

// Add framework assemblies
foreach (var dll in frameworkAssemblies)
{
    bundler.AddToBundle(dll, BundlerFileType.Assembly);
}

// Add native libraries  
foreach (var native in nativeLibs)
{
    bundler.AddToBundle(native, BundlerFileType.NativeBinary);
}

// Generate the bundle
bundler.GenerateBundle();
```

#### Bundle Structure

The resulting `dotnet-aspire` binary would have this internal structure:

```text
dotnet-aspire (single file, ~100-120 MB)
├── [AppHost Header]
├── [hostfxr + hostpolicy code]
├── [Bundle Manifest]
│   ├── File count, offsets, sizes
│   └── Compression metadata
├── [Framework Assemblies - Memory Mapped]
│   ├── System.Runtime.dll
│   ├── System.Collections.dll
│   ├── Microsoft.AspNetCore.*.dll
│   └── ... (~800 assemblies)
└── [Native Libraries - Extracted on demand]
    ├── coreclr.dll/libcoreclr.so
    ├── clrjit.dll/libclrjit.so
    └── System.*.Native.dll/so
```

#### Runtime Behavior

1. **First run**: Native libraries extracted to `~/.aspire/runtime-cache/{bundle-hash}/`
2. **Subsequent runs**: Cache hit, no extraction needed
3. **Managed code**: Memory-mapped directly from bundle, no disk I/O

#### Usage in CLI

```csharp
// LayoutProcessRunner would use the single-file runtime
public async Task<int> RunAsync(string dllPath, string[] args)
{
    var runtimePath = _layout.GetSingleFileRuntime(); // "dotnet-aspire"
    
    var process = new Process();
    process.StartInfo.FileName = runtimePath;
    process.StartInfo.ArgumentList.Add("exec");
    process.StartInfo.ArgumentList.Add(dllPath);
    // ...
}
```

#### Estimated Sizes

| Component | Current | Single-File |
|-----------|---------|-------------|
| runtime/ folder | 106 MB | - |
| dotnet-aspire binary | - | ~100-120 MB |
| **Net change** | - | ~0-15 MB smaller |

The main benefit is **simplicity** (one file vs folder tree) rather than size reduction.

#### Trade-offs

**Pros:**
- Single file instead of ~200 files in runtime/ folder
- Simpler xcopy deployment
- Managed assemblies load faster (memory-mapped)
- No need to manage runtime folder structure

**Cons:**
- Native libraries still extract to disk (required by OS loader)
- More complex build process
- Harder to debug/inspect
- Updates require full binary replacement

#### Implementation Effort

- **Low**: Self-extracting archive (compress runtime/, extract on first use)
- **Medium**: Use existing single-file publish infrastructure
- **High**: Custom Bundler integration with proper framework resolution

#### Dependencies

- `Microsoft.NET.HostModel` NuGet package (contains Bundler API)
- Understanding of deps.json and runtimeconfig.json generation
- Platform-specific native library handling

---

## Implementation Status

This section tracks the implementation progress of the bundle feature.

### Completed

- [x] **Specification document** - This document (`docs/specs/bundle.md`)
- [x] **Layout configuration classes** - `src/Aspire.Cli/Layout/LayoutConfiguration.cs`
- [x] **Layout discovery service** - `src/Aspire.Cli/Layout/LayoutDiscovery.cs`
- [x] **Layout process runner** - `src/Aspire.Cli/Layout/LayoutProcessRunner.cs`
- [x] **Bundle NuGet service** - `src/Aspire.Cli/NuGet/BundleNuGetService.cs`
- [x] **NuGet Helper tool** - `src/Aspire.Cli.NuGetHelper/`
  - [x] Search command (NuGet v3 HTTP API)
  - [x] Restore command (NuGet RestoreRunner)
  - [x] Layout command (flat DLL layout from project.assets.json)
- [x] **Layout services registered in DI** - `src/Aspire.Cli/Program.cs`
- [x] **Pre-built AppHost server class** - `src/Aspire.Cli/Projects/PrebuiltAppHostServer.cs`
- [x] **DCP/Dashboard/Runtime env var support** - `src/Aspire.Hosting/Dcp/DcpOptions.cs`, `src/Aspire.Hosting/Dashboard/DashboardEventHandlers.cs`
  - `ASPIRE_DCP_PATH` environment variable
  - `ASPIRE_DASHBOARD_PATH` environment variable
  - `ASPIRE_RUNTIME_PATH` environment variable (guest apphosts only)
- [x] **Shared discovery logic** - `src/Shared/BundleDiscovery.cs`
  - `TryDiscoverDcpFromEntryAssembly()` / `TryDiscoverDcpFromDirectory()`
  - `TryDiscoverDashboardFromEntryAssembly()` / `TryDiscoverDashboardFromDirectory()`
  - `TryDiscoverRuntimeFromEntryAssembly()` / `TryDiscoverRuntimeFromDirectory()`
  - `GetDotNetExecutablePath()` - env → disk → PATH fallback
- [x] **GuestAppHostProject bundle mode integration** - `src/Aspire.Cli/Projects/GuestAppHostProject.cs`
  - Automatic bundle mode detection via `TryGetBundleLayout()`
  - `PrepareSdkModeAsync()` for traditional SDK-based server build
  - `PrepareBundleModeAsync()` for pre-built server from bundle
- [x] **Standalone aspire-server project** - `src/Aspire.Hosting.RemoteHost/`
  - Pre-built server for bundle distribution
  - Framework-dependent deployment (uses bundled runtime)
- [x] **Certificate management** - `src/Aspire.Cli/Certificates/`
  - `ICertificateToolRunner` abstraction
  - `BundleCertificateToolRunner` - uses bundled runtime + dev-certs.dll
  - `SdkCertificateToolRunner` - uses global `dotnet dev-certs`
- [x] **Bundle build tooling** - `tools/CreateLayout/`
  - Downloads .NET SDK and extracts runtime + dev-certs
  - Copies DCP, Dashboard, aspire-server, NuGetHelper
  - Generates layout.json metadata
  - Enables RollForward=Major for all managed tools

### In Progress

- [ ] Integrate NuGet service with AddCommand

### Pending

- [ ] Self-update command (`aspire update --self`) - BundleDownloader exists but not wired
- [ ] Installation scripts (bash/PowerShell)
- [ ] Multi-platform build workflow (GitHub Actions)

### Key Files

| File | Purpose |
|------|---------|
| `src/Aspire.Cli/Layout/LayoutConfiguration.cs` | Configuration classes for layout structure |
| `src/Aspire.Cli/Layout/LayoutDiscovery.cs` | Priority-based layout discovery (env > config > relative) |
| `src/Aspire.Cli/Layout/LayoutProcessRunner.cs` | Run managed DLLs via layout's .NET runtime |
| `src/Aspire.Cli/NuGet/BundleNuGetService.cs` | NuGet operations wrapper for bundle mode |
| `src/Aspire.Cli.NuGetHelper/` | Managed tool for search/restore/layout |
| `src/Aspire.Cli/Projects/PrebuiltAppHostServer.cs` | Bundle-mode server runner |
| `src/Aspire.Cli/Projects/GuestAppHostProject.cs` | Main polyglot handler with bundle/SDK mode switching |
| `src/Aspire.Hosting/Dcp/DcpOptions.cs` | DCP/Dashboard path resolution with env var support |
| `src/Aspire.Cli/Certificates/ICertificateToolRunner.cs` | Certificate tool abstraction |
| `src/Aspire.Cli/Certificates/BundleCertificateToolRunner.cs` | Bundled dev-certs runner |
| `src/Aspire.Cli/Certificates/SdkCertificateToolRunner.cs` | SDK-based dev-certs runner |
| `tools/CreateLayout/Program.cs` | Bundle build tool |

---

## Build Process

The bundle is built using the `tools/CreateLayout` tool, which assembles all components into the final bundle layout.

### SDK Download Approach

The bundle's .NET runtime is extracted from the official .NET SDK, which provides several advantages:

1. **Single download**: The SDK contains the runtime, ASP.NET Core framework, and dev-certs tool
2. **Version consistency**: All components come from the same SDK release
3. **Official source**: Direct from Microsoft's build infrastructure

```text
SDK download (~200 MB)
├── dotnet.exe                              → runtime/dotnet.exe
├── host/                                   → runtime/host/
├── shared/Microsoft.NETCore.App/10.0.x/    → runtime/shared/Microsoft.NETCore.App/
├── shared/Microsoft.AspNetCore.App/10.0.x/ → runtime/shared/Microsoft.AspNetCore.App/
├── sdk/10.0.x/DotnetTools/dotnet-dev-certs → tools/dev-certs/
└── (discard: sdk/, templates/, packs/, etc.)
```

The SDK version is discovered dynamically from `https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json`.

### RollForward Configuration

All managed tools in the bundle are configured with `rollForward: Major` in their runtimeconfig.json files. This allows:

- Tools built for .NET 8.0 or 9.0 to run on the bundled .NET 10+ runtime
- Maximum compatibility with older Aspire.Hosting packages
- Simpler bundle maintenance (single runtime version)

The CreateLayout tool automatically patches all `*.runtimeconfig.json` files:

```json
{
  "runtimeOptions": {
    "rollForward": "Major",
    "framework": {
      "name": "Microsoft.AspNetCore.App",
      "version": "8.0.0"
    }
  }
}
```

### Build Steps

1. **Download .NET SDK** for the target platform
2. **Extract runtime components** (muxer, host, shared frameworks)
3. **Extract dev-certs tool** from `sdk/*/DotnetTools/dotnet-dev-certs/`
4. **Build and copy managed tools** (aspire-server, aspire-dashboard, NuGetHelper)
5. **Download and copy DCP** binaries
6. **Patch runtimeconfig.json files** to enable RollForward=Major
7. **Generate layout.json** with component metadata
8. **Create archive** (ZIP for Windows, tar.gz for Unix)
