# Aspire.HealthChecks library

Exposes Health Check related classes for extending default HealthCheck behavior.

## Getting started

### Install the package

Install the Aspire HealthChecks library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.HealthChecks
```

## Usage example

In the _Extensions.cs_ file of your project (if you're using the Aspire templates; otherwise, wherever you call `app.MapHealthChecks("/health")`), use the overload that accepts `HealthCheckOptions` and configure the `ResponseWriter` as shown below:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = AspireHealthCheckResponseWriter.WriteResponse
});
```

The default behavior only returns the overall HealthCheck status, regardless of how many checks the service has in place. Providing the AspireHealthCheckResponseWriter surfaces an elaborate response the Aspire Health Check system understands how to parse.
