# Aspire.Azure.AI.Inference library

Registers [ChatCompletionsClient](https://learn.microsoft.com/dotnet/api/azure.ai.inference.chatcompletionsclient) as a singleton in the DI container for connecting to Azure AI Foundry and GitHub Models. Enables corresponding metrics, logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure AI Foundry Resource - [create an Azure AI Foundry resource](https://learn.microsoft.com/azure/ai-foundry/how-to/develop/sdk-overview?tabs=sync&pivots=programming-language-csharp)

### Install the package

Install the .NET Aspire Azure Inference library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Azure.AI.Inference
```

## Usage example

In the _AppHost.cs_ file of your project, call the `AddChatCompletionsClient` extension method to register a `ChatCompletionsClient` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddChatCompletionsClient("connectionName");
```

You can then retrieve the `ChatCompletionsClient` instance using dependency injection. For example, to retrieve the client from a Web API controller:

```csharp
private readonly ChatCompletionsClient _client;

public CognitiveController(ChatCompletionsClient client)
{
    _client = client;
}
```

See the [Azure AI Foundry SDK quickstarts](https://learn.microsoft.com/azure/ai-foundry/how-to/develop/sdk-overview) for examples on using the `ChatCompletionsClient`.

## Configuration

The .NET Aspire Azure AI Inference library provides multiple options to configure the Azure AI Foundry Service based on the requirements and conventions of your project. Note that either an `Endpoint` and `DeploymentId`, or a `ConnectionString` is required to be supplied.

### Use a connection string

A connection can be constructed from the __Keys, Deployment ID and Endpoint__ tab with the format `Endpoint={endpoint};Key={key};DeploymentId={deploymentId}`. You can provide the name of the connection string when calling `builder.AddChatCompletionsClient()`:

```csharp
builder.AddChatCompletionsClient("connectionName");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

#### Azure AI Foundry Endpoint

The recommended approach is to use an Endpoint, which works with the `ChatCompletionsClientSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "connectionName": "Endpoint=https://{endpoint}/;DeploymentId={deploymentName}"
  }
}
```

#### Connection string

Alternatively, a custom connection string can be used.

```json
{
  "ConnectionStrings": {
    "connectionName": "Endpoint=https://{endpoint}/;Key={account_key};DeploymentId={deploymentName}"
  }
}
```

### Use configuration providers

The .NET Aspire Azure AI Inference library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `ChatCompletionsClientSettings` and `AzureAIInferenceClientOptions` from configuration by using the `Aspire:Azure:AI:Inference` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "AI": {
        "Inference": {
          "DisableTracing": false,
          "ClientOptions": {
            "UserAgentApplicationId": "myapp"
          }
        }
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<ChatCompletionsClientSettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
builder.AddChatCompletionsClient("connectionName", settings => settings.DisableTracing = true);
```

You can also setup the [AzureAIInferenceClientOptions](https://learn.microsoft.com/dotnet/api/azure.ai.inference.AzureAIInferenceClientOptions) using the optional `Action<IAzureClientBuilder<ChatCompletionsClient, AzureAIInferenceClientOptions>> configureClientBuilder` parameter of the `AddChatCompletionsClient` method. For example, to set the client ID for this client:

```csharp
builder.AddChatCompletionsClient("connectionName", configureClientBuilder: configureClientBuilder: builder => builder.ConfigureOptions(options => options.NetworkTimeout = TimeSpan.FromSeconds(2)));
```

## Experimental Telemetry

Azure AI OpenAI telemetry support is experimental, the shape of traces may change in the future without notice.
It can be enabled by invoking

```c#
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
```

or by setting the "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".

## Additional documentation

* https://learn.microsoft.com/dotnet/api/azure.ai.inference
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
