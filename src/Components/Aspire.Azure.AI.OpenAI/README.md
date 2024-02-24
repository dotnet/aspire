# Aspire.Azure.AI.OpenAI library

Registers [OpenAIClient](https://learn.microsoft.com/dotnet/api/azure.ai.openai.openaiclient) as a singleton in the DI container for connecting to Azure OpenAI or OpenAI. Enables corresponding logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure OpenAI or OpenAI account - [create an Azure OpenAI Service resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource)

### Install the package

Install the .NET Aspire Azure AI OpenAI library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Azure.AI.OpenAI
```

## Usage example

In the _Program.cs_ file of your project, call the `AddAzureAIOpenAI` extension method to register an `OpenAIClient` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureAIOpenAI("openaiConnectionName");
```

You can then retrieve the `OpenAIClient` instance using dependency injection. For example, to retrieve the client from a Web API controller:

```csharp
private readonly OpenAIClient _client;

public CognitiveController(OpenAIClient client)
{
    _client = client;
}
```

See the [Azure OpenAI Service quickstarts](https://learn.microsoft.com/azure/ai-services/openai/quickstart) for examples on using the `OpenAIClient`.

## Configuration

The .NET Aspire Azure Azure OpenAI library provides multiple options to configure the Azure OpenAI Service based on the requirements and conventions of your project. Note that either an `Endpoint` or a `ConnectionString` is required to be supplied.

### Use a connection string

A connection can be constructed from the __Keys and Endpoint__ tab with the format `Endpoint={endpoint};Key={key};`. You can provide the name of the connection string when calling `builder.AddAzureAIOpenAI()`:

```csharp
builder.AddAzureAIOpenAI("openaiConnectionName");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

#### Account Endpoint

The recommended approach is to use an Endpoint, which works with the `AzureOpenAISettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "openaiConnectionName": "https://{account_name}.openai.azure.com/"
  }
}
```

#### Connection string

Alternatively, a custom connection string can be used.

```json
{
  "ConnectionStrings": {
    "openaiConnectionName": "Endpoint=https://{account_name}.openai.azure.com/;Key={account_key};"
  }
}
```

In order to connect to the non-Azure OpenAI service, drop the Endpoint property and only set the Key property to set the API key (https://platform.openai.com/account/api-keys).

### Use configuration providers

The .NET Aspire Azure AI OpenAI library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureOpenAISettings` and `OpenAIClientOptions` from configuration by using the `Aspire:Azure:AI:OpenAI` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "AI": {
        "OpenAI": {
          "Tracing": true,
        }
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<AzureOpenAISettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
    builder.AddAzureAIOpenAI("openaiConnectionName", settings => settings.Tracing = false);
```

You can also setup the [OpenAIClientOptions](https://learn.microsoft.com/dotnet/api/azure.ai.openai.openaiclientoptions) using the optional `Action<IAzureClientBuilder<OpenAIClient, OpenAIClientOptions>> configureClientBuilder` parameter of the `AddAzureAIOpenAI` method. For example, to set the client ID for this client:

```csharp
    builder.AddAzureAIOpenAI("openaiConnectionName", configureClientBuilder: builder => builder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "CLIENT_ID"));
```

## AppHost extensions

In your AppHost project, install the Aspire Azure Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure
```

Then, in the _Program.cs_ file of `AppHost`, add an Azure AI OpenAI service and consume the connection using the following methods:

```csharp
var openai = builder.AddAzureAIOpenAI("openai");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(openai);
```

The `AddAzureAIOpenAI` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:openai` config key. The `WithReference` method passes that connection information into a connection string named `openai` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using:

```csharp
builder.AddAzureAIOpenAI("openai");
```

## Additional documentation

* https://learn.microsoft.com/dotnet/api/overview/azure/ai.openai-readme
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
