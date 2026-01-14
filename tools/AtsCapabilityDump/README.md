# AtsCapabilityDump

A tool for dumping ATS (Aspire Type System) capabilities from Aspire assemblies to a file for tracking and diffing.

## Overview

This tool scans Aspire assemblies for `[AspireExport]` attributes and generates a report of all discovered capabilities, types, and DTOs. The output is similar to the `api/*.cs` files that track the public API surface, but for the polyglot/ATS capability surface.

## Usage

```bash
# Scan Aspire.Hosting and output to stdout (default)
dotnet run --project tools/AtsCapabilityDump

# Output to a file
dotnet run --project tools/AtsCapabilityDump -- -o capabilities.txt

# Output in JSON format
dotnet run --project tools/AtsCapabilityDump -- -f json -o capabilities.json

# Scan additional assemblies
dotnet run --project tools/AtsCapabilityDump -- -a path/to/assembly.dll -o capabilities.txt

# Don't include Aspire.Hosting (only scan specified assemblies)
dotnet run --project tools/AtsCapabilityDump -- --scan-hosting false -a path/to/assembly.dll
```

## Options

| Option | Description | Default |
|--------|-------------|---------|
| `-a, --assembly` | Path(s) to assembly file(s) to scan | None |
| `-o, --output` | Output file path | stdout |
| `-f, --format` | Output format (text or json) | text |
| `--scan-hosting` | Include Aspire.Hosting assembly | true |

## Output Format

### Text Format

The text format is designed for easy diffing:

```text
// Handle Types (passed by reference)
// -----------------------------------
// Type: Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder
//   Kind: Interface
//   ExposeProperties: true

// Capabilities
// ------------

// Target: Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder
// {
//   Aspire.Hosting/addContainer
//     Description: Adds a container resource
//     Kind: Method
//     Parameters: (builder: Aspire.Hosting/..., name: string, image: string)
//     Returns: Aspire.Hosting/...
// }
```

### JSON Format

The JSON format is suitable for programmatic consumption and can be used by code generators or other tools.

## Use Cases

1. **Tracking capability changes**: Commit the output file to source control and use diffs to track changes to the polyglot API surface.

2. **Code generation validation**: Verify that code generators are producing correct output by comparing against the capability dump.

3. **Documentation**: Generate documentation of available capabilities for non-.NET language developers.

4. **API compatibility**: Detect breaking changes to the polyglot API surface during code review.
