# Aspire.Hosting.OpenAI library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure OpenAI Models.

## Getting started

### Prerequisites

- An OpenAI account with access to the OpenAI API
- OpenAI [API key](https://platform.openai.com/api-keys)

### Install the package

In your AppHost project, install the .NET Aspire OpenAI Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.OpenAI
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add an OpenAI Model resource and consume the connection using the following methods:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var chat = builder.AddOpenAIModel("chat", "gpt-4o-mini");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(chat);
```

The `WithReference` method passes that connection information into a connection string named `chat` in the `MyService` project.

In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.OpenAI](https://www.nuget.org/packages/Aspire.OpenAI):

#### OpenAI client usage
```csharp
builder.AddOpenAIClient("chat");
```

## Configuration

The OpenAI Model resource can be configured with the following options:

### API Key

The API key can be set as a configuration value using the default name `{resource_name}-openai-apikey` or the `OPENAI_API_KEY` environment variable.

Then in user secrets:

```json
{
    "Parameters": 
    {
        "chat-openai-apikey": "YOUR_OPENAI_API_KEY_HERE"
    }
}
```

Furthermore, the API key can be configured using a custom parameter:

```csharp
var apiKey = builder.AddParameter("my-api-key", secret: true);
var chat = builder.AddOpenAIModel("chat", "gpt-4o-mini")
                  .WithApiKey(apiKey);
```

Then in user secrets:

```json
{
    "Parameters": 
    {
        "my-api-key": "YOUR_OPENAI_API_KEY_HERE"
    }
}
```

## Available Models

OpenAI supports various AI models. Some popular options include:

- `gpt-4o-mini`
- `gpt-4o`
- `gpt-4-turbo`
- `gpt-3.5-turbo`
- `text-embedding-3-small`
- `text-embedding-3-large`
- `dall-e-3`
- `whisper-1`

Check the [OpenAI Models documentation](https://platform.openai.com/docs/models) for the most up-to-date list of available models.

## Additional documentation

* https://platform.openai.com/docs/models
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
