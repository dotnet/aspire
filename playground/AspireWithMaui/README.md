# AspireWithMaui Playground

This playground demonstrates .NET Aspire integration with .NET MAUI applications.

## Prerequisites

- .NET 10 RC or later
- .NET MAUI workload (will be installed automatically by the restore script)

## Getting Started

### Initial Setup

Before building or running the playground, you must restore dependencies and install the MAUI workload:

**Windows:**
```cmd
restore.cmd
```

**Linux/macOS:**
```bash
./restore.sh
```

This script will:
1. Run the main Aspire restore to set up the local .dotnet SDK
2. Install the MAUI workload into the local `.dotnet` folder (does not affect your global installation)

> **Note:** The MAUI workload is installed only in the repository's local `.dotnet` folder and will not interfere with your system-wide .NET installation.

### Running the Playground

After running the restore script, you can build and run the playground:

**Using Visual Studio:**
1. Run `restore.cmd` (Windows) or `./restore.sh` (Linux/macOS)
2. Open `AspireWithMaui.AppHost` project
3. Set it as the startup project
4. Press F5 to run

**Using VS Code:**
1. Run `restore.cmd` (Windows) or `./restore.sh` (Linux/macOS)
2. From the repository root, run: `./start-code.sh` or `start-code.cmd`
3. Open the `AspireWithMaui` folder
4. Use the debugger to run the AppHost

**Using Command Line:**
1. Run `restore.cmd` (Windows) or `./restore.sh` (Linux/macOS)
2. Navigate to `AspireWithMaui.AppHost` directory
3. Run: `dotnet run`

## What's Included

- **AspireWithMaui.AppHost** - The Aspire app host that orchestrates all services
- **AspireWithMaui.MauiClient** - A .NET MAUI application that connects to the backend
- **AspireWithMaui.WeatherApi** - An ASP.NET Core Web API providing weather data
- **AspireWithMaui.ServiceDefaults** - Shared service defaults for non-MAUI projects
- **AspireWithMaui.MauiServiceDefaults** - Shared service defaults specific to MAUI projects

## Features Demonstrated

### MAUI Platform Support
The playground demonstrates Aspire's ability to manage MAUI apps across multiple platforms:
- Windows
- Android
- iOS
- macCatalyst

### OpenTelemetry Integration
The MAUI client uses OpenTelemetry to send traces and metrics to the Aspire dashboard via dev tunnels.

### Service Discovery
The MAUI app discovers and connects to backend services (WeatherApi) using Aspire's service discovery.

### Multi-Platform Development
The AppHost shows how to:
- Configure different platforms with `.WithWindows()`, `.WithAndroid()`, `.WithiOS()`, `.WithMacCatalyst()`
- Set up dev tunnels for MAUI app communication
- Reference backend services from MAUI apps

## Troubleshooting

### "MAUI workload not detected" Warning
If you see this warning in the Aspire dashboard:
1. Make sure you ran `restore.cmd` or `./restore.sh` in the `playground/AspireWithMaui` directory
2. The warning indicates the MAUI workload is not installed in the local `.dotnet` folder
3. Re-run the restore script to install it

### Build Errors
If you encounter build errors:
1. Ensure you ran the restore script first
2. Make sure you're using .NET 10 RC or later
3. Try running `dotnet build` from the repository root first

### Platform-Specific Issues
- **Windows**: Requires Windows 10 build 19041 or higher for WinUI support
- **Android**: Requires Android SDK and emulator/device
- **iOS/macCatalyst**: Requires macOS with Xcode installed

## Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [OpenTelemetry in .NET](https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel)
