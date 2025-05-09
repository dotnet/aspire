# Aspire.Hosting.Kubernetes library

Provides publishing extensions to .NET Aspire for Kubernetes.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Kubernetes Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Kubernetes
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add the environment:

```csharp
builder.AddKubernetesEnvironment("k8s");
```

```shell
aspire publish -o k8s-artifacts
```

## Feedback & contributing

https://github.com/dotnet/aspire
