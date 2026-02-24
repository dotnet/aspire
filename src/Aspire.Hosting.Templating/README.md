# Aspire.Hosting.Templating library

Provides extension methods and resource definitions for an Aspire AppHost to support Git repository-based project templates with interactive prompts.

## Getting started

### Install the package

In your template host project, install the Aspire Templating Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Templating
```

## Usage example

Then, in the _templatehost.cs_ file of your template, add template prompts and tasks:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var projectName = builder.AddTemplatePrompt("projectName", "Enter the project name")
    .WithDefaultValue("MyApp");

builder.Build().Run();
```

## Additional documentation

https://github.com/dotnet/aspire

## Feedback & contributing

https://github.com/dotnet/aspire
