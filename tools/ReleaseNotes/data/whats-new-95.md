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

## 🖥️ App model enhancements

### 🎨 Resource icon metadata

Surface resource icons in the dashboard and tooling via new APIs:

- `WithIconName<T>(..., string iconName, IconVariant iconVariant = IconVariant.Filled)` extension on `IResourceBuilder<T>`
- `ResourceIconAnnotation` to annotate custom resources
- `CustomResourceSnapshot` now includes `IconName` and `IconVariant`

```csharp
var cache = builder.AddRedis("cache")
    .WithIconName("Database"); // IconVariant defaults to Filled
```

Additional examples:

```csharp
var web = builder
    .AddContainer("nginx", "nginx:alpine")
    .WithIconName("GlobeArrowForward", IconVariant.Regular);

var seedJob = builder
    .AddExecutable("seed", "dotnet", ["run", "--project", "./tools/Seed"])
    .WithIconName("Code");
```

### ✨ Command-line args callback context additions

`CommandLineArgsCallbackContext` now exposes the resource and has a new constructor overload.

- New property: `IResource Resource`
- New ctor: `(IList<object> args, IResource resource, CancellationToken ct = default)`

```csharp
var api = builder.AddProject<Projects.Api>("api")
    .WithArgs(ctx =>
    {
        if (ctx.Resource.Name == "api")
        {
            ctx.Args.Add("--logLevel");
            ctx.Args.Add("Information");
        }
    });
```

### 🤖 OpenAI hosting integration (new)

