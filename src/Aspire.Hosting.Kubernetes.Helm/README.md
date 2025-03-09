# Aspire.Hosting.Kubernetes library

Provides publishing extensions to .NET Aspire for Kubernetes Helm Charts.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Kubernetes Helm Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Kubernetes.Helm
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, register the publisher:

```csharp
builder.AddPublisher<HelmPublisher>("helm");
```

```shell
aspire publish -t helm -o artifacts
```

## Feedback & contributing

https://github.com/dotnet/aspire
