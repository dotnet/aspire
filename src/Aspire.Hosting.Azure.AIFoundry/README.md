# Aspire.Hosting.Azure.AIFoundry library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure AI Foundry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

In your AppHost project, install the .NET Aspire Azure AI Foundry Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.AIFoundry
```

## Configure Azure Provisioning for local development

Adding Azure resources to the .NET Aspire application model will automatically enable development-time provisioning
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
                  .AddDeployment("chat", "phi-3.5-mini", "1", "Microsoft");

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

## Additional documentation

* https://learn.microsoft.com/azure/ai-foundry/what-is-azure-ai-foundry
* https://learn.microsoft.com/azure/ai-foundry/foundry-local/
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
