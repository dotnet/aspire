# Aspire.Hosting.Azure.AIFoundry library

Provides extension methods and resource definitions for an Aspire AppHost to configure Azure AI Foundry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

In your AppHost project, install the Aspire Azure AI Foundry Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.AIFoundry
```

## Configure Azure Provisioning for local development

Adding Azure resources to the Aspire application model will automatically enable development-time provisioning
for Azure resources so that you don't need to configure them manually. Provisioning requires a number of settings
to be available via .NET configuration. Set these values in user secrets in order to allow resources to be configured
automatically.

```json
{
    "Azure": {
      "SubscriptionId": "<your subscription id>",
      "ResourceGroupPrefix": "<prefix for the resource group>",
      "Location": "<azure location>"
    }
}
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add an Azure AI Foundry deployment and consume the connection using the following methods:

```csharp
var chat = builder.AddAzureAIFoundry("foundry")
                  .AddDeployment("chat", "Phi-4", "1", "Microsoft");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(chat).WaitFor(chat);
```

The `WithReference` method passes that connection information into a connection string named `chat` in the `MyService` project.

In the _Program.cs_ file of `MyService`, the connection can be consumed using a client library like [Aspire.Azure.AI.Inference](https://www.nuget.org/packages/Aspire.Azure.AI.Inference) or [Aspire.OpenAI](https://www.nuget.org/packages/Aspire.OpenAI) if the model is compatible with the OpenAI API:

Note: The `format` parameter of the `AddDeployment()` method can be found in the Azure AI Foundry portal in the details
page of the model, right after the `Quick facts` text.

#### Inference client usage
```csharp
builder.AddAzureChatCompletionsClient("chat")
       .AddChatClient();
```

#### OpenAI client usage
```csharp
builder.AddOpenAIClient("chat")
       .AddChatClient();
```

### Emulator usage

Aspire supports the usage of the Foundry Local. Add the following to your AppHost project:

```csharp
// AppHost
var chat = builder.AddAzureAIFoundry("foundry")
                  .RunAsFoundryLocal()
                  .AddDeployment("chat", "phi-3.5-mini", "1", "Microsoft");
```

When the AppHost starts up the local foundry service also be started.

This requires the local machine to have the [Foundry Local](https://learn.microsoft.com/azure/ai-foundry/foundry-local/get-started) installed and running.

## Connection Properties

When you reference Azure AI Foundry resources using `WithReference`, the following connection properties are made available to the consuming project:

### Azure AI Foundry resource

The Azure AI Foundry resource exposes the following connection properties:

| Property Name | Description |
|---------------|-------------|
| `Uri`         | The endpoint URI for the Azure AI Foundry resource (e.g., `https://<resource_name>.services.ai.azure.com/` or the emulator service URI when running Foundry Local (e.g., `http://127.0.0.1:61799/v1`) |
| `Key`         | The API key when using Foundry Local resource, e.g., `OPENAI_API_KEY` |

### Azure AI Foundry deployment

The Azure AI Foundry deployment resource inherits all properties from its parent Azure AI Foundry resource and adds:

| Property Name | Description |
|---------------|-------------|
| `ModelName`   | The deployment name when targeting Azure or model identifier when running Foundry Local, e.g., `Phi-4`, `my-chat` |
| `Format`      | The deployment format, .e.g., `OpenAI`, `Microsoft`, `xAi`, `Deepseek` |
| `Version`     | The deployment version, e.g., `1`, `2025-08-07` |

Note: The property named `ModelName` refers to the deployment name when targeting Azure AI Foundry, but to the model identifier when running Foundry Local.

Aspire exposes each property as an environment variable named `[RESOURCE]_[PROPERTY]`. For instance, the `Uri` property of a resource called `chat` becomes `CHAT_URI`.

### Azure AI Foundry project

The project resource exposes the following connection properties:

| Property Name        | Description |
|----------------------|-------------|
| `Uri`                | The project endpoint URI (e.g., `https://<account>.services.ai.azure.com/api/projects/<project>`) |
| `ConnectionString`   | The connection string in the format `Endpoint=<uri>` |

## Foundry project usage

You can create an Azure AI Foundry project resource to organize agents and model deployments:

```csharp
var foundry = builder.AddAzureAIFoundry("foundry");
var project = foundry.AddProject("my-project");

var chat = project.AddModelDeployment("chat", "gpt-4", "1.0", "OpenAI");

var myService = builder.AddPythonApp("agent", "./app", "main:app")
                       .WithReference(project);
```

The project can also be configured with additional Azure resources:

```csharp
var appInsights = builder.AddAzureApplicationInsights("ai");
var keyVault = builder.AddAzureKeyVault("kv");

var project = foundry.AddProject("my-project")
                     .WithAppInsights(appInsights)
                     .WithKeyVault(keyVault);
```

## Hosted agent usage

To deploy a containerized application as a hosted agent in Azure AI Foundry:

```csharp
var foundry = builder.AddAzureAIFoundry("foundry");
var project = foundry.AddProject("my-project");

builder.AddPythonApp("agent", "./app", "main:app")
       .PublishAsHostedAgent(project);
```

In run mode, the agent runs locally with health check endpoints and OpenTelemetry instrumentation. In publish mode, the agent is deployed as a hosted agent in Azure AI Foundry.

## Additional documentation

* https://learn.microsoft.com/azure/ai-foundry/what-is-azure-ai-foundry
* https://learn.microsoft.com/azure/ai-foundry/foundry-local/
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
