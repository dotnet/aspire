# Aspire.Hosting.Kubernetes library

Provides publishing extensions to .NET Aspire for Kubernetes Kustomize.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Kubernetes Kustomize Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Kubernetes.Kustomize
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, register the publisher:

```csharp
builder.AddPublisher<KustomizePublisher>("kustomize");
```

```shell
aspire publish -t kustomize -o artifacts
```

## Feedback & contributing

https://github.com/dotnet/aspire
