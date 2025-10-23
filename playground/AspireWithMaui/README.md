# AspireWithMaui Playground

This playground demonstrates .NET Aspire integration with .NET MAUI applications.

## Prerequisites

- .NET 10 or later
- .NET MAUI workload

## Getting Started

### Initial Setup

Before building or running the playground, you must restore dependencies and install the MAUI workload.

Run the following commands from the repository root:

**Windows:**
```cmd
.\restore.cmd -restore-maui
```

**Linux/macOS:**
```bash
./restore.sh --restore-maui
```

This will:
1. Restore all Aspire dependencies and set up the local .dotnet SDK
2. Install the MAUI workload into the repository's local `.dotnet` folder (does not affect your global installation)

> **Note:** The MAUI workload is installed only in the repository's local `.dotnet` folder and will not interfere with your system-wide .NET installation.
> This also means that you will still need to do this even if you have the MAUI workload already installed in your system-wide .NET installation.

### Running the Playground

After running the restore script with `-restore-maui`, you can build and run the playground:

**Using Visual Studio:**
1. Run `.\restore.cmd -restore-maui` from the repository root (Windows)
2. Open `AspireWithMaui.AppHost` project
3. Set it as the startup project
4. Press F5 to run

**Using VS Code:**
1. Run `.\restore.cmd -restore-maui` (Windows) or `./restore.sh --restore-maui` (Linux/macOS) from the repository root
2. From the repository root, run: `./start-code.sh` or `start-code.cmd`
3. Open the `AspireWithMaui` folder
4. Use the debugger to run the AppHost

**Using Command Line:**
1. Run `.\restore.cmd -restore-maui` (Windows) or `./restore.sh --restore-maui` (Linux/macOS) from the repository root
2. Navigate to `playground/AspireWithMaui/AspireWithMaui.AppHost` directory
3. Run: `dotnet run`

## What's Included

- **AspireWithMaui.AppHost** - The Aspire app host that orchestrates all services
- **AspireWithMaui.MauiClient** - A .NET MAUI application that connects to the backend (Windows platform only in this playground)
- **AspireWithMaui.WeatherApi** - An ASP.NET Core Web API providing weather data
- **AspireWithMaui.ServiceDefaults** - Shared service defaults for non-MAUI projects
- **AspireWithMaui.MauiServiceDefaults** - Shared service defaults specific to MAUI projects

## Features Demonstrated

### MAUI Windows Platform Support
The playground demonstrates Aspire's ability to manage MAUI apps on Windows:
- Configures the MAUI app with `.AddMauiWindows()`
- Automatically detects the Windows target framework from the project file
- Sets up dev tunnels for MAUI app communication with backend services

### OpenTelemetry Integration
The MAUI client uses OpenTelemetry to send traces and metrics to the Aspire dashboard via dev tunnels.

### Service Discovery
The MAUI app discovers and connects to backend services (WeatherApi) using Aspire's service discovery.

### Future Platform Support
The architecture is designed to support additional platforms (Android, iOS, macCatalyst) through:
- `.AddMauiAndroid()`, `.AddMauiIos()`, `.AddMauiMacCatalyst()` extension methods (coming in future updates)
- Parallel extension patterns for each platform

## Troubleshooting

### "MAUI workload not detected" Warning
If you see this warning in the Aspire dashboard:
1. Make sure you ran `.\restore.cmd -restore-maui` or `./restore.sh --restore-maui` from the repository root
2. The warning indicates the MAUI workload is not installed in the local `.dotnet` folder
3. Re-run the restore command with the `-restore-maui` or `--restore-maui` flag

### Build Errors
If you encounter build errors:
1. Ensure you ran the restore script with the MAUI flag first: `.\restore.cmd -restore-maui`
2. Make sure you're using .NET 10 RC or later
3. Try running `dotnet build` from the repository root first

### Platform-Specific Issues
- **Windows**: Requires Windows 10 build 19041 or higher for WinUI support
- **Android**: Not yet implemented in this playground (coming soon)
- **iOS/macCatalyst**: Not yet implemented in this playground (coming soon)

## Current Status

✅ **Implemented:**
- Windows platform support via `AddMauiWindows()`
- Automatic Windows TFM detection from project file
- Dev tunnel configuration for MAUI-to-backend communication
- Service discovery integration
- OpenTelemetry integration

🚧 **Coming Soon:**
- Android platform support
- iOS platform support
- macCatalyst platform support
- Multi-platform simultaneous debugging

## Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [OpenTelemetry in .NET](https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel)
