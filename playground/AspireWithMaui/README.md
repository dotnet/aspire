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
.\restore.cmd -mauirestore
```

**Linux/macOS:**
```bash
./restore.sh --mauirestore
```

This will:
1. Restore all Aspire dependencies and set up the local .dotnet SDK
2. Install the MAUI workload into the repository's local `.dotnet` folder (does not affect your global installation)

> **Note:** The MAUI workload is installed only in the repository's local `.dotnet` folder and will not interfere with your system-wide .NET installation.
> This also means that you will still need to do this even if you have the MAUI workload already installed in your system-wide .NET installation.

### Running the Playground

After running the restore script with `-mauirestore`, you can build and run the playground:

**Using Visual Studio:**
1. Run `.\restore.cmd -mauirestore` from the repository root (Windows)
2. Open `AspireWithMaui.AppHost` project
3. Set it as the startup project
4. Press F5 to run

**Using VS Code:**
1. Run `.\restore.cmd -mauirestore` (Windows) or `./restore.sh --mauirestore` (Linux/macOS) from the repository root
2. From the repository root, run: `./start-code.sh` or `start-code.cmd`
3. Open the `AspireWithMaui` folder
4. Use the debugger to run the AppHost

**Using Command Line:**
1. Run `.\restore.cmd -mauirestore` (Windows) or `./restore.sh --mauirestore` (Linux/macOS) from the repository root
2. Navigate to `playground/AspireWithMaui/AspireWithMaui.AppHost` directory
3. Run: `dotnet run`

## What's Included

- **AspireWithMaui.AppHost** - The Aspire app host that orchestrates all services
- **AspireWithMaui.MauiClient** - A .NET MAUI application that connects to the backend (Windows and Mac Catalyst platforms)
- **AspireWithMaui.WeatherApi** - An ASP.NET Core Web API providing weather data
- **AspireWithMaui.ServiceDefaults** - Shared service defaults for non-MAUI projects
- **AspireWithMaui.MauiServiceDefaults** - Shared service defaults specific to MAUI projects

## Features Demonstrated

### MAUI Multi-Platform Support
The playground demonstrates Aspire's ability to manage MAUI apps on multiple platforms:
- **Windows**: Configures the MAUI app with `.AddWindowsDevice()`
- **Mac Catalyst**: Configures the MAUI app with `.AddMacCatalystDevice()`
- Automatically detects platform-specific target frameworks from the project file
- Shows "Unsupported" state in dashboard when running on incompatible host OS
- Sets up dev tunnels for MAUI app communication with backend services

### OpenTelemetry Integration
The MAUI client uses OpenTelemetry to send traces and metrics to the Aspire dashboard via dev tunnels.

### Service Discovery
The MAUI app discovers and connects to backend services (WeatherApi) using Aspire's service discovery.

### Future Platform Support
The architecture is designed to support additional platforms (Android, iOS) through:
- `.AddAndroidDevice()`, `.AddIosDevice()` extension methods (coming in future updates)
- Parallel extension patterns for each platform

## Troubleshooting

### "MAUI workload not detected" Warning
If you see this warning in the Aspire dashboard:
1. Make sure you ran `.\restore.cmd -mauirestore` or `./restore.sh --mauirestore` from the repository root
2. The warning indicates the MAUI workload is not installed in the local `.dotnet` folder
3. Re-run the restore command with the `-mauirestore` or `--mauirestore` flag

### Build Errors
If you encounter build errors:
1. Ensure you ran the restore script with the MAUI flag first: `.\restore.cmd -mauirestore`
2. Make sure you're using .NET 10 RC or later
3. Try running `dotnet build` from the repository root first

### Platform-Specific Issues
- **Windows**: Requires Windows 10 build 19041 or higher for WinUI support. Mac Catalyst devices will show as "Unsupported" when running on Windows.
- **Mac Catalyst**: Requires macOS to run. Windows devices will show as "Unsupported" when running on macOS.
- **Android**: Not yet implemented in this playground (coming soon)
- **iOS**: Not yet implemented in this playground (coming soon)

## Current Status

✅ **Implemented:**
- Windows platform support via `AddWindowsDevice()`
- Mac Catalyst platform support via `AddMacCatalystDevice()`
- Automatic platform-specific TFM detection from project file
- Platform validation with "Unsupported" state for incompatible hosts
- Dev tunnel configuration for MAUI-to-backend communication
- Service discovery integration
- OpenTelemetry integration

🚧 **Coming Soon:**
- Android platform support via `AddAndroidDevice()`
- iOS platform support via `AddIosDevice()`
- Multi-platform simultaneous debugging

## Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [OpenTelemetry in .NET](https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel)