New app model APIs to configure OpenAI as a first-class resource and add child models ([#10830](https://github.com/dotnet/aspire/pull/10830)):

- `builder.AddOpenAI(string name)` → `IResourceBuilder<OpenAIResource>`
- `openai.WithApiKey(IResourceBuilder<ParameterResource> apiKey)`
- `openai.WithEndpoint(string endpoint)`
- `openai.AddModel(string name, string model)` → `IResourceBuilder<OpenAIModelResource>`
- `OpenAIModelResource` implements `IResourceWithParent<OpenAIResource>`; you can call `.WithHealthCheck()` on the model builder

Example:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.OpenAI;

var openai = builder.AddOpenAI("openai")
    .WithApiKey(builder.AddParameter("OPENAI__API_KEY"))
    .WithEndpoint("https://api.openai.com");

var gpt4o = openai.AddModel("gpt4o", "gpt-4o-mini")
    .WithHealthCheck();

builder.AddProject<Projects.Api>("api")
    .WithReference(gpt4o); // injects connection info for the selected model
```

## 🧰 CLI improvements

### 🧪 Run commands inside containers with aspire exec

Run ad‑hoc commands inside a running container resource and stream output ([#10380](https://github.com/dotnet/aspire/pull/10380)).

```bash
aspire exec --resource nginxcontainer ls -la
aspire exec --resource api-container dotnet -- --info
```

Sample output:

```text
> aspire exec --resource api-container dotnet -- --info
Executing in container 'api-container'...
Microsoft .NET SDK 9.0.100-preview.6.****
...
Exit code: 0
```

- Details:
  - Executes the command inside the container via DCP ContainerExec; stdout/stderr are streamed back to the CLI
  - Internally models a ContainerExecutableResource for execution; no public API changes
  - Includes unit tests covering execution and logging paths

### 🧷 More robust orphan detection

AppHost orphan detection is now resilient to PID reuse by setting `ASPIRE_CLI_STARTED` and verifying the process start time in addition to PID ([#10673](https://github.com/dotnet/aspire/pull/10673)). This improves lifecycle reliability during local development.

- Details:
  - Adds `ASPIRE_CLI_STARTED` (DateTime.ToBinary) and checks PID + start time with ±1s tolerance
  - Changes in `KnownConfigNames`, `DotNetCliRunner`, and `CliOrphanDetector`; falls back to PID-only if start time is unavailable
  - Comprehensive tests cover PID reuse, invalid start times, and fallback behavior

### 🛑 Fail fast when Dashboard can't become healthy

The CLI no longer hangs when the dashboard fails to start. It surfaces a clear error and exits, enabling faster recovery ([#10567](https://github.com/dotnet/aspire/pull/10567)).

Example error:

```text
Dashboard failed to start: Resource 'dashboard' reached terminal state 'FailedToStart'.
Inner exception: ResourceFailedException: dashboard (FailedToStart)
```

- Details:
  - Introduces shared `ResourceFailedException` (includes resource name and failed state)
  - Thrown for terminal states: `FailedToStart`, `RuntimeUnhealthy`, `Exited`, `Finished`
  - CLI catches and exits with a clear message and non-zero code; AOT JSON serialization is rooted

## 🖼️ Dashboard enhancements

### 🖼️ Custom resource icons in the Dashboard

Resources can specify Fluent UI icons that are shown in the Dashboard topology and lists ([#10760](https://github.com/dotnet/aspire/pull/10760)). See the new app model APIs above.

- Details:
  - New `ResourceIconAnnotation` + `WithIconName()` extension; `CustomResourceSnapshot` gains `IconName`/`IconVariant`
  - Protobuf schema extended; `ResourceNotificationService` manages icon data via `UpdateIcons()` pattern
  - Dashboard prefers custom icons and falls back to defaults when not specified

Screenshot placeholder:
> Replace with final image when available: [Custom icons shown in Dashboard]

### 🔍 Enhanced telemetry navigation

Clickable links in property grids make it easier to navigate between telemetry (trace IDs, span IDs) and related resources ([#10648](https://github.com/dotnet/aspire/pull/10648)).

- Details:
  - Supports registering custom components to render property grid values with deep links
  - Adds icons to various status indicators for quicker scanning

Screenshot placeholder:
> Replace with final image when available: [Property grid links to traces/spans/resources]

### 🎨 UX polish

- Error traces and spans are highlighted with error color for consistency ([#10742](https://github.com/dotnet/aspire/pull/10742)).
- Updated default icons for common resource types (Parameter → Key; Executable → Code; External service → GlobeArrowForward) ([#10762](https://github.com/dotnet/aspire/pull/10762)).

Screenshot placeholder:
> Replace with final image when available: [Error-colored traces and updated default icons]

### 📈 Additional telemetry and UX improvements

- Fix navigating to resources when a resource is already selected ([#10848](https://github.com/dotnet/aspire/pull/10848)).
- Bug fix: correct port parsing so that "pipe"(|) is not mistaken as a port separator ([#10884](https://github.com/dotnet/aspire/pull/10884)).

## 🔌 Remote development

Automatic port‑forwarding settings are now written for SSH Remote VS Code sessions (in addition to Dev Containers and Codespaces), detected via environment variables like `VSCODE_IPC_HOOK_CLI` and `SSH_CONNECTION` ([#10715](https://github.com/dotnet/aspire/pull/10715)).

- Details:
  - Introduces `SshRemoteOptions` and `ConfigureSshRemoteOptions` for environment detection
  - Uses the same settings path as Dev Containers: `.vscode-server/data/Machine/settings.json`
  - Unit tests cover detection matrix; no breaking changes

## 🎯 VS Code extension

The extension adds AppHost debug support, integrating with C# extensions when available and falling back to the terminal otherwise ([#10369](https://github.com/dotnet/aspire/pull/10369)).

- Details:
  - Choose Run or Debug; when debugging and C# extension is installed, the AppHost launches under the debugger; otherwise uses a VS Code terminal with `dotnet watch`
  - Exiting the AppHost stops the CLI process; session management handled by the extension
  - Includes tests; lays groundwork for a dedicated Aspire debugger experience

GIF/video placeholder:
> Replace with final capture when available: [Debugging AppHost from VS Code]

## ⚠️ Breaking changes

- `GitHubModelResource` no longer implements `IResourceWithoutLifetime`.

---

_For the complete list of changes, bug fixes, and improvements in this release, see the [.NET Aspire 9.5 release notes on GitHub](https://github.com/dotnet/aspire/releases)._
