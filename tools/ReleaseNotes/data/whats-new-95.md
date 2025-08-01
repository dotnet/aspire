---
title: What's new in .NET Aspire 9.5
description: Learn what's new in .NET Aspire 9.5.
ms.date: 07/31/2025
---

# What's new in .NET Aspire 9.5

📢 .NET Aspire 9.5 is the next minor version release of .NET Aspire. It supports:

- .NET 8.0 Long Term Support (LTS)
- .NET 9.0 Standard Term Support (STS)
- .NET 10.0 Preview 6

If you have feedback, questions, or want to contribute to .NET Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on our new [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://aka.ms/aspire-discord) to chat with the team and other community members.

It's important to note that .NET Aspire releases out-of-band from .NET releases. While major versions of Aspire align with major .NET versions, minor versions are released more frequently. For more information on .NET and .NET Aspire version support, see:

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy): Definitions for LTS and STS.
- [.NET Aspire support policy](https://dotnet.microsoft.com/platform/support/policy/aspire): Important unique product lifecycle details.

## ⬆️ Upgrade to .NET Aspire 9.5

Moving between minor releases of Aspire is simple:

1. In your AppHost project file (that is, _MyApp.AppHost.csproj_), update the [📦 Aspire.AppHost.Sdk](https://www.nuget.org/packages/Aspire.AppHost.Sdk) NuGet package to version `9.5.0`:

    ```xml
    <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0" />
    ```

    For more information, see [.NET Aspire SDK](xref:dotnet/aspire/sdk).

1. Check for any NuGet package updates, either using the NuGet Package Manager in Visual Studio or the **Update NuGet Package** command from C# Dev Kit in VS Code.
1. Update to the latest [.NET Aspire templates](../fundamentals/aspire-sdk-templates.md) by running the following .NET command line:

    ```dotnetcli
    dotnet new install Aspire.ProjectTemplates
    ```

    > The `dotnet new install` command will update existing Aspire templates to the latest version if they are already installed.

If your AppHost project file doesn't have the `Aspire.AppHost.Sdk` reference, you might still be using .NET Aspire 8. To upgrade to 9, follow [the upgrade guide](../get-started/upgrade-to-aspire-9.md).

## 🚀 CLI improvements

### ✨ Container exec support

The `aspire exec` command now supports executing commands inside containers, providing a powerful way to interact with containerized resources directly from the CLI.

```bash
# Execute a command inside a container resource
aspire exec --resource nginxcontainer ls

# Run an interactive shell in a container
aspire exec --resource webapi bash
```

> [!NOTE]
> Container exec functionality is currently feature-flagged and may require enabling through configuration.

### 🛠️ Enhanced debugging support

VS Code extension now provides improved debugging capabilities for AppHost projects:

- **Debug/No Debug selection**: Choose whether to launch AppHost with debugging enabled
- **Automatic building**: AppHost projects are built automatically when C# Dev Kit is installed  
- **Fallback support**: Graceful fallback to `dotnet watch` when debugging extensions aren't available
- **Integrated lifecycle**: Stopping the AppHost automatically stops the CLI process

### 🌐 SSH Remote development support

Added first-class support for SSH Remote VS Code development environments with automatic port forwarding configuration:

```bash
# SSH Remote environments are automatically detected when both variables are present:
# VSCODE_IPC_HOOK_CLI - Indicates VS Code running with IPC hook CLI  
# SSH_CONNECTION - Standard SSH environment variable
```

Port forwarding settings are now automatically configured for:
- **Codespaces** (existing)
- **Devcontainers** (existing)  
- **SSH Remote** (new in 9.5)

### 🔧 Improved reliability

Enhanced AppHost orphan detection to be robust against PID reuse:

- **Process verification**: Uses both PID and process start time for accurate detection
- **Backwards compatibility**: Gracefully falls back to PID-only logic when needed
- **Cross-platform**: Reliable process monitoring across different operating systems

### ⚡ Better error handling  

Improved `aspire exec` command validation and error reporting:

- **Fail-fast validation**: Arguments are validated early to provide quick feedback
- **Better error messages**: Clear error reporting when commands or arguments are missing
- **Enhanced help**: Improved `aspire exec --help` output with better guidance

## 🖥️ Dashboard enhancements

### 📊 Enhanced trace visualization

Significant improvements to trace detail pages:

- **Performance optimizations**: Faster loading and rendering of trace details
- **Error highlighting**: Error traces and spans are displayed with error color coding
- **Improved span details**: Better percentage calculations and visual indicators
- **Enhanced navigation**: Links between telemetry and resources in grid values

### 🔗 Better resource integration

- **Resource linking**: Navigate directly from telemetry data to associated resources
- **Improved filtering**: Better resource filtering with grouping labels
- **Cleaner display**: Hide "(None)" text when only one resource is available

### 🎨 UI improvements

- **Updated toolbars**: Refreshed mobile and desktop toolbar designs
- **Better accessibility**: Resource details view headers now use proper heading elements
- **Icon enhancements**: More icons added to dashboard property values for better visual clarity
- **FluentUI update**: Updated to FluentUI 4.12.1 with various improvements

### 📋 Console logs enhancements

- **Dynamic updates**: Resource list updates properly after visibility changes
- **Better item counting**: Improved "Showing X items" text with accessibility announcements
- **Argument display**: Arguments with spaces are now properly quoted in displays

## 🔧 Quality and reliability improvements

This release focuses heavily on quality improvements, bug fixes, and developer experience enhancements:

### 🌍 Localization

- **Expanded language support**: Enhanced localization across multiple languages
- **Resource string improvements**: Better organization of CLI and Dashboard resource strings
- **Consistent translations**: Updated translation files across all supported languages

### 🔨 Build and deployment

- **Native AOT support**: Extended native AOT compilation support for CLI on additional platforms:
  - linux-arm64
  - linux-musl-x64
- **Configuration fixes**: Resolved issues with global configuration file handling

### 🧪 External service improvements

- **Manifest generation**: Fixed issues with external services using URL parameters failing to generate manifests
- **Parameter handling**: Enhanced parameter value access for external service resources

## 📝 Developer experience

### 🎯 Better CLI configuration

- **Improved config handling**: Fixed issues with `aspire config set` writing incorrect paths to global settings
- **Feature flag control**: Better control over experimental CLI features through feature flags

### 🔍 Enhanced health checks

- **Log suppression**: Health check logs are now properly suppressed to reduce noise
- **Async support**: Improved health check method support for external service resources with asynchronous parameter access

---

*For the complete list of changes, bug fixes, and improvements in this release, see the [.NET Aspire 9.5 release notes on GitHub](https://github.com/dotnet/aspire/releases).*
