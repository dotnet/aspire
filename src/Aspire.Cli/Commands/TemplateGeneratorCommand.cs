// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class TemplateGeneratorCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly IInteractionService _interactionService;
    private readonly CliExecutionContext _executionContext;

    private static readonly Argument<string?> s_templateTypeArgument = new("template-type")
    {
        Description = "Type of template: hosting, client, or full",
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Argument<string?> s_nameArgument = new("name")
    {
        Description = "Name of the integration (e.g., 'Redis', 'MongoDB')",
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Option<string?> s_outputOption = new("--output", "-o")
    {
        Description = "Output directory for the template",
    };

    private static readonly Option<string?> s_namespaceOption = new("--namespace", "-ns")
    {
        Description = "Namespace for the generated files (defaults to Aspire.Hosting.{Name})",
    };

    public TemplateGeneratorCommand(
        IConfiguration configuration,
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry)
        : base("template", TemplateGeneratorCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(executionContext);

        _configuration = configuration;
        _interactionService = interactionService;
        _executionContext = executionContext;

        Arguments.Add(s_templateTypeArgument);
        Arguments.Add(s_nameArgument);
        Options.Add(s_outputOption);
        Options.Add(s_namespaceOption);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Check if running in extension mode (should use interactive prompts)
        if (_configuration[KnownConfigNames.ExtensionPromptEnabled] is "true")
        {
            return await InteractiveExecuteAsync(cancellationToken);
        }

        // Non-interactive mode: use arguments/options
        var templateType = parseResult.GetValue(s_templateTypeArgument);
        var name = parseResult.GetValue(s_nameArgument);
        var outputPath = parseResult.GetValue(s_outputOption);
        var namespaceValue = parseResult.GetValue(s_namespaceOption);

        if (string.IsNullOrWhiteSpace(templateType))
        {
            _interactionService.DisplayError("Template type is required. Use: hosting, client, or full");
            return ExitCodeConstants.InvalidCommand;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            _interactionService.DisplayError("Integration name is required");
            return ExitCodeConstants.InvalidCommand;
        }

        var type = ParseTemplateType(templateType);
        if (type == IntegrationTemplateType.Unknown)
        {
            _interactionService.DisplayError($"Invalid template type '{templateType}'. Valid values: hosting, client, full");
            return ExitCodeConstants.InvalidCommand;
        }

        return await ExecuteAsync(type, name, outputPath, namespaceValue, cancellationToken);
    }

    private async Task<int> InteractiveExecuteAsync(CancellationToken cancellationToken)
    {
        // Prompt for template type
        var templateTypeChoices = new[]
        {
            new TemplateTypeChoice(IntegrationTemplateType.Hosting, TemplateGeneratorCommandStrings.TemplateTypeHosting),
            new TemplateTypeChoice(IntegrationTemplateType.Client, TemplateGeneratorCommandStrings.TemplateTypeClient),
            new TemplateTypeChoice(IntegrationTemplateType.Full, TemplateGeneratorCommandStrings.TemplateTypeFull)
        };

        var selectedChoice = await _interactionService.PromptForSelectionAsync(
            TemplateGeneratorCommandStrings.PromptForTemplateType,
            templateTypeChoices,
            c => c.Description,
            cancellationToken);

        // Prompt for integration name
        var name = await _interactionService.PromptForStringAsync(
            TemplateGeneratorCommandStrings.PromptForIntegrationName,
            required: true,
            validator: ValidateIntegrationName,
            cancellationToken: cancellationToken);

        // Prompt for output location (default to current directory + name)
        var defaultOutput = Path.Combine(_executionContext.WorkingDirectory.FullName, name);
        var outputPath = await _interactionService.PromptForStringAsync(
            TemplateGeneratorCommandStrings.PromptForOutputLocation,
            defaultValue: defaultOutput.EscapeMarkup(),
            required: false,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = defaultOutput;
        }

        // Prompt for namespace (optional)
        var defaultNamespace = $"Aspire.Hosting.{name}";
        var namespaceValue = await _interactionService.PromptForStringAsync(
            TemplateGeneratorCommandStrings.PromptForNamespace,
            defaultValue: defaultNamespace,
            required: false,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(namespaceValue))
        {
            namespaceValue = defaultNamespace;
        }

        return await ExecuteAsync(selectedChoice.Type, name, outputPath, namespaceValue, cancellationToken);
    }

    private async Task<int> ExecuteAsync(
        IntegrationTemplateType templateType,
        string name,
        string? outputPath,
        string? namespaceValue,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine output path
            outputPath ??= Path.Combine(_executionContext.WorkingDirectory.FullName, name);
            namespaceValue ??= $"Aspire.Hosting.{name}";

            // Create the directory if it doesn't exist
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            await _interactionService.ShowStatusAsync<int>(
                string.Format(CultureInfo.CurrentCulture, TemplateGeneratorCommandStrings.CreatingTemplate, templateType),
                async () =>
                {
                    await GenerateTemplateFilesAsync(templateType, name, outputPath, namespaceValue, cancellationToken);
                    return 0;
                });

            _interactionService.DisplaySuccess(
                string.Format(CultureInfo.CurrentCulture, TemplateGeneratorCommandStrings.TemplateCreated, templateType, outputPath));

            // Open the created directory in the editor if running from extension
            if (ExtensionHelper.IsExtensionHost(_interactionService, out var extensionInteractionService, out _))
            {
                extensionInteractionService.OpenEditor(outputPath);
            }

            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            Telemetry.RecordError($"Error creating template: {ex.Message}", ex);
            _interactionService.DisplayError($"Error creating template: {ex.Message}");
            return ExitCodeConstants.InvalidCommand;
        }
    }

    private static async Task GenerateTemplateFilesAsync(
        IntegrationTemplateType templateType,
        string name,
        string outputPath,
        string namespaceValue,
        CancellationToken cancellationToken)
    {
        // Generate based on template type
        switch (templateType)
        {
            case IntegrationTemplateType.Hosting:
                await GenerateHostingIntegrationAsync(name, outputPath, namespaceValue, cancellationToken);
                break;

            case IntegrationTemplateType.Client:
                await GenerateClientIntegrationAsync(name, outputPath, namespaceValue, cancellationToken);
                break;

            case IntegrationTemplateType.Full:
                await GenerateHostingIntegrationAsync(name, outputPath, namespaceValue, cancellationToken);
                await GenerateClientIntegrationAsync(name, outputPath, namespaceValue, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unknown template type: {templateType}");
        }
    }

    private static async Task GenerateHostingIntegrationAsync(
        string name,
        string outputPath,
        string namespaceValue,
        CancellationToken cancellationToken)
    {
        var hostingDir = Path.Combine(outputPath, $"Aspire.Hosting.{name}");
        Directory.CreateDirectory(hostingDir);

        // Create a basic README.md file
        var readmePath = Path.Combine(hostingDir, "README.md");
        var readmeContent = $@"# Aspire.Hosting.{name} library

Provides extension methods and resource definitions for an Aspire AppHost to configure {name}.

## Getting started

### Install the package

In your AppHost project, install the Aspire {name} Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.{name}
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add {name} resource and consume the connection using the following methods:

```csharp
var {name.ToLowerInvariant()} = builder.Add{name}(""{name.ToLowerInvariant()}"");

var myService = builder.AddProject<Projects.MyService>()
    .WithReference({name.ToLowerInvariant()});
```

## Additional documentation

https://github.com/dotnet/aspire

## Feedback & contributing

https://github.com/dotnet/aspire
";

        await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);

        // Create a basic extension class file
        var extensionsPath = Path.Combine(hostingDir, $"{name}HostingExtensions.cs");
        var extensionsContent = $@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace {namespaceValue};

/// <summary>
/// Extension methods for adding {name} resources to the application model.
/// </summary>
public static class {name}HostingExtensions
{{
    /// <summary>
    /// Adds a {name} resource to the application model.
    /// </summary>
    /// <param name=""builder"">The <see cref=""IDistributedApplicationBuilder""/>.</param>
    /// <param name=""name"">The name of the resource.</param>
    /// <returns>A reference to the <see cref=""IResourceBuilder{{T}}""/>.</returns>
    public static IResourceBuilder<{name}Resource> Add{name}(
        this IDistributedApplicationBuilder builder,
        string name)
    {{
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new {name}Resource(name);
        return builder.AddResource(resource);
    }}
}}
";

        await File.WriteAllTextAsync(extensionsPath, extensionsContent, cancellationToken);

        // Create a basic resource class file
        var resourcePath = Path.Combine(hostingDir, $"{name}Resource.cs");
        var resourceContent = $@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace {namespaceValue};

/// <summary>
/// Represents a {name} resource in the distributed application model.
/// </summary>
/// <param name=""name"">The name of the resource.</param>
public class {name}Resource(string name) : Resource(name)
{{
}}
";

        await File.WriteAllTextAsync(resourcePath, resourceContent, cancellationToken);
    }

    private static async Task GenerateClientIntegrationAsync(
        string name,
        string outputPath,
        string namespaceValue,
        CancellationToken cancellationToken)
    {
        var clientDir = Path.Combine(outputPath, $"Aspire.{name}");
        Directory.CreateDirectory(clientDir);

        // Use namespace value for future extensibility
        _ = namespaceValue;

        // Create a basic README.md file
        var readmePath = Path.Combine(clientDir, "README.md");
        var readmeContent = $@"# Aspire.{name} library

Registers a {name} client in the DI container for connecting to {name}. Enables corresponding health check, logging and telemetry.

## Getting started

### Prerequisites

- {name} server and connection string for accessing the service.

### Install the package

Install the Aspire {name} library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.{name}
```

## Usage example

In the _Program.cs_ file of your project, call the `Add{name}Client` extension method to register a client for use via the dependency injection container.

```csharp
builder.Add{name}Client(""{name.ToLowerInvariant()}"");
```

You can then retrieve the client instance using dependency injection. For example, to retrieve the client from a Web API controller:

```csharp
private readonly I{name}Client _{name.ToLowerInvariant()}Client;

public ProductsController(I{name}Client {name.ToLowerInvariant()}Client)
{{
    _{name.ToLowerInvariant()}Client = {name.ToLowerInvariant()}Client;
}}
```

## Configuration

The Aspire {name} library provides multiple options to configure the connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.Add{name}Client()`:

```csharp
builder.Add{name}Client(""{name.ToLowerInvariant()}"");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{{
  ""ConnectionStrings"": {{
    ""{name.ToLowerInvariant()}"": ""Host=localhost;Port=6379""
  }}
}}
```

## Additional documentation

* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
";

        await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);

        // Create a basic extension class file
        var extensionsPath = Path.Combine(clientDir, $"{name}Extensions.cs");
        var extensionsContent = $@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.{name};

/// <summary>
/// Extension methods for registering {name} client.
/// </summary>
public static class Aspire{name}Extensions
{{
    /// <summary>
    /// Registers a {name} client in the services collection.
    /// </summary>
    /// <param name=""builder"">The <see cref=""IHostApplicationBuilder""/> to add services to.</param>
    /// <param name=""connectionName"">The connection name to use.</param>
    /// <returns>The <see cref=""IHostApplicationBuilder""/> for chaining.</returns>
    public static IHostApplicationBuilder Add{name}Client(
        this IHostApplicationBuilder builder,
        string connectionName)
    {{
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        // TODO: Implement actual client registration
        // This is a placeholder implementation
        throw new NotImplementedException(""Client registration not yet implemented"");
    }}
}}
";

        await File.WriteAllTextAsync(extensionsPath, extensionsContent, cancellationToken);
    }

    private static ValidationResult ValidateIntegrationName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Error(TemplateGeneratorCommandStrings.InvalidIntegrationName);
        }

        // Check if it's a valid C# identifier (simplified check)
        if (!IsValidIdentifier(name))
        {
            return ValidationResult.Error(TemplateGeneratorCommandStrings.InvalidIntegrationName);
        }

        return ValidationResult.Success();
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // Must start with letter or underscore
        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            return false;
        }

        // Rest must be letters, digits, or underscores
        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static IntegrationTemplateType ParseTemplateType(string templateType)
    {
        return templateType.ToLowerInvariant() switch
        {
            "hosting" => IntegrationTemplateType.Hosting,
            "client" => IntegrationTemplateType.Client,
            "full" => IntegrationTemplateType.Full,
            _ => IntegrationTemplateType.Unknown
        };
    }

    private sealed record TemplateTypeChoice(IntegrationTemplateType Type, string Description);
}

internal enum IntegrationTemplateType
{
    Unknown,
    Hosting,
    Client,
    Full
}
