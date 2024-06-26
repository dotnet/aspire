# Aspire.Hosting.Structurizr library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Structurizr resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Structurizr Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Structurizr
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add a Structurizr resource:

```csharp
builder.AddStructurizr("structurizr");
```

## Additional documentation
https://docs.structurizr.com/

## Feedback & contributing

https://github.com/dotnet/aspire
