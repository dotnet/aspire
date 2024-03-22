# Vendoring code sync instructions

## OpenTelemetry.Shared

```console
git clone https://github.com/open-telemetry/opentelemetry-dotnet.git
git fetch --tags
git checkout tags/Instrumentation.SqlClient-1.7.0-beta.1
```

### Instructions

- Copy required files from `src/Shared`:
    - `DiagnosticSourceInstrumentation\*.cs`
    - `ExceptionExtensions.cs`
    - `Guard.cs`
    - `SemanticConventions.cs`

## OpenTelemetry.Instrumentation.SqlClient

```console
git clone https://github.com/open-telemetry/opentelemetry-dotnet.git
git fetch --tags
git checkout tags/Instrumentation.SqlClient-1.7.0-beta.1
```

### Instructions

- Copy files from `src/OpenTelemetry.Instrumentation.SqlClient`:
    - `**\*.cs`

## OpenTelemetry.Instrumentation.EventCounters

```console
git clone https://github.com/open-telemetry/opentelemetry-dotnet-contrib.git
git fetch --tags
git checkout tags/Instrumentation.EventCounters-1.5.1-alpha.1
```

### Instructions

- Copy files from `src/OpenTelemetry.Instrumentation.EventCounters`:
    - `**\*.cs`

## Customizations

- Added `#nullable disable` in files that require it.
- Change all `public` classes to `internal`.
- Update `src/Vendoring/.editorconfig` with the required exemptions.
