# Vendoring code sync instructions

## OpenTelemetry.Shared

```console
git clone https://github.com/open-telemetry/opentelemetry-dotnet.git
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
```

### Instructions

- Copy files from `src/OpenTelemetry.Instrumentation.SqlClient`:
    - `OpenTelemetry.Instrumentation.SqlClient.csproj`
    - `**\*.cs`

### Customizations

- Added `#nullable disable` in files that require it.
- Added `GlobalSuppressions.cs` to fix incompatible coding style.
