---
title: What's new in .NET Aspire 9.5
description: Learn what's new in .NET Aspire 9.5.
ms.date: 08/21/2025
---

# What's new in .NET Aspire 9.5

## Table of contents

- [Upgrade to .NET Aspire 9.5](#️-upgrade-to-net-aspire-95)
- [CLI improvements](#-cli-improvements)
  - [`aspire exec` (9.5 delta)](#-aspire-exec-95-delta)
  - [Robust orphan detection](#-robust-orphan-detection)
  - [Package channel & templating enhancements](#-package-channel--templating-enhancements)
  - [Improved cancellation & ctrl-c ux](#-improved-cancellation--ctrl-c-ux)
  - [New aspire update command (preview)](#-new-aspire-update-command-preview)
  - [Channel-aware aspire add & templating](#-channel-aware-aspire-add--templating)
  - [Improved aspire exec (feature flag)](#-improved-aspire-exec-feature-flag)
  - [Smarter package prefetching](#-smarter-package-prefetching)
  - [Rich markdown & styling](#-rich-markdown--styling)
  - [Orphan & runtime diagnostics](#-orphan--runtime-diagnostics)
  - [Localization & resource strings](#-localization--resource-strings)
  - [Stability & error handling](#-stability--error-handling)
  - [Developer ergonomics](#developer-ergonomics)
  - [Command infrastructure & interactions](#command-infrastructure--interactions)
- [Dashboard enhancements](#-dashboard-enhancements)
  - [Deep-linked telemetry navigation](#-deep-linked-telemetry-navigation)
  - [Multi-resource console logs](#-multi-resource-console-logs)
  - [Custom resource icons](#-custom-resource-icons)
  - [Reverse proxy support](#-reverse-proxy-support)
  - [Improved mobile experience](#-improved-mobile-experience)
  - [Enhanced resource management](#-enhanced-resource-management)
  - [Container runtime notifications](#-container-runtime-notifications)
  - [UI improvements](#-ui-improvements)
  - [Trace performance & integration](#-trace-performance--integration)
  - [Localization & deployment](#-localization--deployment)
- [Integration changes and additions](#integration-changes-and-additions)
  - [OpenAI hosting integration](#openai-hosting-integration)
  - [GitHub Models typed catalog](#github-models-typed-catalog)
- [App model enhancements](#️-app-model-enhancements)
  - [Telemetry configuration apis](#telemetry-configuration-apis)
  - [Resource waiting patterns](#resource-waiting-patterns)
  - [Externalservice waitfor behavior change](#externalservice-waitfor-behavior-change)
  - [Context-based endpoint resolution](#context-based-endpoint-resolution)
  - [Resource lifetime behavior](#resource-lifetime-behavior)
  - [Interactioninput api changes](#interactioninput-api-changes)
  - [Custom resource icons (app model)](#custom-resource-icons)
  - [Mysql password improvements](#mysql-password-improvements)
  - [Remote & debugging experience](#remote--debugging-experience)
- [Azure](#azure)
  - [Azure ai foundry enhancements](#azure-ai-foundry-enhancements)
  - [Azure app configuration emulator apis](#azure-app-configuration-emulator-apis)
  - [Broader azure resource capability surfacing](#broader-azure-resource-capability-surfacing)
  - [Azure provisioning & deployer](#azure-provisioning--deployer)
  - [Azure deployer interactive command handling](#azure-deployer-interactive-command-handling)
  - [Azure resource idempotency & existing resources](#azure-resource-idempotency--existing-resources)
  - [Compute image deployment](#compute-image-deployment)
  - [Module-scoped bicep deployment](#module-scoped-bicep-deployment)
- [Docker & container tooling](#docker--container-tooling)
  - [Docker compose aspire dashboard forwarding headers](#docker-compose-aspire-dashboard-forwarding-headers)
  - [Container build customization](#container-build-customization)
- [Publishing & interactions](#publishing--interactions)
  - [Publishing progress & activity reporting](#publishing-progress--activity-reporting)
  - [Parameter & interaction api updates](#parameter--interaction-api-updates)
- [Localization & ux consistency](#localization--ux-consistency)
- [Reliability & diagnostics](#reliability--diagnostics)
- [Minor enhancements](#minor-enhancements)

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

2. Check for any NuGet package updates, either using the NuGet Package Manager in Visual Studio or the **Update NuGet Package** command from C# Dev Kit in VS Code.

3. Update to the latest [.NET Aspire templates](../fundamentals/aspire-sdk-templates.md) by running the following .NET command line:

    ```dotnetcli
    dotnet new install Aspire.ProjectTemplates
    ```

  > [!NOTE]
  > The `dotnet new install` command will update existing Aspire templates to the latest version if they are already installed.

If your AppHost project file doesn't have the `Aspire.AppHost.Sdk` reference, you might still be using .NET Aspire 8. To upgrade to 9, follow [the upgrade guide](../get-started/upgrade-to-aspire-9.md).

## 🚀 CLI improvements

### 🧪 `aspire exec` (9.5 delta)

Building on the 9.4 preview, 9.5 adds:

- `--workdir` flag to run inside a specific container directory (#10912)
- Fail-fast argument validation & clearer errors (#10606)
- Improved help/usage text (#10598)

Example (new flag):

```bash
aspire exec --resource worker --workdir /app/tools -- dotnet run -- --seed
```

The feature flag requirement from 9.4 still applies.

### 🧷 Robust orphan detection

Resilient to PID reuse via `ASPIRE_CLI_STARTED` timestamp + PID verification:

```text
Detected orphaned prior AppHost process (PID reused). Cleaning up...
```

### 📦 Package channel & templating enhancements

New packaging channel infrastructure lets `aspire add` and templating flows surface stable vs pre-release channels with localized UI.

### 🧵 Improved cancellation & CTRL-C UX

- CTRL-C guidance message
- Fix for stall on interrupt

### 🔄 New `aspire update` command (preview)

Introduces automated project/package upgrade workflows (#11019) with subsequent formatting and correctness improvements (#11148, #11145):

```bash
# Preview: analyze and update out-of-date Aspire packages & templates
aspire update

# Non-interactive (planned scenarios) can be scripted once stabilized
```

Features:

- Detects outdated Aspire NuGet packages (respecting channels)
- Resolves diamond dependencies without duplicate updates (#11145)
- Enhanced, colorized output and summary (#11148)

Extended markdown rendering support (#10815) with:

- Code fences, emphasis, bullet lists
- Safe markup escaping (#10462)
- Purple styling for default values in prompts (#10474)
*** End Patch

### 📦 Channel-aware `aspire add` & templating

New packaging channel infrastructure (#10801, #10899) adds stable vs pre-release channel selection inside interactive flows:

- Channel menu for `aspire add` when multiple package qualities exist
- Pre-release surfacing with localized labels
- Unified PackagingService powering add + template selection

### 🧪 Improved `aspire exec` (feature flag)

- `--workdir` flag for container working directory selection (#10912)
- Fail-fast validation & clearer error messaging (#10606)
- Help text and usage clarity improvements (#10598, #10522)
- Feature flag gating (`ExecCommand` flag) (#10664)

### 🧠 Smarter package prefetching

Refactored NuGet prefetch architecture (#11120) reducing UI lag during `aspire new` on macOS (#11069) and enabling command-aware caching. Temporary NuGet config improvements ensure wildcard mappings (#10894).

### 📰 Rich markdown & styling

Extended markdown rendering support (#10815) with:
- Code fences, emphasis, bullet lists
- Safe markup escaping (#10462)
- Purple styling for default values in prompts (#10474)

### 🧭 Orphan & runtime diagnostics

- Robust orphan AppHost detection via start timestamp (#10673)
- .NET SDK availability check with actionable errors (#10525)
- Container runtime health surfaced earlier (shared infra)

### 🌐 Localization & resource strings

Moved hardcoded CLI strings to `.resx` (#10504) and enabled multi-language builds (numerous OneLocBuild commits). Command help, prompts, and errors are now localizable.

### Developer ergonomics

- AppHost debug/run selection surfaced in extension & CLI integration (#10877, #10369)
- Relative path included in AppHost status messages + TUI dashboard (#11132)
- Clean Spectre Console debug logging with reduced noise (#11125)
- Improved friendly name generation & pre-release submenu in `aspire add` (#10485)
- Directory safety check for `aspire new` (#10496) and consistent template inputs (#10444, #10508)

### Command infrastructure & interactions

- Context-sensitive completion messages for publish/deploy (#10501)
- Markdown-to-Spectre converter foundation reuse (#10815)
- Interaction answer typing change (`object`) for future extensibility (#10480)

### 🧪 Stability & error handling

- Dashboard startup hang eliminated via `ResourceFailedException` (#10567)
- CTRL-C stall fix (#10962) + guidance message (#10203)
- Improved package channel error surfaces & prefetch retry logic (#11120)

> The `aspire exec` and `aspire update` commands remain in preview behind feature flags; behavior may change in a subsequent release.

## 🎨 Dashboard enhancements

### 🔗 Deep-linked telemetry navigation

Trace IDs, span IDs, resource names, and log levels become interactive buttons in property grids for one-click navigation between telemetry views (#10648).

### 📊 Multi-resource console logs
 
New "All" option streams logs from every running resource simultaneously with deterministic colored name prefixes and a separate timestamp preference to reduce noise in aggregate view (#10981):

```text
[api      INF] Hosting started
[postgres INF] database system is ready
[redis    INF] Ready to accept connections
```

### 🎭 Custom resource icons

Resources can specify custom icons via `WithIconName()` for better visual identification in dashboard views (#10760).

### 🌐 Reverse proxy support
 
Dashboard now explicitly maps forwarded Host & Proto headers when `ASPIRE_DASHBOARD_FORWARDEDHEADERS_ENABLED=true`, fixing OpenID auth redirects and URL generation behind reverse proxies like YARP (#10388). Only these two headers are allowed to limit spoofing surface:

- Enable with `ASPIRE_DASHBOARD_FORWARDEDHEADERS_ENABLED=true`
- Fixes OpenID authentication redirect issues with proxy scenarios

### 📱 Improved mobile experience

Mobile and desktop toolbars redesigned for better usability across all dashboard pages with improved responsive layouts (#10407).

### 🔧 Enhanced resource management

- Resource action menus reorganized into sub-menus to prevent overflow (#10869)
- LaunchProfile property added to project details for easier debugging (#10906)
- Better resource navigation and selection handling (#10848)

### 🚨 Container runtime notifications

Smart notifications appear when Docker/Podman is installed but unhealthy, with automatic dismissal when runtime recovers (#11008).

### ✨ UI improvements

- Error spans use consistent error styling (#10742)
- Better default icons for parameters and services (#10762)
- Improved navigation reliability (#10848)
- Enhanced port parsing (#10884)
- Message truncation for long log entries (#10882)
- Optional log line wrapping (#10271)
- Improved text visualizer dialog (#10964)

### 📊 Trace performance & integration

- Optimized trace detail page rendering (#10308)
- Embedded log entries within trace spans (#10281)
- Better span timing calculations (#10310)

### 🌍 Localization & deployment

- Comprehensive dashboard localization with consolidated resource files
- Launch profile support with localized display names (#10906)
- Forwarded headers support for proxy/container scenarios (#10388)

## Integration changes and additions

### OpenAI hosting integration

New `AddOpenAI` integration lets you model self-hosted or compatible OpenAI endpoints and attach one or more model resources as children. You configure the API key and endpoint once, then add typed model resources you can reference from other projects. This enables local development against an OpenAI-compatible server (self-hosted, gateway, or proxy) using the same resource graph patterns as other services.

Key capabilities:

- Single OpenAI endpoint resource with child model resources (`AddModel`).
- Parameter-based API key provisioning (`ParameterResource`).
- Endpoint override for local gateways / proxies.
- Resource referencing so other projects pick up connection info automatically.

Example:

```csharp
var openai = builder.AddOpenAI("openai")
  .WithApiKey(builder.AddParameter("OPENAI__API_KEY", secret: true))
  .WithEndpoint("http://localhost:9000");

var chat = openai.AddModel("chat", "gpt-4o-mini");

builder.AddProject<Projects.Api>("api")
  .WithReference(chat);
```

### GitHub Models typed catalog

9.5 introduces a strongly-typed catalog for GitHub-hosted models (issue #9568 follow-on, PR #10986) mirroring the Azure AI Foundry pattern. Instead of passing raw string identifiers, you can now reference `GitHubModel` constants for refactoring safety and discoverability (IntelliSense surfaces available providers/variants). A daily automation refreshes the generated catalog (PR #11040) so newly published models become available without waiting for a full release.

Example:

```csharp
// Before (string literal prone to typos)
builder.AddGitHubModel("phi4", model: "microsoft/phi-4" );

// After (typed constant)
builder.AddGitHubModel("phi4", GitHubModel.Microsoft.Phi4);
```

This reduces magic strings, enables code navigation to model definitions, and aligns GitHub model usage with the existing Azure AI Foundry `AIFoundryModel` experience introduced in 9.5.

## 🖥️ App model enhancements

### Telemetry configuration APIs

Enhanced OTLP telemetry configuration with protocol selection:

```csharp
// New OtlpProtocol enum with Grpc and HttpProtobuf options
public enum OtlpProtocol
{
    Grpc = 0,
    HttpProtobuf = 1
}

// Configure OTLP telemetry with specific protocol
var api = builder.AddProject<Projects.Api>("api")
  .WithOtlpExporter(OtlpProtocol.HttpProtobuf);

// Or use default protocol
var worker = builder.AddProject<Projects.Worker>("worker")  \
  .WithOtlpExporter();
```

### Resource waiting patterns

Enhanced waiting with new `WaitForStart` options (issue #7532, implemented in PR #10948). `WaitForStart` waits for a dependency to reach the Running state without blocking on health checks—useful when initialization code (migrations, seeding, registry bootstrap) must run before the service can become "healthy". Compared to `WaitFor`:

- `WaitFor` = Running + passes health checks.
- `WaitForStart` = Running only (ignores health checks, faster in dev / init flows).

This mirrors Docker Compose's `service_started` vs `service_healthy` conditions and supports both existing `WaitBehavior` modes.

```csharp
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");

var api = builder.AddProject<Projects.Api>("api")
  .WaitForStart(postgres)  // New: Wait only for startup, not health
  .WaitFor(redis)  // Existing: Wait for healthy
  .WithReference(postgres)
  .WithReference(redis);
```

### ExternalService WaitFor behavior change

Breaking change: `WaitFor` now properly honors `ExternalService` health checks so dependent resources defer start until the external service reports healthy (issue [#10827](https://github.com/dotnet/aspire/issues/10827)). Previously, dependents would start even if the external target failed its readiness probe. This improves correctness and aligns `ExternalService` with other resource types.

If you relied on the old lenient behavior (e.g., starting a frontend while an external API was still warming up), you can temporarily remove the `WaitFor` call or switch to `WaitForStart` if only startup is required.

```csharp
var externalApi = builder.AddExternalService("backend-api", "http://localhost:5082")
  .WithHttpHealthCheck("/health/ready");

builder.AddProject<Projects.Frontend>("frontend")
  .WaitFor(externalApi);
```

### Context-based endpoint resolution

Breaking change: Endpoint resolution in `WithEnvironment` now correctly resolves container hostnames instead of always using "localhost" ([#8574](https://github.com/dotnet/aspire/issues/8574)):

```csharp
var redis = builder.AddRedis("redis");

builder.AddRabbitMQ("rabbitmq")
  .WithEnvironment(context =>
  {
    var endpoint = redis.GetEndpoint("tcp");
    var redisHost = endpoint.Property(EndpointProperty.Host);
    var redisPort = endpoint.Property(EndpointProperty.Port);

    context.EnvironmentVariables["REDIS_HOST"] = redisHost;
    context.EnvironmentVariables["REDIS_PORT"] = redisPort;
  });
```

### Resource lifetime behavior

Breaking change: Several resources now support `WaitFor` operations that were previously not supported ([#10851](https://github.com/dotnet/aspire/pull/10851), [#10842](https://github.com/dotnet/aspire/pull/10842)):

```csharp
var connectionString = builder.AddConnectionString("db");
var apiKey = builder.AddParameter("api-key", secret: true);

builder.AddProject<Projects.Api>("api")
  .WaitFor(connectionString)
  .WaitFor(apiKey);
```

Resources like `ParameterResource`, `ConnectionStringResource`, and GitHub Models no longer implement `IResourceWithoutLifetime`. They now show as "Running" and can be used with `WaitFor` operations.

### InteractionInput API changes

Breaking change: The `InteractionInput` API now requires `Name` and makes `Label` optional ([#10835](https://github.com/dotnet/aspire/pull/10835)):

```csharp
var input = new InteractionInput 
{ 
  Name = "username",
  Label = "Username",
  InputType = InputType.Text 
};
```

All `InteractionInput` instances must now specify a `Name`. The `Label` property is optional and will default to the `Name` if not provided.

### Custom resource icons

Resources can specify custom icon names for better visual identification:

```csharp
var postgres = builder.AddPostgres("postgres")
  .WithIconName("database");

var api = builder.AddProject<Projects.Api>("api")
  .WithIconName("web-app", ApplicationModel.IconVariant.Regular);
```

### MySQL password improvements

Consistent password handling across database resources:

```csharp
var mysql = builder.AddMySql("mysql")
  .WithPassword(builder.AddParameter("mysql-password", secret: true));

// Password can be modified during configuration
mysql.Resource.PasswordParameter = builder.AddParameter("new-password", secret: true);
```

### Remote & debugging experience

### 🔄 SSH remote auto port forwarding

VS Code SSH sessions now get automatic port forwarding configuration just like Dev Containers and Codespaces:

```text
Remote SSH environment detected – configuring forwarded ports (dashboard, api, postgres).
```

### 🐞 AppHost debugging in VS Code

The extension offers Run vs Debug for the AppHost. If the C# extension is present it launches under the debugger; otherwise a terminal with `dotnet watch` is used.

### 🧩 Extension modernization

Package upgrades and localization support plus groundwork for richer debugging scenarios.

## Azure

### Azure AI Foundry enhancements

9.5 adds a generated, strongly-typed model catalog (`AIFoundryModel`) for IntelliSense + ref safety when creating deployments (PR #10986) and a daily automation that refreshes the catalog as new models appear in Azure AI Foundry (PR #11040). Sample apps and end-to-end tests now use these constants (PR #11039) instead of raw strings. The original Foundry hosting integration and local runtime support were introduced earlier (issue #9568); this release focuses on developer ergonomics and keeping model metadata current.

Strongly-typed model catalog with IntelliSense support:

```csharp
var aiFoundry = builder.AddAzureAIFoundry("ai-foundry");

// Strongly-typed model references
var gpt4 = aiFoundry.AddDeployment("gpt-4", AIFoundryModel.OpenAI.Gpt4);
var mistral = aiFoundry.AddDeployment("mistral", AIFoundryModel.MistralAi.MistralLarge2411);

// Local on-device mode
var localFoundry = builder.AddAzureAIFoundry("local-ai")
  .RunAsFoundryLocal();
```

### Azure App Configuration emulator APIs

Run emulators locally with full configuration support:

```csharp
var appConfig = builder.AddAzureAppConfiguration("config")
  .RunAsEmulator(emulator => emulator
    .WithDataVolume("config-data")
    .WithHostPort(8080));
```

### Broader Azure resource capability surfacing

Several Azure hosting resource types now implement `IResourceWithEndpoints` enabling uniform endpoint discovery and waiting semantics:

- `AzureAIFoundryResource`
- `AzureAppConfigurationResource`  
- `AzureKeyVaultResource`
- `AzurePostgresFlexibleServerResource`
- `AzureRedisCacheResource`

### Azure provisioning & deployer

9.5 delivers the first iteration of the Azure provisioning & deployment pipeline that unifies interactive prompting, Bicep compilation, and mode-specific behavior (run vs publish) across Azure resources:

- New provisioning contexts separate run-mode and publish-mode flows ([#11094](https://github.com/dotnet/aspire/pull/11094)).
- Graph-based dependency planning (`ResourceDeploymentGraph`) ensures correct ordering of resource provisioning.
- Improved error handling and idempotency for `AddAsExistingResource` across all Azure resources ([#10562](https://github.com/dotnet/aspire/issues/10562)).
- Support for deploying compute images and resources (custom images referenced in your environment) ([#11030](https://github.com/dotnet/aspire/pull/11030)).
- Deploy individual Bicep modules instead of a monolithic `main.bicep` for clearer failure isolation and faster iteration ([#11098](https://github.com/dotnet/aspire/pull/11098)).
- Localized interaction + notification strings across all provisioning prompts (multiple OneLocBuild PRs).

Provisioning automatically prompts for required values only once per run, caches results, and reuses them in publish-mode without re-prompting. This reduces friction when iterating locally while maintaining reproducibility for production publish.

### Azure deployer interactive command handling

The AppHost now wires Azure provisioning prompts into the standard interaction system (initial work in [#10038](https://github.com/dotnet/aspire/pull/10038), extended in [#10792](https://github.com/dotnet/aspire/pull/10792) and [#10845](https://github.com/dotnet/aspire/pull/10845)). This enables:

- Consistent UX for parameter entry (names, descriptions, validation)
- Localized prompt text
- Support for non-interactive scenarios via pre-supplied parameters

### Azure resource idempotency & existing resources

Calling `AddAsExistingResource` is now idempotent across Azure hosting resource builders; repeated calls no longer cause duplicate annotations or inconsistent behavior ([#10562](https://github.com/dotnet/aspire/issues/10562)). This improves reliability when composing reusable extension methods.

### Compute image deployment

You can now reference and deploy custom compute images as part of Azure environment provisioning ([#11030](https://github.com/dotnet/aspire/pull/11030)). This lays groundwork for richer VM/container hybrid topologies.

### Module-scoped Bicep deployment

Instead of generating a single aggregated template, 9.5 deploys individual Bicep modules ([#11098](https://github.com/dotnet/aspire/pull/11098)). Failures surface with more precise context and partial successes require less rework.

## Docker & container tooling

### Docker Compose Aspire Dashboard forwarding headers

`AddDockerComposeEnvironment(...).WithDashboard()` gained `WithForwardedHeaders()` to enable forwarded `Host` and `Proto` handling for dashboard scenarios behind reverse proxies or compose networks ([#11080](https://github.com/dotnet/aspire/pull/11080)). This mirrors the standalone dashboard forwarded header support and fixes auth redirect edge cases.

```csharp
builder.AddDockerComposeEnvironment("env")
  .WithComposeFile("docker-compose.yml")
  .WithDashboard(d => d.WithForwardedHeaders());
```

### Container build customization

`ContainerBuildOptions` support (commit [#10074](https://github.com/dotnet/aspire/pull/10074)) enables customizing the underlying `dotnet publish` invocation when Aspire builds project-sourced container images (for example to change configuration, trimming, or pass additional MSBuild properties). Use the new options hook on the project container image configuration to set MSBuild properties instead of maintaining a custom Dockerfile. (Exact API surface is intentionally summarized here to avoid drift; see API docs for `ContainerBuildOptions` in the hosting namespace for usage.)

## Publishing & interactions

### Publishing progress & activity reporting

`IPublishingActivityProgressReporter` was renamed to `IPublishingActivityReporter` and output formatting was reworked to provide clearer, structured progress (multiple commits culminating in improved messages). Expect more concise status lines and actionable error sections when using `aspire publish`.

### Parameter & interaction API updates

- `ParameterResource.Value` is now obsolete: switch to `await parameter.GetValueAsync()` or inject parameter resources directly ([#10363](https://github.com/dotnet/aspire/pull/10363)). This change improves async value acquisition and avoids accidental blocking.
- Interaction inputs enforce server-side validation and required `Name` property (breaking, [#10835](https://github.com/dotnet/aspire/pull/10835)).
- New notification terminology (renamed from MessageBar, [#10449](https://github.com/dotnet/aspire/pull/10449)).

Migration example:

```csharp
// Before (deprecated)
var value = myParam.Value;

// After
var value = await myParam.GetValueAsync();
```

## Localization & UX consistency

Extensive localization landed across the AppHost, Azure provisioning, interactions, launch profiles, and dashboard-facing messages (multiple OneLocBuild commits). Resource strings replace hardcoded literals, enabling translated tooling experiences out-of-the-box.

## Reliability & diagnostics

- New `ResourceStoppedEvent` provides lifecycle insight when resources shut down or fail ([#11103](https://github.com/dotnet/aspire/pull/11103)).
- Hardened container runtime health checks now block image build when the runtime is unhealthy rather than failing later ([#10399](https://github.com/dotnet/aspire/pull/10399), [#10402](https://github.com/dotnet/aspire/pull/10402)).
- Dashboard startup failure now surfaces an immediate, clear exception instead of hanging (`ResourceFailedException`, [#10567](https://github.com/dotnet/aspire/pull/10567)).
- Version checking messages localized and de-duplicated ([#11017](https://github.com/dotnet/aspire/pull/11017)).

## Minor enhancements

- Server-side validation of interaction inputs ([#10527](https://github.com/dotnet/aspire/pull/10527)).
- Custom resource icons section already noted now also apply to project & container resources via unified annotation.
- Launch profile localization and model surfaced in dashboard resource details ([#10906](https://github.com/dotnet/aspire/pull/10906)).

<!-- API details merged into relevant sections above to avoid duplication. Refer to api-changes-diff.txt for exhaustive list. -->


