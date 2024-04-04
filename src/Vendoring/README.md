# Vendoring code sync instructions

## OpenTelemetry.Instrumentation.SqlClient

```console
git clone https://github.com/open-telemetry/opentelemetry-dotnet.git
git fetch --tags
git checkout tags/Instrumentation.SqlClient-1.7.0-beta.1
```

### Instructions

- Copy files from `src/OpenTelemetry.Instrumentation.SqlClient` to `src/Vendoring/OpenTelemetry.Instrumentation.SqlClient`:
    - `**\*.cs` minus `AssemblyInfo.cs`
- Update `SqlActivitySourceHelper` with:
  ```csharp
  public const string ActivitySourceName = "OpenTelemetry.Instrumentation.SqlClient";
  public static readonly Version Version = new Version(1, 7, 0, 1173);
  ```
- Copy files from `src/Shared` to `src/Vendoring/OpenTelemetry.Instrumentation.SqlClient/Shared`:
    - `DiagnosticSourceInstrumentation\*.cs`
    - `ExceptionExtensions.cs`
    - `Guard.cs`
    - `SemanticConventions.cs`

## OpenTelemetry.Instrumentation.StackExchangeRedis

```console
git clone https://github.com/open-telemetry/opentelemetry-dotnet-contrib.git
git fetch --tags
git checkout tags/Instrumentation.StackExchangeRedis-1.0.0-rc9.13
```

### Instructions

- Copy files from `src/OpenTelemetry.Instrumentation.StackExchangeRedis` to `src/Vendoring/OpenTelemetry.Instrumentation.StackExchangeRedis`:
    - `**\*.cs` minus `AssemblyInfo.cs`
- Copy files from `src/Shared` to `src/Vendoring/OpenTelemetry.Instrumentation.StackExchangeRedis/Shared`:
    - `Guard.cs`
    - `PropertyFetcher.AOT.cs`
    - `SemanticConventions.cs`
- In `StackExchangeRedisConnectionInstrumentation.cs` update `ActivitySourceName` to `internal const string ActivitySourceName = "OpenTelemetry.Instrumentation.StackExchangeRedis";` and `Version` to `internal static readonly Version Version = new Version(1, 0, 0, 13);`
- Apply the changes from https://github.com/open-telemetry/opentelemetry-dotnet-contrib/pull/1625 if necessary.

## Customizations

- Add `#nullable disable` in files that require it.
- Change all `public` classes to `internal`.
- Update `src/Vendoring/.editorconfig` with the required exemptions.
