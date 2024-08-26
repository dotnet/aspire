using Aspire.Hosting.Azure;

/// <remarks>
/// This well-defined AzureFunctionsProjectResource type allows us to implement custom logic required by
/// Azure Functions. Specifically, running the `func host` via coretools to support running a
/// Functions host locally. We can imagine a future where we enable ProjectResource like behavior
/// for Functions apps, in which case we could model the function app as a project resource.
/// Regardless, we'll likely want to use a custom resource type for functions to permit us to
/// grow from one implementation to the next.
/// </remarks>
public class AzureFunctionsProjectResource(string name, string executable, string workingDirectory)
    : ExecutableResource(name, executable, workingDirectory), IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery
{ }

public static class AzureFunctionsProjectResourceExtensions
{
    /// <remarks>
    /// This implementation demonstrates the approach of "teaching Aspire about Functions" where we take advantage of
    /// Aspire's ConnectionStringExpression to map to the connection strings format that Azure Functions understands.
    /// An alternative approach here is to "teach Functions about Aspire" where integrate the Aspire configuration
    /// model into the Azure Functions worker extensions.
    /// </remarks>
    public static IResourceBuilder<AzureFunctionsProjectResource> WithReference(this IResourceBuilder<AzureFunctionsProjectResource> builder, IResourceBuilder<IResourceWithConnectionString> source, string? name = null)
    {
        return builder.WithEnvironment((context) =>
        {
            if (source.Resource is AzureQueueStorageResource azureQueueStorageResource)
            {
                var suffix = name ?? "Storage";
                if (azureQueueStorageResource.Parent.IsEmulator)
                {
                    context.EnvironmentVariables[$"AzureWebJobs{suffix}"] = azureQueueStorageResource.Parent.GetEmulatorConnectionString();
                }
                else
                {
                    context.EnvironmentVariables[$"AzureWebJobs{suffix}__queueServiceUri"] = azureQueueStorageResource.ConnectionStringExpression;
                }
            }
            else if (source.Resource is AzureBlobStorageResource azureBlobStorageResource)
            {
                var suffix = name ?? "Storage";
                if (azureBlobStorageResource.Parent.IsEmulator)
                {
                    context.EnvironmentVariables[$"AzureWebJobs{suffix}"] = azureBlobStorageResource.Parent.GetEmulatorConnectionString();
                }
                else
                {
                    context.EnvironmentVariables[$"AzureWebJobs{suffix}__blobServiceUri"] = azureBlobStorageResource.ConnectionStringExpression;
                }
            }
            else if (source.Resource is AzureEventHubsResource azureEventHubsResource)
            {
                if (azureEventHubsResource.IsEmulator)
                {
                    context.EnvironmentVariables["EventHub"] = azureEventHubsResource.ConnectionStringExpression;
                }
                else
                {
                    context.EnvironmentVariables["EventHub__fullyQualifiedNamespace"] = azureEventHubsResource.ConnectionStringExpression;
                }
            }
        });
    }

    public static IResourceBuilder<AzureFunctionsProjectResource> AddAzureFunctionsProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IProjectMetadata, new()
    {
        var projectDirectory = Path.GetDirectoryName(new TProject().ProjectPath)!;
        var resource = new AzureFunctionsProjectResource(name, "func", projectDirectory);
        return builder.AddResource(resource)
            .WithArgs(async context =>
            {
                var http = resource.GetEndpoint("http");
                var debug = resource.GetEndpoint("debug");

                context.Args.Add("host");
                context.Args.Add("start");
                context.Args.Add("--verbose");
                context.Args.Add("--csharp");
                context.Args.Add("--port");
                var httpPort = await http.Property(EndpointProperty.TargetPort).GetValueAsync(CancellationToken.None);
                if (httpPort is not null)
                {
                    context.Args.Add(httpPort);
                }
                context.Args.Add("--language-worker");
                context.Args.Add("dotnet-isolated");
            })
            .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true")
            .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true")
            .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory")
            .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
            .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated")
            .WithOtlpExporter()
            .WithEndpoint("http", (endpoint) =>
            {
                endpoint.Protocol = System.Net.Sockets.ProtocolType.Tcp;
                endpoint.UriScheme = "http";
                endpoint.Transport = "http";
                endpoint.IsExternal = true;
            })
            .WithManifestPublishingCallback(async (context) =>
            {
                context.Writer.WriteString("type", "function.v0");
                context.Writer.WriteString("path", context.GetManifestRelativePath(new TProject().ProjectPath));
                await context.WriteEnvironmentVariablesAsync(resource);
                context.Writer.WriteStartObject("bindings");
                foreach (var s in new string[] { "http", "https" })
                {
                    context.Writer.WriteStartObject(s);
                    context.Writer.WriteString("scheme", s);
                    context.Writer.WriteString("protocol", "tcp");
                    context.Writer.WriteString("transport", "http");
                    context.Writer.WriteBoolean("external", true);
                    context.Writer.WriteEndObject();
                }
                context.Writer.WriteEndObject();
            });
    }
}
