# Aspire.Hosting.Azure.CognitiveServices library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure OpenAI.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure OpenAI or OpenAI account - [create an Azure OpenAI Service resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource)

### Install the package

In your AppHost project, install the .NET Aspire Azure Hosting Cognitive Services library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.CognitiveServices
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add an Azure AI OpenAI service and consume the connection using the following methods:

```csharp
var openai = builder.AddAzureOpenAI("openai");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(openai);
```

The `AddAzureOpenAI` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:openai` config key. The `WithReference` method passes that connection information into a connection string named `openai` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Azure.AI.OpenAI](https://www.nuget.org/packages/Aspire.Azure.AI.OpenAI):

```csharp
builder.AddAzureOpenAIClient("openai");
```

## Additional documentation

* https://learn.microsoft.com/dotnet/api/overview/azure/ai.openai-readme
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
