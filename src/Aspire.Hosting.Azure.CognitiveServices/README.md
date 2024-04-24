# Aspire.Hosting.Azure.CognitiveServices library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure OpenAI.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

In your AppHost project, install the .NET Aspire Azure Hosting Cognitive Services library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.CognitiveServices
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

> NOTE: Developers must have Owner access to the target subscription so that role assignments
> can be configured for the provisioned resources.

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add an Azure AI OpenAI service and consume the connection using the following methods:

```csharp
var openai = builder.AddAzureOpenAI("openai");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(openai);
```

The `WithReference` method passes that connection information into a connection string named `openai` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Azure.AI.OpenAI](https://www.nuget.org/packages/Aspire.Azure.AI.OpenAI):

```csharp
builder.AddAzureOpenAIClient("openai");
```

## Additional documentation

* https://learn.microsoft.com/dotnet/api/overview/azure/ai.openai-readme
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
