# Aspire.Hosting.GitHub.Models library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure GitHub Models.

## Getting started

### Prerequisites

- GitHub account with access to GitHub Models
- GitHub [personal access token](https://docs.github.com/en/github-models/use-github-models/prototyping-with-ai-models#experimenting-with-ai-models-using-the-api) with appropriate permissions (`models: read`)

### Install the package

In your AppHost project, install the .NET Aspire GitHub Models Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.GitHub.Models
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a GitHub Model resource and consume the connection using the following methods:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("github-api-key", secret: true);

var chat = builder.AddGitHubModel("chat", "openai/gpt-4o-mini")
                  .WithApiKey(apiKey);

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(chat);
```

The `WithReference` method passes that connection information into a connection string named `chat` in the `MyService` project.

In the _Program.cs_ file of `MyService`, the connection can be consumed using a client library like [Aspire.Azure.AI.Inference](https://www.nuget.org/packages/Aspire.Azure.AI.Inference):

#### Inference client usage
```csharp
builder.AddAzureChatCompletionsClient("chat")
       .AddChatClient();
```

## Configuration

The GitHub Model resource can be configured with the following options:

### API Key

The API key can be configured using a parameter:

```csharp
var apiKey = builder.AddParameter("github-api-key", secret: true);
var chat = builder.AddGitHubModel("chat", "openai/gpt-4o-mini")
                  .WithApiKey(apiKey);
```

Then in user secrets:

```json
{
    "Parameters": 
    {
        "github-api-key": "YOUR_GITHUB_TOKEN_HERE"
    }
}
```

Or directly as a string (not recommended for production):

```csharp
var chat = builder.AddGitHubModel("chat", "openai/gpt-4o-mini")
                  .WithApiKey("your-api-key-here");
```

## Available Models

GitHub Models supports various AI models. Some popular options include:

- `openai/gpt-4o-mini`
- `openai/gpt-4o`
- `deepseek/DeepSeek-V3-0324`
- `microsoft/Phi-4-mini-instruct`

Check the [GitHub Models documentation](https://docs.github.com/en/github-models) for the most up-to-date list of available models.

## Additional documentation

* https://docs.github.com/en/github-models
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
