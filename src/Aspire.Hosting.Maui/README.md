# Aspire.Hosting.Maui

This library provides support for running .NET MAUI applications within an Aspire application model. It enables local development and debugging of MAUI apps alongside other services in your distributed application.

## Getting Started

### Adding a MAUI Application

Add a MAUI project to your Aspire app host:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var mauiApp = builder.AddMauiProject("mauiapp", "../MauiApp/MauiApp.csproj");
```

### Adding Platform Targets

Add specific platform targets for your MAUI application:

```csharp
// Windows
mauiApp.AddWindowsDevice();

// macOS Catalyst
mauiApp.AddMacCatalystDevice();

// Android Device
mauiApp.AddAndroidDevice();

// Android Emulator
mauiApp.AddAndroidEmulator();
```

You can optionally specify custom names:

```csharp
mauiApp.AddWindowsDevice("my-windows-app");
mauiApp.AddAndroidEmulator("pixel-7-emulator");
```

## OpenTelemetry Connectivity for Mobile Platforms

Mobile devices, Android emulators, and iOS simulators cannot reach `localhost` where the Aspire dashboard's OTLP endpoint typically runs. This library provides a simple way to configure OpenTelemetry connectivity using dev tunnels.

### Using Dev Tunnel

Automatically create and configure a dev tunnel for the dashboard's OTLP endpoint, this is needed when running a .NET MAUI app on an Android emulator/device or iOS Simulator/device. By default, Aspire will send the OpenTelemetry data back to localhost, however localhost is different when running on a emulator/Simulator/device.

You should not need to use this when running on Windows or Mac Catalyst.

```csharp
// Automatically creates a dev tunnel for OTLP
mauiApp.AddAndroidEmulator()
    .WithOtlpDevTunnel();
```

When `WithOtlpDevTunnel()` is not added, things will still work, however tracing, metrics and telemetry data will not be complete.

This method automatically:
- Resolves the dashboard's OTLP endpoint from configuration
- Creates a dev tunnel for it
- Configures the MAUI platform to use the tunneled endpoint
- Handles all service discovery and environment variable configuration

**Requirements:**
- Aspire.Hosting.DevTunnels package must be referenced
- Dev tunnel CLI must be installed (automatic prompt if missing)
- User must be logged in to dev tunnel service (automatic prompt if needed)

### Environment Variables Set

When you configure OTLP with dev tunnel, the following environment variables are automatically set:

- `OTEL_EXPORTER_OTLP_ENDPOINT`: The dev tunnel URL for the OTLP endpoint
- `OTEL_EXPORTER_OTLP_PROTOCOL`: Set to `grpc` (standard Aspire configuration)
- `OTEL_SERVICE_NAME`: The resource name
- `OTEL_RESOURCE_ATTRIBUTES`: Service instance ID

## Example: Complete Aspire App with MAUI

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add backend services
var apiService = builder.AddProject<Projects.ApiService>("apiservice");

// Create a dev tunnel for the API service
var apiTunnel = builder.AddDevTunnel("api-tunnel")
    .WithAnonymousAccess()
    .WithReference(apiService.GetEndpoint("https"));

// Add MAUI app with multiple platform targets
var mauiApp = builder.AddMauiProject("mauiapp", "../MauiApp/MauiApp.csproj");

// Windows - can access localhost directly
mauiApp.AddWindowsDevice()
    .WithReference(apiService);

// Android Emulator - needs dev tunnels for both API and OTLP
mauiApp.AddAndroidEmulator()
    .WithOtlpDevTunnel() // For telemetry
    .WithReference(apiService, apiTunnel); // For API calls

// Android Device - same configuration
mauiApp.AddAndroidDevice()
    .WithOtlpDevTunnel()
    .WithReference(apiService, apiTunnel);

builder.Build().Run();
```

## Requirements

- .NET 10.0 or later
- MAUI workload must be installed: `dotnet workload install maui`
- Platform-specific SDKs:
  - Windows: Windows SDK 10.0.19041.0 or later
  - macOS: Xcode with command-line tools
  - Android: Android SDK via Visual Studio or Android Studio

## Feedback & Issues

Please file issues at https://github.com/dotnet/aspire/issues
